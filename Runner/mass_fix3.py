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

        # Indicator<decimal> -> BaseIndicator (class inheritance)
        prev = c
        c = c.replace(': Indicator<decimal>', ': BaseIndicator')
        c = c.replace(': Indicator<ICandleMessage>', ': BaseIndicator')
        if c != prev:
            inc('IndicatorGeneric')

        # BaseIndicator<decimal> -> BaseIndicator
        prev = c
        c = c.replace(': BaseIndicator<decimal>', ': BaseIndicator')
        if c != prev:
            inc('BaseIndicatorGeneric')

        # LengthIndicator<decimal> -> DecimalLengthIndicator
        prev = c
        c = c.replace('LengthIndicator<decimal>', 'DecimalLengthIndicator')
        if c != prev:
            inc('LengthIndicator')

        # AwesomeOscillator .ShortPeriod -> .ShortMa.Length (only 2 files)
        prev = c
        c = re.sub(r'((?:_awesome|awesome|_ao|ao)\w*)\.ShortPeriod\s*=\s*(\w+)',
                    r'\1.ShortMa.Length = \2', c, flags=re.IGNORECASE)
        c = re.sub(r'((?:_awesome|awesome|_ao|ao)\w*)\.LongPeriod\s*=\s*(\w+)',
                    r'\1.LongMa.Length = \2', c, flags=re.IGNORECASE)
        if c != prev:
            inc('AwesomePeriod')

        # decimal? ?? decimal fix: Position ?? 0 when Position is decimal
        # Skip - too context dependent

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
    except Exception as e:
        print(f'Error in {f}: {e}')

for k, v in sorted(fixes.items()):
    print(f'{k}: {v} files fixed')
