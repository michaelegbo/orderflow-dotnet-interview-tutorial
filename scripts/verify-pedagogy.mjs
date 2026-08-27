import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDir = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(scriptDir, '..');
const html = fs.readFileSync(path.join(root, 'docs', 'index.html'), 'utf8');
const manifest = JSON.parse(fs.readFileSync(path.join(root, 'docs', 'lesson-manifest.json'), 'utf8'));

function lessonSequence(titlePattern) {
  const lesson = manifest.lessons.find(item => titlePattern.test(item.title.replace(/`/g, '')));
  if (!lesson) throw new Error(`Could not find introduction lesson: ${titlePattern}`);
  return lesson.sequence;
}

const concepts = [
  ['string interpolation', /^5\. Strings and String Interpolation$/i, /\$"/],
  ['if / else', /^7\. if \/ else$/i, /\b(?:if|else)\b/],
  ['ternary operator', /^8\. Ternary Operator$/i, /\?[^?\n;]+:/],
  ['switch', /^9\. switch$/i, /\bswitch\b/],
  ['loops', /^10\. Loops$/i, /\b(?:for|foreach|while)\s*\(/],
  ['method declarations', /^11\. Methods$/i, /^\s*(?:public\s+|private\s+|protected\s+|internal\s+|static\s+)*(?:void|string|int|decimal|bool|double|long|char|[A-Z]\w*(?:<[^>]+>)?)\s+[A-Za-z_]\w*\s*\([^;]*\)/m],
  ['classes', /^12\. Classes and Objects$/i, /\bclass\s+[A-Za-z_]\w*/],
  ['properties', /^13\. Fields vs Properties$/i, /\{\s*(?:get|set|init)\s*;/],
  ['constructors', /^14\. Constructors$/i, /\b(?:public|private|protected|internal)\s+[A-Z]\w*\s*\(/],
  ['private encapsulation', /^16\. Encapsulation$/i, /\bprivate\b/],
  ['static', /^18\. static$/i, /\bstatic\b/],
  ['inheritance', /^19\. Inheritance$/i, /\bclass\s+\w+\s*:\s*\w+/],
  ['virtual / override', /^20\. Polymorphism$/i, /\b(?:virtual|override)\b/],
  ['interfaces', /^22\. Interfaces$/i, /\binterface\s+I\w+|\bclass\s+\w+\s*:\s*I\w+/],
  ['abstract classes', /^23\. Abstract Classes$/i, /\babstract\b/],
  ['arrays', /^24\. Arrays$/i, /\b[A-Za-z_]\w*\s*\[\]/, /^Exercise 2 — Filtering Orders$/i, 'Supplied scaffold'],
  ['List<T>', /^25\. List<T>$/i, /\bList</],
  ['Dictionary<TKey,TValue>', /^26\. Dictionary/i, /\bDictionary</],
  ['HashSet<T>', /^27\. HashSet/i, /\bHashSet</],
  ['Stack<T>', /^28\. Stack/i, /\bStack</],
  ['Queue<T>', /^29\. Queue/i, /\bQueue</],
  ['nullable references', /^31\. Nullable Reference Types$/i, /\b(?:string|Order|object|T)\?/],
  ['null conditional', /^32\. Null Conditional$/i, /\?\./],
  ['null coalescing', /^33\. Null Coalescing$/i, /\?\?/],
  ['exceptions', /^34\. Exceptions$/i, /\b(?:try|catch|finally|throw)\b/],
  ['using / disposal', /^35\. using and IDisposable$/i, /\busing\s+(?:var|[A-Z]\w*)\b/],
  ['lambdas / expression bodies', /^36\. Lambdas$/i, /=>/, /^9\. switch$/i],
  ['LINQ', /^37\. LINQ$/i, /\.(?:Where|Select|OrderBy|OrderByDescending|Sum|ToList|Count)\s*\(/],
  ['IEnumerable<T>', /^40\. IEnumerable/i, /\bIEnumerable</],
  ['delegates', /^41\. Delegates$/i, /\b(?:Func|Action)</],
  ['events', /^42\. Events$/i, /\bevent\b/],
  ['async / await / Task', /^43\. Why Async Exists$/i, /\b(?:async|await|Task(?:<|\b))\b/],
  ['CancellationToken', /^49\. CancellationToken$/i, /\bCancellationToken\b/],
  ['ASP.NET Core host', /^64\. Program\.cs$/i, /\bWebApplication(?:Builder)?\b/],
  ['middleware', /^66\. Middleware$/i, /\bapp\.Use[A-Za-z]+\s*\(/],
  ['controllers', /^67\. Controllers$/i, /\bControllerBase\b|\[ApiController\]/],
  ['routing attributes', /^68\. Routing$/i, /\[(?:Route|HttpGet|HttpPost|HttpPut|HttpPatch|HttpDelete)/],
  ['DTOs', /^71\. DTOs$/i, /\b(?:CreateOrderRequest|OrderResponse|\w+Dto)\b/],
  ['validation attributes', /^73\. Validation$/i, /\[(?:Required|Range|StringLength|MinLength|MaxLength)/],
  ['EF Core', /^78\. What EF Core Is$/i, /\b(?:DbContext|DbSet<|EntityTypeBuilder<)/],
  ['IQueryable<T>', /^82\. IEnumerable<T> vs IQueryable<T>$/i, /\bIQueryable</],
  ['AsNoTracking', /^83\. Tracking and AsNoTracking$/i, /\.AsNoTracking\s*\(/],
  ['SaveChangesAsync', /^84\. SaveChangesAsync$/i, /\bSaveChangesAsync\s*\(/],
  ['unit-test attributes', /^97\. Unit Tests$/i, /\[(?:Fact|Theory|Test)\]/],
  ['logging', /^103\. Logging$/i, /\bILogger</]
].map(([name, intro, pattern, allow, scaffold]) => ({ name, introSequence:lessonSequence(intro), pattern, allow, scaffold }));

function decode(value) {
  return value
    .replace(/<span[^>]*>/g, '').replace(/<\/span>/g, '')
    .replace(/&quot;/g, '"').replace(/&#39;/g, "'")
    .replace(/&lt;/g, '<').replace(/&gt;/g, '>').replace(/&amp;/g, '&');
}

const cards = [...html.matchAll(/<article class="lesson [\s\S]*?<\/article>/g)].map(match => match[0]);
if (cards.length !== manifest.lessons.length)
  throw new Error(`Expected ${manifest.lessons.length} lesson cards, found ${cards.length}`);

const findings = [];
function inspect(lesson, surface, code, surroundingText = '') {
  for (const rule of concepts) {
    if (lesson.sequence >= rule.introSequence || !rule.pattern.test(code)) continue;
    const allowed = rule.allow?.test(lesson.title) && (!rule.scaffold || surroundingText.includes(rule.scaffold));
    if (!allowed) findings.push({
      sequence:lesson.sequence,
      title:lesson.title,
      surface,
      concept:rule.name,
      introducedAt:manifest.lessons.find(item => item.sequence === rule.introSequence).title
    });
  }
}

for (const [index, lesson] of manifest.lessons.entries()) {
  const card = cards[index];
  const contentStart = card.indexOf('<div class="lesson-content">');
  const practiceStart = card.indexOf('<section class="lesson-practice');
  const lessonContent = contentStart >= 0 && practiceStart > contentStart
    ? card.slice(contentStart, practiceStart)
    : '';
  const lessonCodes = [...lessonContent.matchAll(/<pre><code class="language-(?:csharp|cs)">([\s\S]*?)<\/code><\/pre>/g)].map(match => decode(match[1]));
  for (const [blockIndex, code] of lessonCodes.entries()) inspect(lesson, `lesson example ${blockIndex + 1}`, code, decode(lessonContent));

  if (lesson.exercise?.starter?.kind === 'csharp')
    inspect(lesson, 'supplied exercise starter', lesson.exercise.starter.content, card);

  const answerSection = card.match(/<div class="exercise-answer-code">([\s\S]*?)<\/div><\/div>/)?.[1] || '';
  const focusedCode = answerSection.match(/<pre><code[^>]*>([\s\S]*?)<\/code><\/pre>/)?.[1];
  if (focusedCode) inspect(lesson, 'focused answer', decode(focusedCode), card);
}

const report = { lessons:manifest.lessons.length, concepts:concepts.length, findings:findings.length, items:findings };
console.log(JSON.stringify(report, null, 2));
if (findings.length) process.exit(1);
