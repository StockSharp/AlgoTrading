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

        # Fix MACD initializer { Fast = N, Slow = M, Signal = K }
        # -> { Macd = { ShortMa = { Length = N }, LongMa = { Length = M } }, SignalMa = { Length = K } }
        # But also need to change type from MovingAverageConvergenceDivergence to MovingAverageConvergenceDivergenceSignal

        # Step 1: Fix initializer properties on any object
        # { Fast = N -> { Macd = { ShortMa = { Length = N } } (but only in MACD context)

        # Actually, let's be more targeted - fix the full pattern
        # new MovingAverageConvergenceDivergence { Fast = X, Slow = Y, Signal = Z }
        def fix_macd_init(m):
            body = m.group(1)
            # Extract Fast/Slow/Signal values
            fast = re.search(r'Fast\s*=\s*([^,}]+)', body)
            slow = re.search(r'Slow\s*=\s*([^,}]+)', body)
            signal = re.search(r'Signal\s*=\s*([^,}]+)', body)

            parts = []
            macd_parts = []
            if fast:
                macd_parts.append(f'ShortMa = {{ Length = {fast.group(1).strip()} }}')
            if slow:
                macd_parts.append(f'LongMa = {{ Length = {slow.group(1).strip()} }}')

            if macd_parts:
                parts.append(f'Macd = {{ {", ".join(macd_parts)} }}')

            if signal:
                parts.append(f'SignalMa = {{ Length = {signal.group(1).strip()} }}')

            return f'new MovingAverageConvergenceDivergenceSignal {{ {", ".join(parts)} }}'

        prev = c
        c = re.sub(
            r'new\s+MovingAverageConvergenceDivergence\s*\{([^}]*(?:Fast|Slow|Signal)[^}]*)\}',
            fix_macd_init, c
        )
        if c != prev:
            inc('MacdInit')

        # Also fix: new MovingAverageConvergenceDivergenceSignal { Fast = ... }
        prev = c
        c = re.sub(
            r'new\s+MovingAverageConvergenceDivergenceSignal\s*\{([^}]*(?:Fast|Slow|Signal\s*=)[^}]*)\}',
            fix_macd_init, c
        )
        if c != prev:
            inc('MacdSignalInit')

        # Fix type declarations: MovingAverageConvergenceDivergence _macd -> MovingAverageConvergenceDivergenceSignal
        # Only if the file uses Signal property on that variable
        if 'MovingAverageConvergenceDivergenceSignal' in c and 'MovingAverageConvergenceDivergence _' in c:
            prev = c
            # Replace type declaration (but not if already Signal variant)
            lines = c.split('\n')
            new_lines = []
            for line in lines:
                if ('MovingAverageConvergenceDivergence ' in line and
                    'MovingAverageConvergenceDivergenceSignal' not in line and
                    ('_macd' in line.lower() or 'private' in line)):
                    line = line.replace('MovingAverageConvergenceDivergence ', 'MovingAverageConvergenceDivergenceSignal ')
                    inc('MacdTypeDecl')
                new_lines.append(line)
            c = '\n'.join(new_lines)

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
    except Exception as e:
        print(f'Error in {f}: {e}')

for k, v in sorted(fixes.items()):
    print(f'{k}: {v} fixes')
