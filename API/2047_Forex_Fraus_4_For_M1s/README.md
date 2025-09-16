# Forex Fraus 4 For M1s Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Conversion of MQL4 strategy #13643. The original expert advisor enters trades when the Williams %R indicator touches extreme levels and then crosses back. This C# version uses the high-level API of StockSharp.

The strategy works on 1-minute candles and reacts to two key levels:
- A long signal is generated after Williams %R rises above -99.9 having been below it.
- A short signal appears when Williams %R falls below -0.1 having been above it.

Positions are closed by fixed stops, targets or trailing stop. A time filter can restrict trading to a specific intraday window.

## Details

- **Entry Criteria**  
  - Long: `WilliamsR` crosses up `BuyThreshold` (-99.9) after being lower.  
  - Short: `WilliamsR` crosses down `SellThreshold` (-0.1) after being higher.
- **Long/Short**: Both
- **Exit Criteria**  
  - Price hits stop-loss (`StopLoss`) or take-profit (`TakeProfit`)  
  - Trailing stop (`TrailingStop`) activated when enabled
- **Stops**: Step-based
- **Default Values**  
  - `WprPeriod` = 360  
  - `BuyThreshold` = -99.9  
  - `SellThreshold` = -0.1  
  - `StopLoss` = 0  
  - `TakeProfit` = 0  
  - `UseProfitTrailing` = true  
  - `TrailingStop` = 30  
  - `TrailingStep` = 1  
  - `UseTimeFilter` = false  
  - `StartHour` = 7  
  - `StopHour` = 17  
  - `Volume` = 0.01  
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filters**  
  - Category: Trend reversal  
  - Direction: Both  
  - Indicators: Williams %R  
  - Stops: Yes  
  - Complexity: Basic  
  - Timeframe: Intraday (M1)  
  - Seasonality: No  
  - Neural Networks: No  
  - Divergence: No  
  - Risk Level: Medium

