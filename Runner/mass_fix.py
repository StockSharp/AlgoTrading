import os, re, glob

fixes = {
    'UtcDateTime': 0,
    'LocalDateTime': 0,
    'StochLength': 0,
    'MacdShortPeriod': 0,
    'MacdLongPeriod': 0,
    'MacdSignalPeriod': 0,
    'MacdFast': 0,
    'MacdSlow': 0,
    'SetOptions': 0,
}

os.chdir(os.path.join(os.path.dirname(__file__), '..', 'API'))

for f in glob.glob('**/CS/*.cs', recursive=True):
    try:
        with open(f, 'r', encoding='utf-8', errors='ignore') as fh:
            c = fh.read()
        orig = c

        # DateTime.UtcDateTime -> just DateTime (remove .UtcDateTime)
        prev = c
        c = re.sub(r'(\w+)\.UtcDateTime', r'\1', c)
        if c != prev:
            fixes['UtcDateTime'] += 1

        # DateTime.LocalDateTime -> just DateTime (remove .LocalDateTime)
        prev = c
        c = re.sub(r'(\w+)\.LocalDateTime', r'\1', c)
        if c != prev:
            fixes['LocalDateTime'] += 1

        # StochasticOscillator .Length -> .K.Length
        prev = c
        c = re.sub(
            r'((?:_stoch|stoch|_stochastic|stochastic|_so)\w*)\.Length',
            r'\1.K.Length', c, flags=re.IGNORECASE
        )
        if c != prev:
            fixes['StochLength'] += 1

        # MACD .ShortPeriod = N -> .ShortMa = { Length = N }
        prev = c
        c = re.sub(r'\.ShortPeriod\s*=\s*([^,;\r\n]+)', r'.ShortMa = { Length = \1 }', c)
        if c != prev:
            fixes['MacdShortPeriod'] += 1

        # MACD .LongPeriod = N -> .LongMa = { Length = N }
        prev = c
        c = re.sub(r'\.LongPeriod\s*=\s*([^,;\r\n]+)', r'.LongMa = { Length = \1 }', c)
        if c != prev:
            fixes['MacdLongPeriod'] += 1

        # MACD .SignalPeriod = N -> .SignalMa = { Length = N }
        prev = c
        c = re.sub(r'\.SignalPeriod\s*=\s*([^,;\r\n]+)', r'.SignalMa = { Length = \1 }', c)
        if c != prev:
            fixes['MacdSignalPeriod'] += 1

        # MACD .Fast -> .ShortMa (property access)
        prev = c
        c = re.sub(r'((?:_macd|macd|_signal)\w*)\.Fast\b', r'\1.ShortMa', c, flags=re.IGNORECASE)
        if c != prev:
            fixes['MacdFast'] += 1

        # MACD .Slow -> .LongMa (property access)
        prev = c
        c = re.sub(r'((?:_macd|macd|_signal)\w*)\.Slow\b', r'\1.LongMa', c, flags=re.IGNORECASE)
        if c != prev:
            fixes['MacdSlow'] += 1

        # .SetOptions( -> .SetOptimizeValues(new[] {  ... })
        prev = c
        c = re.sub(r'\.SetOptions\(([^)]+)\)', r'.SetOptimizeValues(new[] { \1 })', c)
        if c != prev:
            fixes['SetOptions'] += 1

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
    except Exception as e:
        print(f'Error in {f}: {e}')

for k, v in fixes.items():
    print(f'{k}: {v} files fixed')
