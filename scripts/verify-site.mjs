import fs from 'node:fs';
import path from 'node:path';
import vm from 'node:vm';
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
assert(count(/class="exercise-attempt"/g) === lessonIds.length, 'Every lesson must let the learner record an attempt.');
assert(count(/class="exercise-hint"/g) === lessonIds.length, 'Every lesson must include a progressive hint.');
assert(count(/class="reveal-exercise-answer"/g) === lessonIds.length, 'Every lesson must include a focused answer.');
assert(count(/class="exercise-snapshot"/g) === lessonIds.length, 'Every lesson must expose a full project snapshot.');
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
  const practiceIndex = card.indexOf('class="lesson-practice');
  const knowledgeIndex = card.indexOf('class="active-check"');
  const finishIndex = card.indexOf('class="lesson-finish"');
  const completionIndex = card.indexOf('class="lesson-check"');
  assert(contentIndex >= 0 && practiceIndex > contentIndex && knowledgeIndex > practiceIndex && finishIndex > knowledgeIndex && completionIndex > finishIndex, `Lesson ${index + 1} does not follow teach → practise → check → finish order.`);
}

for (const lesson of manifest.lessons) {
  assert(lessonIds.includes(lesson.id), `Manifest lesson missing from HTML: ${lesson.id}`);
  assert(codeLinks.includes(lesson.codeUrl), `Code URL missing from HTML: ${lesson.codeUrl}`);
  assert(/^https:\/\/github\.com\/[^/]+\/[^/]+\/tree\/stage-0[1-8]/.test(lesson.codeUrl), `Invalid stage URL: ${lesson.codeUrl}`);
  assert(/^(?:BUILD|EXPERIMENT|TRACE\/DEBUG|DESIGN|REHEARSE)$/.test(lesson.exercise?.type ?? ''), `Invalid exercise type for ${lesson.id}`);
  assert(/^[a-f0-9]{40}$/.test(lesson.exercise?.gitRef ?? ''), `Invalid lesson-history ref for ${lesson.id}`);
  assert(/^[a-f0-9]{40}$/.test(lesson.exercise?.treeHash ?? ''), `Invalid lesson-history tree for ${lesson.id}`);
  assert(/^[a-f0-9]{64}$/.test(lesson.sourceContentHash ?? ''), `Invalid source-content hash for ${lesson.id}`);
}

for (const [chapter, stage] of Object.entries(manifest.stages)) {
  const target = stage.path ? path.join(root, stage.path) : root;
  assert(fs.existsSync(target), `Stage ${chapter} path does not exist: ${stage.path || '.'}`);
}

if (!packageMode)
  assert(fs.existsSync(path.join(docs, 'orderflow-verified.zip')), 'The downloadable verified solution is missing.');
assert(html.includes('orderflow-verified.zip'), 'The HTML does not link to the downloadable solution.');
const htmlWithoutSnapshotData = html.replace(/<script type="application\/json" id="snapshot-store">[\s\S]*?<\/script>/, '');
assert(!htmlWithoutSnapshotData.includes('${'), 'Unresolved template interpolation found.');
assert(!htmlWithoutSnapshotData.includes('OWNER/REPO'), 'Unresolved repository placeholder found.');

try {
  new vm.Script(`(function(){${inlineScript}})`);
}
catch (error) {
  failures.push(`Inline JavaScript syntax error: ${error.message}`);
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
  projectSnapshots: count(/class="exercise-snapshot"/g),
  bottomCompletionControls: count(/class="lesson-finish"/g),
  stages: new Set(manifest.lessons.map(lesson => lesson.codeStage)).size,
  duplicateIds: allIds.length - new Set(allIds).size,
  failures: []
}, null, 2));
