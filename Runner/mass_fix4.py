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

        # candle.Time -> candle.ServerTime (or candle.OpenTime)
        prev = c
        c = re.sub(r'candle\.Time\b', 'candle.ServerTime', c)
        if c != prev:
            inc('CandleTime')

        # .ForEach( -> .Bind(
        prev = c
        c = c.replace('.ForEach(', '.Bind(')
        if c != prev:
            inc('ForEach')

        # new EMA( or new EMA { -> new ExponentialMovingAverage( or {
        prev = c
        c = re.sub(r'\bnew EMA\b', 'new ExponentialMovingAverage', c)
        c = re.sub(r'\bnew SMA\b', 'new SimpleMovingAverage', c)
        if c != prev:
            inc('EMA_SMA')

        # CandlePrice property on indicators - remove
        prev = c
        c = re.sub(r',?\s*CandlePrice\s*=\s*CandlePrice\.\w+,?', '', c)
        if c != prev:
            inc('CandlePrice')

        # .High / .Low / .Open / .Close on ICandleMessage -> .HighPrice etc
        # Only match if preceded by candle-like variable
        prev = c
        c = re.sub(r'(candle|c|bar|_candle|candles\[\w+\]|_candles\[\w+\])\.High\b(?!Price)', r'\1.HighPrice', c)
        c = re.sub(r'(candle|c|bar|_candle|candles\[\w+\]|_candles\[\w+\])\.Low\b(?!Price)', r'\1.LowPrice', c)
        c = re.sub(r'(candle|c|bar|_candle|candles\[\w+\]|_candles\[\w+\])\.Open\b(?!Price|Time)', r'\1.OpenPrice', c)
        c = re.sub(r'(candle|c|bar|_candle|candles\[\w+\]|_candles\[\w+\])\.Close\b(?!Price|Time)', r'\1.ClosePrice', c)
        if c != prev:
            inc('CandleFields')

        # DecimalIndicatorValue with 2 args (missing time) - common error
        # Pattern: new DecimalIndicatorValue(indicator, value) -> need time
        # Can't fix automatically without knowing the time source - skip

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
    except Exception as e:
        print(f'Error in {f}: {e}')

for k, v in sorted(fixes.items()):
    print(f'{k}: {v} files fixed')
