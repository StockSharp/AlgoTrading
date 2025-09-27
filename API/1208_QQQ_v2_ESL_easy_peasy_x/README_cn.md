# QQQ 策略 v2 ESL easy-peasy-x
[English](README.md) | [Русский](README_ru.md)

该策略通过主移动平均线的突破并结合趋势过滤来交易QQQ。当收盘价上穿主均线且均线向上并且价格高于长期趋势均线时买入；当收盘价下穿主均线且均线向下并且价格低于短期趋势均线时做空。

## Details

- **Entry Criteria**:
  - **Long**: 收盘价上穿主均线、均线向上、价格高于长趋势均线。
  - **Short**: 收盘价下穿主均线、均线向下、价格低于短趋势均线。
- **Long/Short**: 两者。
- **Exit Criteria**: 反向信号。
- **Stops**: 无。
- **Default Values**:
  - `Main MA Length` = 200
  - `Trend Long Length` = 100
  - `Trend Short Length` = 50
- **Filters**:
  - Category: 趋势跟随
  - Direction: 双向
  - Indicators: 移动平均线
  - Stops: 无
  - Complexity: 中等
  - Timeframe: 中期
  - Seasonality: 否
  - Neural networks: 否
  - Divergence: 否
  - Risk level: 中等

