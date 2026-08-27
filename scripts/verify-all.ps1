[CmdletBinding()]
param(
    [int] $Port = 5194,
    [switch] $SkipLiveApi
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$apiProject = 'src/OrderFlow.Api'
$infrastructureProject = 'src/OrderFlow.Infrastructure'
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("orderflow-verify-" + [Guid]::NewGuid().ToString('N'))
$apiProcess = $null
$tokensCreated = $false
$checks = [System.Collections.Generic.List[string]]::new()

function Invoke-DotNet {
    param(
        [Parameter(Mandatory)] [string] $Label,
        [Parameter(Mandatory)] [string[]] $Arguments
    )

    Write-Host "[verify] $Label"
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed with exit code $LASTEXITCODE."
    }
    $checks.Add($Label)
}

function Invoke-Stage {
    param(
        [Parameter(Mandatory)] [string] $Label,
        [Parameter(Mandatory)] [string] $Project,
        [Parameter(Mandatory)] [string] $Marker
    )

    Write-Host "[verify] $Label"
    $output = (& dotnet run --project $Project 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) {
        throw "$Label failed.`n$output"
    }
    if (-not $output.Contains($Marker, [StringComparison]::Ordinal)) {
        throw "$Label ran but its pass marker was missing.`n$output"
    }
    Write-Host $Marker
    $checks.Add($Label)
}

function Send-ApiRequest {
    param(
        [Parameter(Mandatory)] [string] $Method,
        [Parameter(Mandatory)] [string] $Uri,
        [string] $Token,
        [string] $Body
    )

    $parameters = @{
        Method = $Method
        Uri = $Uri
        SkipHttpErrorCheck = $true
    }
    if ($Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body
    }
    if ($Token) {
        $parameters.Headers = @{ Authorization = "Bearer $Token" }
    }
    Invoke-WebRequest @parameters
}

function Assert-Status {
    param(
        [Parameter(Mandatory)] $Response,
        [Parameter(Mandatory)] [int] $Expected,
        [Parameter(Mandatory)] [string] $Label
    )

    if ([int]$Response.StatusCode -ne $Expected) {
        throw "$Label expected HTTP $Expected but received $($Response.StatusCode). Body: $($Response.Content)"
    }
    $checks.Add("$Label -> HTTP $Expected")
}

$previousEnvironment = @{
    ASPNETCORE_URLS = $env:ASPNETCORE_URLS
    ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
    ConnectionStrings__Orders = $env:ConnectionStrings__Orders
}

Push-Location $repoRoot
try {
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null

    Invoke-Stage 'Stage 01 console execution' `
        'tutorial-snapshots/01-console/OrderFlow.Console.csproj' `
        'STAGE 01 PASS — syntax, control flow and methods'
    Invoke-Stage 'Stage 02 domain/LINQ execution' `
        'tutorial-snapshots/02-domain-linq/OrderFlow.Stage02.csproj' `
        'STAGE 02 PASS — objects, interface, collections and LINQ'
    Invoke-Stage 'Stage 03 async-service execution' `
        'tutorial-snapshots/03-async-service/OrderFlow.Stage03.csproj' `
        'STAGE 03 PASS — async, cancellation and retry-safe state transition'

    Invoke-DotNet 'Restore local tools' @('tool', 'restore')
    Invoke-DotNet 'Restore every solution project' @('restore', 'OrderFlow.sln')
    Invoke-DotNet 'Build every snapshot and final project' @('build', 'OrderFlow.sln', '--no-restore', '--nologo', '-warnaserror')
    Invoke-DotNet 'Run all unit and integration tests' @('test', 'OrderFlow.sln', '--no-build', '--nologo')
    Invoke-DotNet 'Check for pending EF model changes' @(
        'tool', 'run', 'dotnet-ef', 'migrations', 'has-pending-model-changes',
        '--project', $infrastructureProject,
        '--startup-project', $apiProject
    )

    $audit = (& dotnet list OrderFlow.sln package --vulnerable --include-transitive 2>&1 | Out-String)
    if ($LASTEXITCODE -ne 0) {
        throw "NuGet vulnerability audit failed.`n$audit"
    }
    if ($audit -match 'following vulnerable packages') {
        throw "The NuGet vulnerability audit reported a vulnerable package.`n$audit"
    }
    $checks.Add('NuGet vulnerability audit -> no vulnerable packages reported')

    $databasePath = Join-Path $temporaryRoot 'orderflow-e2e.db'
    $env:ConnectionStrings__Orders = "Data Source=$databasePath"
    Invoke-DotNet 'Apply the clean migration to a new SQLite database' @(
        'tool', 'run', 'dotnet-ef', 'database', 'update',
        '--project', $infrastructureProject,
        '--startup-project', $apiProject
    )
    if (-not (Test-Path -LiteralPath $databasePath)) {
        throw 'EF reported success but the SQLite database was not created.'
    }

    if (-not $SkipLiveApi) {
        Write-Host '[verify] Create short-lived development JWTs without printing them'
        $viewerToken = (& dotnet user-jwts create --project $apiProject --role Viewer --valid-for 5m --output token).Trim()
        if ($LASTEXITCODE -ne 0 -or $viewerToken.Length -lt 100) {
            throw 'Could not create the Viewer development JWT.'
        }
        $managerToken = (& dotnet user-jwts create --project $apiProject --role OrderManager --valid-for 5m --output token).Trim()
        if ($LASTEXITCODE -ne 0 -or $managerToken.Length -lt 100) {
            throw 'Could not create the OrderManager development JWT.'
        }
        $tokensCreated = $true

        $env:ASPNETCORE_URLS = "http://127.0.0.1:$Port"
        $env:ASPNETCORE_ENVIRONMENT = 'Development'
        $stdoutPath = Join-Path $temporaryRoot 'api.stdout.log'
        $stderrPath = Join-Path $temporaryRoot 'api.stderr.log'
        $apiProcess = Start-Process `
            -FilePath 'dotnet' `
            -ArgumentList @('run', '--project', $apiProject, '--no-build', '--no-launch-profile') `
            -WorkingDirectory $repoRoot `
            -WindowStyle Hidden `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath `
            -PassThru

        $baseUri = "http://127.0.0.1:$Port"
        $health = $null
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds(30)
        while ([DateTimeOffset]::UtcNow -lt $deadline) {
            if ($apiProcess.HasExited) {
                $stderr = if (Test-Path -LiteralPath $stderrPath) { Get-Content -LiteralPath $stderrPath -Raw } else { '' }
                throw "The live API exited before becoming ready. $stderr"
            }
            try {
                $health = Send-ApiRequest -Method Get -Uri "$baseUri/health"
                if ($health.StatusCode -eq 200) { break }
            }
            catch {
                # The server is still starting.
            }
            Start-Sleep -Milliseconds 250
        }
        if ($null -eq $health -or $health.StatusCode -ne 200) {
            throw 'The live API did not become healthy within 30 seconds.'
        }
        Assert-Status $health 200 'Health endpoint'

        $requestBody = @{
            customer = 'Ada'
            lines = @(@{ product = 'Keyboard'; quantity = 2; unitPrice = 40 })
        } | ConvertTo-Json -Depth 5 -Compress

        Assert-Status (Send-ApiRequest -Method Post -Uri "$baseUri/api/orders" -Body $requestBody) 401 'Unauthenticated create'
        Assert-Status (Send-ApiRequest -Method Post -Uri "$baseUri/api/orders" -Token $viewerToken -Body $requestBody) 403 'Wrong-role create'
        $invalidBody = @{ customer = ''; lines = @() } | ConvertTo-Json -Depth 3 -Compress
        Assert-Status (Send-ApiRequest -Method Post -Uri "$baseUri/api/orders" -Token $managerToken -Body $invalidBody) 400 'Invalid create model'
        Assert-Status (Send-ApiRequest -Method Get -Uri "$baseUri/api/orders/$([Guid]::Empty)") 404 'Missing order lookup'

        $created = Send-ApiRequest -Method Post -Uri "$baseUri/api/orders" -Token $managerToken -Body $requestBody
        Assert-Status $created 201 'Manager create'
        $createdOrder = $created.Content | ConvertFrom-Json
        if (-not $createdOrder.id -or [decimal]$createdOrder.total -ne [decimal]80) {
            throw "The create response did not contain the expected order ID and £80 total. Body: $($created.Content)"
        }

        Assert-Status (Send-ApiRequest -Method Get -Uri "$baseUri/api/orders/$($createdOrder.id)") 200 'Read created order'
        Assert-Status (Send-ApiRequest -Method Put -Uri "$baseUri/api/orders/$($createdOrder.id)/pay" -Token $managerToken) 200 'First payment transition'
        Assert-Status (Send-ApiRequest -Method Put -Uri "$baseUri/api/orders/$($createdOrder.id)/pay" -Token $managerToken) 200 'Repeated payment transition'
    }

    Write-Host ''
    Write-Host "ORDERFLOW END-TO-END PASS — $($checks.Count) checks completed"
    foreach ($check in $checks) {
        Write-Host "  ✓ $check"
    }
}
finally {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Stop-Process -Id $apiProcess.Id -Force
        $apiProcess.WaitForExit()
    }
    if ($tokensCreated) {
        & dotnet user-jwts clear --project $apiProject --force | Out-Null
    }

    foreach ($name in $previousEnvironment.Keys) {
        $value = $previousEnvironment[$name]
        if ($null -eq $value) {
            Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue
        }
        else {
            Set-Item -LiteralPath "Env:$name" -Value $value
        }
    }

    $resolvedTemp = [System.IO.Path]::GetFullPath($temporaryRoot)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedTemp.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and
        [System.IO.Path]::GetFileName($resolvedTemp).StartsWith('orderflow-verify-', [StringComparison]::Ordinal)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force -ErrorAction SilentlyContinue
    }
    Pop-Location
}
