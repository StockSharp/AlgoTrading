import os, re, glob

count = 0

os.chdir(os.path.join(os.path.dirname(__file__), '..', 'API'))

for f in glob.glob('**/CS/*.cs', recursive=True):
    try:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fh:
            c = fh.read()
        orig = c

        # Fix 3-arg Process: indicator.Process(value, time, isFinal) ->
        # indicator.Process(new DecimalIndicatorValue(indicator, value, time))
        # The third arg (isFinal) is dropped as DecimalIndicatorValue doesn't use it
        c = re.sub(
            r'(\w+)\.Process\(([^,]+),\s*([^,]+),\s*(?:true|false|[^)]+State\s*==\s*CandleStates\.Finished)\)',
            r'\1.Process(new DecimalIndicatorValue(\1, \2, \3))',
            c
        )

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
            count += 1
    except Exception as e:
        print(f'Error in {f}: {e}')

print(f'Fixed 3-arg Process in {count} files')
