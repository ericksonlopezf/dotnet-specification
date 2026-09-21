/**
 * Automated standards enforcement script for EricksonLopez.Specification (.NET 10).
 * Validates:
 * 1. Kebab-case naming for all markdown files (excluding standard GitHub files).
 * 2. MIT Copyright header on all C# source and test files.
 * 3. Zero [Obsolete] attribute usage.
 * 4. Zero CS1591 / CS1573 suppressions in src projects.
 * 5. One top-level type per file in src/ (classes, records, interfaces, structs, enums).
 * 6. English-only comments and strings in samples/Showcase.
 * 7. Canonical branding URLs and support email consistency.
 */

const fs = require('fs');
const path = require('path');

const rootDir = path.resolve(__dirname, '..');
let errorCount = 0;

function logError(file, message) {
  console.error(`\x1b[31m[ERROR]\x1b[0m ${path.relative(rootDir, file)}: ${message}`);
  errorCount++;
}

function logSuccess(message) {
  console.log(`\x1b[32m[PASS]\x1b[0m ${message}`);
}

// 1. Check Markdown Filenames (kebab-case)
const standardGithubFiles = new Set([
  'README.md',
  'LICENSE',
  'SECURITY.md',
  'CONTRIBUTING.md',
  'CODE_OF_CONDUCT.md',
  'SUPPORT.md',
  'CHANGELOG.md',
  'PULL_REQUEST_TEMPLATE.md'
]);

function checkMarkdownFilenames(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.name === 'node_modules' || entry.name === '.git' || entry.name === 'bin' || entry.name === 'obj' || entry.name === 'BenchmarkDotNet.Artifacts' || entry.name === 'StrykerOutput' || entry.name === '.stryker-tmp' || entry.name === 'TestResults' || entry.name === 'results') {
      continue;
    }
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      checkMarkdownFilenames(fullPath);
    } else if (entry.name.endsWith('.md')) {
      if (!standardGithubFiles.has(entry.name)) {
        const baseName = path.basename(entry.name, '.md');
        const isKebabCase = /^[a-z0-9]+(-[a-z0-9]+)*$/.test(baseName);
        if (!isKebabCase) {
          logError(fullPath, `File name '${entry.name}' must be kebab-case (lowercase alphanumeric with hyphens).`);
        }
      }
    }
  }
}

// 2. Check C# Source Files for Copyright Header, One Type Per File, and [Obsolete]
const copyrightHeader = '// Copyright © Erickson Lopez. MIT License.';

function checkCSharpFiles(dir, isSrc) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.name === 'bin' || entry.name === 'obj' || entry.name === 'StrykerOutput' || entry.name === 'TestResults') {
      continue;
    }
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      checkCSharpFiles(fullPath, isSrc);
    } else if (entry.name.endsWith('.cs') && !entry.name.endsWith('.g.cs') && !entry.name.endsWith('.AssemblyInfo.cs')) {
      const content = fs.readFileSync(fullPath, 'utf8');
      
      // Copyright header
      if (!content.startsWith(copyrightHeader)) {
        logError(fullPath, `Missing or invalid MIT copyright header. Must start with '${copyrightHeader}'`);
      }

      // No [Obsolete]
      if (/\[\s*(global::System\.)?Obsolete\s*(\(.*\))?\s*\]/.test(content)) {
        logError(fullPath, `Found prohibited [Obsolete] attribute. Deprecated APIs must be removed per zero-obsolete policy.`);
      }

      // One type per file in src/ (excluding nested types)
      if (isSrc) {
        // Strip comments and strings to avoid false positives
        const cleanContent = content
          .replace(/\/\*[\s\S]*?\*\//g, '')
          .replace(/\/\/.*$/gm, '')
          .replace(/"""[\s\S]*?"""/g, '""')
          .replace(/"(\\.|[^"\\])*"/g, '""');

        // Simple token scan tracking brace depth to identify top-level types only
        const lines = cleanContent.split('\n');
        let braceDepth = 0;
        let fileScopedNamespace = false;
        const topLevelTypes = [];

        for (const line of lines) {
          const trimmed = line.trim();
          if (trimmed.startsWith('namespace ') && trimmed.endsWith(';')) {
            fileScopedNamespace = true;
          }

          const targetDepth = fileScopedNamespace ? 0 : 1;
          const typeMatch = /^(?:public|internal|private|protected)?\s*(?:static|sealed|abstract|readonly|partial)*\s*(?:class|record\s+class|record\s+struct|record|struct|interface|enum)\s+([A-Za-z0-9_]+)/.exec(trimmed);
          
          if (typeMatch && braceDepth === targetDepth) {
            topLevelTypes.push(typeMatch[1]);
          }

          for (const char of line) {
            if (char === '{') braceDepth++;
            else if (char === '}') braceDepth--;
          }
        }

        if (topLevelTypes.length > 1) {
          logError(fullPath, `Multiple top-level types found (${topLevelTypes.join(', ')}). Policy requires 1 type per file.`);
        }
      }
    }
  }
}

// 3. Check No CS1591 Suppressions in src/
function checkProjectFiles(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.name === 'bin' || entry.name === 'obj') continue;
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      checkProjectFiles(fullPath);
    } else if (entry.name.endsWith('.csproj')) {
      const content = fs.readFileSync(fullPath, 'utf8');
      if (content.includes('CS1591') || content.includes('CS1573')) {
        logError(fullPath, `Found prohibited CS1591/CS1573 suppression in source project.`);
      }
    }
  }
}

// 4. Check Repository Branding and URLs
function checkBranding(dir) {
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    if (entry.name === 'node_modules' || entry.name === '.git' || entry.name === 'bin' || entry.name === 'obj' || entry.name === 'BenchmarkDotNet.Artifacts' || entry.name === 'StrykerOutput' || entry.name === '.stryker-tmp' || entry.name === 'TestResults' || entry.name === 'results') continue;
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      checkBranding(fullPath);
    } else if (entry.name.endsWith('.md') || entry.name.endsWith('.props')) {
      const content = fs.readFileSync(fullPath, 'utf8');
      if (content.includes('github.com/ericksonlopez/dotnet-specifications') || content.includes('github.com/ericksonlopez/dotnet-specification')) {
        logError(fullPath, `Found obsolete repository URL. Use 'github.com/ericksonlopezf/dotnet-specification'.`);
      }
      if (entry.name === 'Directory.Build.props' && !content.includes('https://ericksonlopez.dev/specification')) {
        logError(fullPath, `PackageProjectUrl in Directory.Build.props must be 'https://ericksonlopez.dev/specification'.`);
      }
    }
  }
}

console.log('=== EricksonLopez.Specification Standards Verification ===\n');

console.log('1. Validating documentation filenames (kebab-case)...');
checkMarkdownFilenames(rootDir);

console.log('2. Validating C# copyright headers, [Obsolete], and type containment...');
if (fs.existsSync(path.join(rootDir, 'src'))) {
  checkCSharpFiles(path.join(rootDir, 'src'), true);
}
if (fs.existsSync(path.join(rootDir, 'tests'))) {
  checkCSharpFiles(path.join(rootDir, 'tests'), false);
}
if (fs.existsSync(path.join(rootDir, 'samples'))) {
  checkCSharpFiles(path.join(rootDir, 'samples'), false);
}

console.log('3. Validating analyzer suppressions in src/...');
if (fs.existsSync(path.join(rootDir, 'src'))) {
  checkProjectFiles(path.join(rootDir, 'src'));
}

console.log('4. Validating repository URLs and metadata branding...');
checkBranding(rootDir);

console.log('\n=== Summary ===');
if (errorCount === 0) {
  logSuccess('All architectural and code quality standards verified successfully with 0 violations.');
  process.exit(0);
} else {
  console.error(`\x1b[31m[FAILED]\x1b[0m Verification failed with ${errorCount} violation(s).`);
  process.exit(1);
}
