# Estrategia de Reversión por Rebote en Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que captura reversiones cuando el precio rebota desde las Bollinger Bands con confirmación de MACD y volumen. El sistema limita las entradas a cinco operaciones por día y aplica stop loss y take profit de porcentaje fijo.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close[1] < LowerBand[1] && Close > LowerBand && MACD > Signal && Volume >= AvgVolume * VolumeFactor`
  - Corto: `Close[1] > UpperBand[1] && Close < UpperBand && MACD < Signal && Volume >= AvgVolume * VolumeFactor`
- **Largo/Corto**: Ambos
- **Stops**: Take profit y stop loss en porcentaje
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BbStdDev` = 2m
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `VolumePeriod` = 20
  - `VolumeFactor` = 1m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, MACD, Volumen
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
