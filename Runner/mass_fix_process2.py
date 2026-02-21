import os, re, glob

count = 0

os.chdir(os.path.join(os.path.dirname(__file__), '..', 'API'))

for f in glob.glob('**/CS/*.cs', recursive=True):
    try:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fh:
            c = fh.read()
        orig = c

        # Fix remaining 3-arg Process calls that aren't already wrapped
        # Pattern: indicator.Process(value, time, anything_else) -> indicator.Process(new DecimalIndicatorValue(indicator, value, time))
        # But skip if already has 'new DecimalIndicatorValue' or 'new CandleIndicatorValue'

        def fix_process(m):
            full = m.group(0)
            if 'new DecimalIndicatorValue' in full or 'new CandleIndicatorValue' in full:
                return full
            ind = m.group(1)
            val = m.group(2)
            time = m.group(3)
            return f'{ind}.Process(new DecimalIndicatorValue({ind}, {val}, {time}))'

        # Match indicator.Process(arg1, arg2, arg3) where arg3 is the isFinal/time/etc
        c = re.sub(
            r'(\w+)\.Process\(([^,)]+),\s*([^,)]+),\s*[^)]+\)',
            fix_process,
            c
        )

        # Also fix 5-arg Process like: _atr.Process(high, low, close, time, true)
        # These should use CandleIndicatorValue or just pass the candle
        # For now, skip these edge cases

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
            count += 1
    except Exception as e:
        print(f'Error in {f}: {e}')

print(f'Fixed remaining 3-arg Process in {count} files')
