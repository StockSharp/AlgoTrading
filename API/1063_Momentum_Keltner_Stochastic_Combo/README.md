# Momentum Keltner Stochastic Combo Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combining momentum comparison with a Keltner-based stochastic oscillator.  
Positions are scaled dynamically based on equity and protected by a fixed stop loss.

## Details

- **Entry Criteria**:  
  - Long: `Momentum > 0` and `KeltnerStoch < Threshold`  
  - Short: `Momentum < 0` and `KeltnerStoch > Threshold`
- **Long/Short**: Both  
- **Exit Criteria**:  
  - Long: `KeltnerStoch > Threshold`  
  - Short: `KeltnerStoch < Threshold`
- **Stops**: Fixed `SlPoints` below/above entry  
- **Default Values**:  
  - `MomLength` = 7  
  - `KeltnerLength` = 9  
  - `KeltnerMultiplier` = 0.5  
  - `Threshold` = 99  
  - `AtrLength` = 20  
  - `SlPoints` = 1185  
  - `EnableScaling` = true  
  - `BaseContracts` = 1  
  - `InitialCapital` = 30000  
  - `EquityStep` = 150000  
  - `MaxContracts` = 15  
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:  
  - Category: Trend following  
  - Direction: Both  
  - Indicators: Momentum, EMA, ATR  
  - Stops: Yes  
  - Complexity: Intermediate  
  - Timeframe: Mid-term  
  - Seasonality: No  
  - Neural Networks: No  
  - Divergence: No  
  - Risk Level: Medium

