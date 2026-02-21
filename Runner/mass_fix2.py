import os, re, glob

fixes = {}

def inc(key):
    fixes[key] = fixes.get(key, 0) + 1

os.chdir(os.path.join(os.path.dirname(__file__), '..', 'API'))

for f in glob.glob('**/CS/*.cs', recursive=True):
    try:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fh:
            c = fh.read()
        orig = c

        # StochasticOscillator initializer: { Length = N } -> { K = { Length = N } }
        prev = c
        c = re.sub(
            r'(new\s+StochasticOscillator\s*\{[^}]*?)(?<![KD]\s=\s\{)\s*Length\s*=\s*(\w+)',
            r'\1 K = { Length = \2 }',
            c
        )
        if c != prev:
            inc('StochInitLength')

        # StochasticOscillator KPeriod -> (remove, K.Length is already set)
        prev = c
        c = re.sub(r',?\s*KPeriod\s*=\s*\d+', '', c)
        if c != prev:
            inc('StochKPeriod')

        # StochasticOscillator DPeriod -> D = { Length = N }
        prev = c
        c = re.sub(r'DPeriod\s*=\s*(\w+)', r'D = { Length = \1 }', c)
        if c != prev:
            inc('StochDPeriod')

        # SetDisplay with 1 arg: .SetDisplay("name") -> .SetDisplay("name", "name", "General")
        prev = c
        c = re.sub(
            r'\.SetDisplay\("([^"]+)"\)(?!\s*\.)',
            r'.SetDisplay("\1", "\1", "General")',
            c
        )
        # Also handle .SetDisplay("name"); and .SetDisplay("name")\n
        c = re.sub(
            r'\.SetDisplay\("([^"]+)"\)\s*;',
            r'.SetDisplay("\1", "\1", "General");',
            c
        )
        if c != prev:
            inc('SetDisplay1Arg')

        # SetDisplay with 2 args: .SetDisplay("name", "desc") -> .SetDisplay("name", "desc", "General")
        prev = c
        c = re.sub(
            r'\.SetDisplay\("([^"]+)",\s*"([^"]+)"\)',
            r'.SetDisplay("\1", "\2", "General")',
            c
        )
        if c != prev:
            inc('SetDisplay2Args')

        # Operator ?? on non-nullable decimal: Position ?? 0 -> Position
        # This is tricky - Position is decimal (non-nullable), so ?? doesn't work
        # But we need to be careful not to break nullable decimals
        # Skip for now - too risky

        # .SetCanOptimize(true/false) -> remove (method may not exist)
        prev = c
        c = re.sub(r'\.SetCanOptimize\((?:true|false)\)', '', c)
        if c != prev:
            inc('SetCanOptimize')

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
    except Exception as e:
        print(f'Error in {f}: {e}')

for k, v in sorted(fixes.items()):
    print(f'{k}: {v} files fixed')
