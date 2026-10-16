import difflib
import hashlib
import pathlib
import re
import subprocess

root = pathlib.Path(__file__).resolve().parents[1]
git = r'D:\vs studio\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe'
head = subprocess.check_output([git, 'show', 'HEAD:Framework/DeepNestLib/Background.cs'], cwd=root).decode('utf-8-sig').splitlines(True)
patch = (root / 'validation/pre-solver-changes.patch').read_text(encoding='utf-8-sig').splitlines()
output = []
cursor = 0
i = 0
active = False
while i < len(patch):
    line = patch[i]
    if line.startswith('diff --git '):
        active = ' b/Framework/DeepNestLib/Background.cs' in line
    if active and line.startswith('@@ '):
        start = int(re.match(r'@@ -(\d+)(?:,\d+)? \+', line).group(1)) - 1
        output.extend(head[cursor:start])
        cursor = start
        i += 1
        while i < len(patch) and not patch[i].startswith(('@@ ', 'diff --git ')):
            item = patch[i]
            if item.startswith(' '):
                output.append(head[cursor])
                cursor += 1
            elif item.startswith('-'):
                cursor += 1
            elif item.startswith('+'):
                output.append(item[1:] + '\n')
            i += 1
        continue
    i += 1
output.extend(head[cursor:])
baseline = ''.join(output)
current = (root / 'Framework/DeepNestLib/Background.cs').read_text(encoding='utf-8-sig')
for name, next_marker in [('placeParts', '        static void ExpandPlacementBounds'),
                          ('placeParts1', '        /// <summary>\n        /// 混合启发式放置：placeParts2')]:
    def extract(text):
        start = text.index('        public static SheetPlacement ' + name + '(')
        end = text.index(next_marker, start)
        return text[start:end].rstrip()
    before, after = extract(baseline), extract(current)
    if before != after:
        print(''.join(difflib.unified_diff(before.splitlines(True), after.splitlines(True))))
        raise SystemExit(name + ' changed')
    print(name + ' unchanged: ' + hashlib.sha256(after.encode()).hexdigest())
