# SpectrAnalysis WPR Strategy

This strategy is converted from the MQL5 expert *Exp_i-SpectrAnalysis_WPR*.
It analyzes the direction of the Williams %R indicator and opens or closes
positions according to indicator turns.

## Logic

1. Subscribe to candles of the selected timeframe.
2. Calculate Williams %R with the configured period.
3. Keep the last two indicator values to detect upward or downward direction.
4. When the indicator turns upward and long entries are allowed:
   - Close short positions if enabled.
   - Open a new long position.
5. When the indicator turns downward and short entries are allowed:
   - Close long positions if enabled.
   - Open a new short position.

Only finished candles are processed. The strategy does not use complex
historical queries and relies on high-level API bindings.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `Candle Type` | Timeframe of the candles used for calculations | `4h` |
| `WPR Period` | Period of the Williams %R indicator | `13` |
| `Allow Long Entry` | Permit opening long positions | `true` |
| `Allow Short Entry` | Permit opening short positions | `true` |
| `Allow Long Exit` | Permit closing long positions | `true` |
| `Allow Short Exit` | Permit closing short positions | `true` |

## Notes

The original MQL version applied spectral analysis to the Williams %R output.
This C# conversion uses the standard Williams %R indicator and replicates the
signal logic by tracking recent indicator values.
