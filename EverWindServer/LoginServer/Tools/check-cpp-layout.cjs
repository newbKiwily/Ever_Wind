process.chdir(require('path').resolve(__dirname, '../../..'));
const fs = require('fs');
const path = require('path');
const roots = ['EverWindServer/LoginServer'];
function files(dir) {
    return fs.readdirSync(dir, {withFileTypes: true}).flatMap(entry => entry.isDirectory()
        ? (['bin','obj'].includes(entry.name)?[]:files(path.join(dir, entry.name)))
        : /\.(h|cpp)$/.test(entry.name) ? [path.join(dir, entry.name)] : []);
}
function clean(text) {
    return text.replace(/\/\*[\s\S]*?\*\/|\/\/[^\r\n]*|"(?:\\.|[^"\\])*"|'(?:\\.|[^'\\])*'/g,
        match => match.replace(/[^\r\n]/g, ' '))
        .replace(/\benum\s+class\b/g, 'enum      ');
}
const problems = [];
let pairs = 0, headers = 0, sources = 0;
for (const file of roots.flatMap(files)) {
    const text = clean(fs.readFileSync(file, 'utf8'));
    const owner = path.basename(file, path.extname(file));
    const declarations = [...text.matchAll(/\bclass\s+(\w+)(?:\s+final)?(?:\s*:\s*[^;{]+)?\s*([;{])/g)];
    if (file.endsWith('.h')) {
        ++headers;
        for (const match of declarations)
            if (match[1] !== owner) problems.push(`${file}: foreign class declaration ${match[1]}`);
        const definitions = declarations.filter(match => match[2] === '{');
        if (definitions.length > 1) problems.push(`${file}: more than one class definition`);
        if (definitions.length) {
            ++pairs;
            if (!fs.existsSync(file.replace(/\.h$/, '.cpp'))) problems.push(`${file}: missing matching .cpp`);
        }
    } else {
        ++sources;
        if (declarations.length) problems.push(`${file}: class declaration/definition inside .cpp`);
    }
}
// Every production class file must also appear under the corresponding VS filter.
const projectPath = 'EverWindServer/LoginServer/LoginServer.vcxproj';
const project = fs.readFileSync(projectPath, 'utf8');
const filters = fs.readFileSync(projectPath + '.filters', 'utf8');
for (const file of files(roots[0])) {
    const relative = path.relative(roots[0], file).replaceAll('/', '\\');
    const element = file.endsWith('.h') ? 'ClInclude' : 'ClCompile';
    if (!project.includes(`<${element} Include="${relative}"`)) problems.push(`${file}: absent from project`);
    if (!filters.includes(`<${element} Include="${relative}"`)) problems.push(`${file}: absent from filters`);
}
if (problems.length) {
    process.stderr.write(problems.join('\n') + '\n');
    process.exitCode = 1;
} else {
    process.stdout.write(`PASS: ${pairs} class header-source pairs; ${headers} headers; ${sources} sources; no foreign declarations or .cpp-local classes; VS registration complete.\n`);
}
