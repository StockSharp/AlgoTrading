# Ticks File Strategy

This strategy reproduces the functionality of the original **TicksFile.mq5** script. It records every incoming trade together with information about the previous completed candle and the current forming candle into a CSV file.

## Parameters
- `CandleType` – candle time frame used for context. Default: `TimeSpan.FromMinutes(1).TimeFrame()`
- `Discrete` – when enabled, only the first tick of each bar is recorded. Default: `false`
- `Filler` – field separator used in the output file. Default: `;`
- `FileEnabled` – enables writing data to disk. Default: `true`

## Behavior
The strategy subscribes to three data streams:
- **Level1** to capture best bid and ask prices.
- **Trades** to receive tick prices and volumes.
- **Candles** to obtain information about completed and current bars.

For each processed tick the following fields are written in order:
```
day,mon,year,hour,min,S,close,high,low,open,spread,tick_volume,
T,ask,bid,last,volume,
N,H,M,close,high,low,open,spread,tick_volume
```
The CSV file name is generated as:
```
T_<symbol>_M<minutes>_<year>_<month>_<day>_<hour>x<minute>.csv
```
This class contains no trading logic and is intended for research or data collection.

## Usage
Attach the strategy to a security and run. The file will be created in the working directory.
