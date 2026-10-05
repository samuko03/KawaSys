import re, glob

files = glob.glob('routers/*.py') + ['main.py']

for f in files:
    txt = open(f, encoding='utf-8').read()

    def fix(m):
        quote = m.group(1)
        name = m.group(2)
        body = m.group(3)
        body2 = re.sub(r"""['"]request['"]\s*:\s*request\s*,?\s*""", '', body).strip().strip(',').strip()
        return f'templates.TemplateResponse(request, {quote}{name}{quote}, {{{body2}}})'

    new = re.sub(
        r"templates\.TemplateResponse\((['\"])([\w/\.]+)\1\s*,\s*\{([^}]*)\}\)",
        fix, txt, flags=re.DOTALL
    )
    if new != txt:
        open(f, 'w', encoding='utf-8').write(new)
        print(f'Fixed: {f}')
    else:
        print(f'No change: {f}')
