import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const scriptsDirectory = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(scriptsDirectory, '..');
const docs = path.join(root, 'docs');
const html = fs.readFileSync(path.join(docs, 'index.html'), 'utf8');
const manifest = JSON.parse(fs.readFileSync(path.join(docs, 'lesson-manifest.json'), 'utf8'));
const packageMode = process.argv.includes('--package');
const failures = [];
const assert = (condition, message) => { if (!condition) failures.push(message); };

const lessonIds = [...html.matchAll(/<article class="lesson [^"]*" id="([^"]+)"/g)].map(match => match[1]);
const codeLinks = [...html.matchAll(/<a class="lesson-code-link" href="([^"]+)"/g)].map(match => match[1].replaceAll('&amp;', '&'));
const allIds = [...html.matchAll(/\sid="([^"]+)"/g)].map(match => match[1]);
const count = pattern => [...html.matchAll(pattern)].length;
const inlineScript = html.match(/<script>([\s\S]*?)<\/script>/)?.[1] ?? '';
const htmlWithoutSnapshotData = html.replace(/<script type="application\/json" id="snapshot-store">[\s\S]*?<\/script>/, '');
const contentCount = pattern => [...htmlWithoutSnapshotData.matchAll(pattern)].length;

assert(lessonIds.length === 176, `Expected 176 lesson cards; found ${lessonIds.length}.`);
assert(manifest.schemaVersion === 2, `Expected lesson manifest schema 2; found ${manifest.schemaVersion}.`);
assert(codeLinks.length === lessonIds.length, 'Every lesson must have one code-checkpoint link.');
assert(manifest.lessons.length === lessonIds.length, 'The lesson manifest and HTML lesson count differ.');
assert(new Set(allIds).size === allIds.length, 'Duplicate DOM IDs found.');
assert(new Set(manifest.lessons.map(lesson => lesson.id)).size === lessonIds.length, 'Manifest lesson IDs are not unique.');
assert(new Set(manifest.lessons.map(lesson => lesson.codeStage)).size === 8, 'All eight stages are not represented.');
assert(count(/class="lesson-finish"/g) === lessonIds.length, 'Every lesson must have a bottom completion footer.');
assert(count(/class="lesson-check"/g) === lessonIds.length, 'Every lesson must have one completion control.');
assert(count(/id="time-left"/g) === 1, 'Estimated time-left indicator is missing or duplicated.');
assert(count(/class="lesson-estimate"/g) === lessonIds.length, 'Every lesson must show an estimated duration.');
assert(count(/class="nav-estimate"/g) === 8, 'Every sidebar chapter must show its remaining-time marker.');
assert(count(/class="chapter-progress-track"/g) === 8, 'Every chapter must show progress with remaining time.');
assert(count(/class="lesson-practice/g) === lessonIds.length, 'Every lesson must have one practical exercise panel.');
assert(count(/class="learning-lens mastery-teaching/g) === 128, 'Every concept lesson from 2–129 must have a visible mastery explanation.');
assert(count(/class="lesson-role role-/g) === lessonIds.length - 128, 'Every non-concept lesson must identify its orientation, practice, recall, or rehearsal role.');
assert(count(/class="practice-given"/g) === lessonIds.length, 'Every lesson must say what the learner is given.');
assert(count(/class="practice-starter"/g) === lessonIds.length, 'Every lesson must provide starter inputs or a response frame.');
assert(count(/class="practice-do"/g) === lessonIds.length, 'Every lesson must break the task into ordered steps.');
assert(count(/class="practice-steps"/g) === lessonIds.length, 'Every lesson must render its ordered steps.');
assert(count(/class="exercise-attempt"/g) === lessonIds.length, 'Every lesson must let the learner record an attempt.');
assert(count(/class="exercise-hint"/g) === lessonIds.length, 'Every lesson must include a progressive hint.');
assert(count(/class="reveal-exercise-answer"/g) === lessonIds.length, 'Every lesson must include a focused answer.');
assert(count(/class="exercise-snapshot chapter-snapshot"/g) === 8, 'Every chapter must expose one cumulative checkpoint after its lessons.');
assert(count(/class="lesson-scope"/g) === lessonIds.length, 'Every lesson must explicitly stay inside its current concept.');
const practicalLessonCount = manifest.lessons.filter(lesson => ['BUILD', 'EXPERIMENT'].includes(lesson.exercise?.type)).length;
assert(contentCount(/Only the new lines for this lesson/g) === practicalLessonCount, 'Every BUILD/EXPERIMENT lesson must use a lesson-scoped answer.');
assert(!htmlWithoutSnapshotData.includes('Potential code answer'), 'Unsafe first-code-block answer selection remains in the tutorial.');
assert(!/@@[A-Z]+\d*@@/.test(htmlWithoutSnapshotData), 'An internal rendering placeholder leaked into the tutorial.');
assert(inlineScript.includes("const perLessonMinutes = list.length ? plannedMinutes / list.length : 0"), 'Per-lesson estimated time calculation is missing.');
assert(inlineScript.includes("timeLeft += chapterTimeLeft"), 'Chapter estimates are not included in total time left.');
assert(inlineScript.includes("document.getElementById('time-left').textContent = formatMinutes(timeLeft)"), 'Estimated time-left UI is not updated with progress.');
assert(
  count(/class="check-feedback"/g) + count(/class="reveal-check-answer"/g) + count(/class="reveal-answer"/g) === lessonIds.length,
  'Every lesson must offer a quiz or revealable answer.'
);

const lessonCards = [...html.matchAll(/<article class="lesson [\s\S]*?<\/article>/g)].map(match => match[0]);
for (const [index, card] of lessonCards.entries()) {
  const contentIndex = card.indexOf('class="lesson-content"');
  const masteryIndex = card.indexOf('class="learning-lens mastery-teaching');
  const roleIndex = card.indexOf('class="lesson-role role-');
  const practiceIndex = card.indexOf('class="lesson-practice');
  const knowledgeIndex = card.indexOf('class="active-check"');
  const finishIndex = card.indexOf('class="lesson-finish"');
  const completionIndex = card.indexOf('class="lesson-check"');
  assert(contentIndex >= 0 && practiceIndex > contentIndex && knowledgeIndex > practiceIndex && finishIndex > knowledgeIndex && completionIndex > finishIndex, `Lesson ${index + 1} does not follow teach → practise → check → finish order.`);
  if (manifest.lessons[index]?.teaching)
    assert(masteryIndex > contentIndex && masteryIndex < practiceIndex, `Lesson ${index + 1} does not teach fully before practice.`);
  else
    assert(roleIndex > contentIndex && roleIndex < practiceIndex, `Lesson ${index + 1} does not explain its assessment or orientation role before practice.`);
}

const masteryLessons = manifest.lessons.filter(lesson => lesson.teaching);
assert(masteryLessons.length === 128, `Expected 128 teaching contracts; found ${masteryLessons.length}.`);
assert(masteryLessons.every(lesson => lesson.sourceNumber >= 2 && lesson.sourceNumber <= 129), 'A teaching contract is attached outside concept lessons 2–129.');
assert(manifest.lessons.filter(lesson => lesson.sourceNumber >= 2 && lesson.sourceNumber <= 129).every(lesson => lesson.teaching), 'A concept lesson from 2–129 is missing teaching metadata.');
for (const lesson of masteryLessons) {
  const teaching = lesson.teaching;
  assert(teaching.wordCount >= 225, `Teaching explanation is too thin (${teaching.wordCount} words): ${lesson.id}`);
  assert(!/fallback/i.test(teaching.category), `Teaching uses a generic fallback: ${lesson.id}`);
  assert(['anchor', 'supporting'].includes(teaching.tier), `Teaching tier is invalid: ${lesson.id}`);
  assert((teaching.simple ?? '').trim().length >= 35, `Simple explanation is too short: ${lesson.id}`);
  assert((teaching.precise ?? '').trim().length >= 45, `Technical explanation is too short: ${lesson.id}`);
  assert(Array.isArray(teaching.mechanics) && teaching.mechanics.length === 4 && teaching.mechanics.every(step => step.trim().length >= 45), `Mechanism walkthrough is incomplete: ${lesson.id}`);
  assert((teaching.connection ?? '').trim().length >= 90, `OrderFlow connection is too short: ${lesson.id}`);
  assert((teaching.why ?? '').trim().length >= 90, `Why-it-matters explanation is too short: ${lesson.id}`);
  assert((teaching.trap ?? '').trim().length >= 45, `Common boundary is too short: ${lesson.id}`);
  assert((teaching.interview ?? '').trim().length >= 90, `Interview-ready explanation is too short: ${lesson.id}`);
  assert(/^[a-f0-9]{64}$/.test(teaching.contentHash ?? ''), `Teaching content hash is missing: ${lesson.id}`);
}

for (const lesson of manifest.lessons) {
  assert(lessonIds.includes(lesson.id), `Manifest lesson missing from HTML: ${lesson.id}`);
  assert(codeLinks.includes(lesson.codeUrl), `Code URL missing from HTML: ${lesson.codeUrl}`);
  assert(/^https:\/\/github\.com\/[^/]+\/[^/]+\/tree\/stage-0[1-8]/.test(lesson.codeUrl), `Invalid stage URL: ${lesson.codeUrl}`);
  assert(/^(?:BUILD|EXPERIMENT|TRACE\/DEBUG|DESIGN|REHEARSE)$/.test(lesson.exercise?.type ?? ''), `Invalid exercise type for ${lesson.id}`);
  assert(/^[a-f0-9]{40}$/.test(lesson.exercise?.gitRef ?? ''), `Invalid lesson-history ref for ${lesson.id}`);
  assert(/^[a-f0-9]{40}$/.test(lesson.exercise?.treeHash ?? ''), `Invalid lesson-history tree for ${lesson.id}`);
  assert(/^[a-f0-9]{64}$/.test(lesson.sourceContentHash ?? ''), `Invalid source-content hash for ${lesson.id}`);
  assert((lesson.exercise?.task ?? '').trim().length >= 30, `Exercise task is missing or vague: ${lesson.id}`);
  assert((lesson.exercise?.given ?? '').trim().length >= 35, `Exercise supplied starting point is missing or vague: ${lesson.id}`);
  assert((lesson.exercise?.starter?.label ?? '').trim().length >= 12, `Exercise starter label is missing: ${lesson.id}`);
  assert((lesson.exercise?.starter?.content ?? '').trim().length >= 20, `Exercise starter inputs or response frame are missing: ${lesson.id}`);
  assert(Array.isArray(lesson.exercise?.steps) && lesson.exercise.steps.length >= 3, `Exercise is not broken into ordered steps: ${lesson.id}`);
  assert(lesson.exercise.steps.every(step => step.trim().length >= 15), `Exercise contains a vague ordered step: ${lesson.id}`);
  assert((lesson.exercise?.hint ?? '').trim().length >= 25, `Exercise hint is missing or vague: ${lesson.id}`);
  assert((lesson.exercise?.answer ?? '').trim().length >= 20, `Exercise answer is missing or vague: ${lesson.id}`);
  assert(/^[a-f0-9]{64}$/.test(lesson.exercise?.answerTextHash ?? ''), `Exercise answer text hash is missing: ${lesson.id}`);
  assert(lesson.exercise.answerTextHash === crypto.createHash('sha256').update(lesson.exercise.answer).digest('hex'), `Exercise answer text hash drifted: ${lesson.id}`);
  assert((lesson.exercise?.expected ?? '').trim().length >= 25, `Exercise proof is missing or vague: ${lesson.id}`);
  const shouldHaveCode = ['BUILD', 'EXPERIMENT'].includes(lesson.exercise?.type);
  assert(lesson.exercise?.answerSource === (shouldHaveCode ? 'explicit-code-contract' : 'explicit-concept-contract'), `Exercise answer is not explicitly authored for its task: ${lesson.id}`);
  assert(shouldHaveCode ? /^[a-f0-9]{64}$/.test(lesson.exercise?.answerCodeHash ?? '') : lesson.exercise?.answerCodeHash === null, `Focused-answer hash does not match exercise type: ${lesson.id}`);
  assert(Array.isArray(lesson.exercise?.answerContract), `Focused-answer contract is missing: ${lesson.id}`);
  assert(Array.isArray(lesson.exercise?.buildingBlocks), `Hidden guidance contract is missing: ${lesson.id}`);
  if (shouldHaveCode)
    assert(JSON.stringify(lesson.exercise.buildingBlocks) === JSON.stringify(lesson.exercise.answerContract), `Hidden guidance contract drifted from the answer contract: ${lesson.id}`);
  if (shouldHaveCode) {
    assert(!/Your learning project compiles|same learning project compiles|focused asynchronous flow|learner API builds|focused algorithm tests/.test(lesson.exercise.expected), `Practical exercise still uses a generic proof: ${lesson.id}`);
    assert(lesson.exercise.starter.label !== 'Keep your previous working code; add only this lesson', `Practical exercise still uses a generic starter: ${lesson.id}`);
  }
}

const interpolation = manifest.lessons.find(lesson => /Strings and String Interpolation/i.test(lesson.title));
assert(Boolean(interpolation), 'String interpolation lesson is missing.');
assert(interpolation?.exercise?.answerContract?.includes('$"'), 'String interpolation answer does not require an interpolated string.');
assert(interpolation?.exercise?.answerContract?.includes('{unitPrice:C}'), 'String interpolation answer does not require currency formatting.');
assert(!interpolation?.exercise?.answerContract?.some(value => value.includes('total') || value.includes('*')), 'String interpolation answer introduces operators too early.');

const operators = manifest.lessons.find(lesson => /6\. Operators/i.test(lesson.title));
assert(operators?.exercise?.starter?.content.includes('int quantity = 3;'), 'Operators exercise does not supply quantity.');
assert(operators?.exercise?.starter?.content.includes('decimal unitPrice = 25m;'), 'Operators exercise does not supply unitPrice.');
assert(operators?.exercise?.starter?.content.includes('bool isPaid = true;'), 'Operators exercise does not supply paid state.');
assert(!operators?.exercise?.steps?.join(' ').includes('quantity * unitPrice'), 'Operators steps reveal the hidden implementation.');
assert(operators?.exercise?.expected.includes('Valid quantity: True; can fulfil: True'), 'Operators exercise has no concrete expected result.');

const renderedChapters = html.split('<section class="chapter"').slice(1);
for (const [index, chapter] of renderedChapters.entries()) {
  const lessonsIndex = chapter.indexOf('class="lessons"');
  const checkpointIndex = chapter.indexOf('class="build-spine"');
  assert(lessonsIndex >= 0 && checkpointIndex > lessonsIndex, `Chapter ${index + 1} exposes its checkpoint before its lessons.`);
}
assert(html.includes('Contains later lessons—do not copy it yet'), 'Lesson links do not warn about later syntax.');

for (const [chapter, stage] of Object.entries(manifest.stages)) {
  const target = stage.path ? path.join(root, stage.path) : root;
  assert(fs.existsSync(target), `Stage ${chapter} path does not exist: ${stage.path || '.'}`);
}

if (!packageMode)
  assert(fs.existsSync(path.join(docs, 'orderflow-verified.zip')), 'The downloadable verified solution is missing.');
assert(html.includes('orderflow-verified.zip'), 'The HTML does not link to the downloadable solution.');
assert(!htmlWithoutSnapshotData.includes('${'), 'Unresolved template interpolation found.');
assert(!htmlWithoutSnapshotData.includes('OWNER/REPO'), 'Unresolved repository placeholder found.');

try {
  new vm.Script(`(function(){${inlineScript}})`);
}
catch (error) {
  failures.push(`Inline JavaScript syntax error: ${error.message}`);
}

try {
  execFileSync(process.execPath, [path.join(root, 'scripts', 'verify-pedagogy.mjs')], {
    cwd:root,
    stdio:'inherit'
  });
}
catch (error) {
  failures.push(`Pedagogical dependency audit failed with exit code ${error.status ?? 'unknown'}.`);
}

if (failures.length) {
  console.error(JSON.stringify({ failures }, null, 2));
  process.exit(1);
}

console.log(JSON.stringify({
  lessons: lessonIds.length,
  codeLinks: codeLinks.length,
  knowledgeChecks: count(/class="active-check"/g),
  practicalExercises: count(/class="lesson-practice/g),
  masteryExplanations: count(/class="learning-lens mastery-teaching/g),
  explicitPracticeAndRecallRoles: count(/class="lesson-role role-/g),
  projectSnapshots: count(/class="exercise-snapshot chapter-snapshot"/g),
  bottomCompletionControls: count(/class="lesson-finish"/g),
  stages: new Set(manifest.lessons.map(lesson => lesson.codeStage)).size,
  duplicateIds: allIds.length - new Set(allIds).size,
  failures: []
}, null, 2));
