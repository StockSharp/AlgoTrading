import os, re, glob

count = 0

os.chdir(os.path.join(os.path.dirname(__file__), '..', 'API'))

for f in glob.glob('**/CS/*.cs', recursive=True):
    try:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fh:
            c = fh.read()
        orig = c

        # Fix remaining 3-arg Process calls with null-forgiving (!) and array indexers
        # Pattern: _var!.Process(v, t, b) or _var[i].Process(v, t, b)
        def fix_process3(m):
            full = m.group(0)
            if 'new DecimalIndicatorValue' in full or 'new CandleIndicatorValue' in full:
                return full
            ind = m.group(1)
            val = m.group(2).strip()
            time = m.group(3).strip()
            # Remove the ! for the constructor arg
            ind_clean = ind.rstrip('!')
            return f'{ind}.Process(new DecimalIndicatorValue({ind_clean}, {val}, {time}))'

        # Match indicator with optional ! and/or [index]
        c = re.sub(
            r'([\w.]+(?:\[\w+\])?!?)\.Process\(([^,)]+),\s*([^,)]+),\s*[^)]+\)',
            fix_process3,
            c
        )

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
            count += 1
    except Exception as e:
        print(f'Error in {f}: {e}')

print(f'Fixed remaining Process calls in {count} files')
