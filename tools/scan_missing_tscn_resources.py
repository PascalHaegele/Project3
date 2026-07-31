import os, re, sys
root = r"c:\Users\Zinna\Desktop\Project3"
missing = {}
for dirpath, dirnames, filenames in os.walk(root):
    for fn in filenames:
        if fn.endswith('.tscn'):
            path = os.path.join(dirpath, fn)
            with open(path, 'r', encoding='utf-8', errors='ignore') as f:
                txt = f.read()
            for m in re.finditer(r'path="res://([^"]+)".*id="([^"]+)"', txt):
                res_path = m.group(1)
                full = os.path.join(root, *res_path.split('/'))
                if not os.path.exists(full):
                    missing.setdefault(res_path, []).append(path)

if not missing:
    print('No missing ext_resource files found in .tscn files')
    sys.exit(0)

print('Missing resources:')
for res, files in missing.items():
    print(res)
    for f in files:
        print('  referenced in', os.path.relpath(f, root))
