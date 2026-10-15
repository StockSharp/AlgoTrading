# Desviación de Tendencia BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina cruces DMI con Bollinger Bands y confirmaciones de Momentum, MACD, SuperTrend y Aroon. La estrategia busca desviaciones de precio dentro de una tendencia y entra cuando múltiples señales se alinean.

## Detalles

- **Criterios de entrada**: +DI cruzando por encima de -DI, precio por debajo de la banda superior de Bollinger y cualquier confirmación de Momentum/MACD/SuperTrend/Aroon.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `DmiPeriod` = 15
  - `BbLength` = 13
  - `BbMultiplier` = 2.3
  - `MomentumLength` = 10
  - `AroonLength` = 5
  - `MacdFast` = 15
  - `MacdSlow` = 200
  - `MacdSignal` = 25
  - `AtrPeriod` = 200
  - `SuperTrendFactor` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: DMI, Bollinger Bands, Momentum, MACD, SuperTrend, Aroon
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
