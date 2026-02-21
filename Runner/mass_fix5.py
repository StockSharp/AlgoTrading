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

        # Revert new ExponentialMovingAverage -> new EMA (since EMA alias exists)
        prev = c
        c = re.sub(r'\bnew ExponentialMovingAverage\b', 'new EMA', c)
        if c != prev:
            inc('RevertEMA')

        # Revert new SimpleMovingAverage -> new SMA
        prev = c
        c = re.sub(r'\bnew SimpleMovingAverage\b', 'new SMA', c)
        if c != prev:
            inc('RevertSMA')

        # Also fix type declarations that use full names when variable was originally alias
        # ExponentialMovingAverage -> EMA in declarations (private, local vars)
        # Only if file also uses EMA somewhere already
        # Actually skip this - just fixing the new expressions is enough

        # Fix PositionAvgPrice -> PositionPrice (which we added in Strategy_Compat.cs)
        prev = c
        c = c.replace('PositionAvgPrice', 'PositionPrice')
        if c != prev:
            inc('PositionAvgPrice')

        # Fix .Offset on DateTime -> remove it (DateTime doesn't have .Offset)
        # candle.ServerTime.Offset -> DateTimeOffset.Zero (or just remove usage)
        # Actually candle.ServerTime is DateTime, .Offset doesn't exist
        # Most common pattern: time.Offset or candle.ServerTime.Offset
        prev = c
        c = re.sub(r'candle\.ServerTime\.Offset\b', 'TimeSpan.Zero', c)
        if c != prev:
            inc('ServerTimeOffset')

        # Fix DateTime.Offset -> remove
        prev = c
        c = re.sub(r'(\w+)\.ServerTime\.Offset\b', 'TimeSpan.Zero', c)
        if c != prev:
            inc('DateTimeOffset')

        # Fix .Log event used incorrectly (BaseLogSource.Log is an event, can only use += or -=)
        # Common error: this.Log(...) -> this.AddLog(LogLevels.Info, ...)
        # This is too complex for regex, skip

        # Fix OnSubscriptionsNeeded -> GetWorkingSecurities (common override error)
        prev = c
        c = c.replace('override void OnSubscriptionsNeeded()', 'override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()')
        if c != prev:
            inc('OnSubscriptionsNeeded')

        if c != orig:
            with open(f, 'w', encoding='utf-8') as fh:
                fh.write(c)
    except Exception as e:
        print(f'Error in {f}: {e}')

for k, v in sorted(fixes.items()):
    print(f'{k}: {v} files fixed')
