# Volume Divergence
[Русский](README_ru.md) | [中文](README_cn.md)
 
Volume Divergence looks for discrepancies between price movement and trading volume. If price falls but volume increases, it may signal accumulation; if price rises with strong volume, it may signal distribution.

Testing indicates an average annual return of about 43%. It performs best in the stocks market.

The strategy enters long when falling prices are accompanied by rising volume, and enters short when rising prices pair with heavy volume. Exits rely on a moving average crossover.

This approach attempts to trade against unsustainable moves.

## Details

- **Entry Criteria**: Price and volume moving in opposite directions.
- **Long/Short**: Both directions.
- **Exit Criteria**: Price crosses MA or stop.
- **Stops**: Yes.
- **Default Values**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Divergence
  - Direction: Both
  - Indicators: Volume, MA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes
  - Risk Level: Medium

