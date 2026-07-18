# Estrategia PulseWave
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza VWAP, cruce de MACD y filtro RSI.

La estrategia compra cuando el precio está por encima del VWAP, el MACD cruza al alza su línea de señal y el RSI está por debajo del umbral de sobrecompra. Sale cuando el precio cae por debajo del VWAP, el MACD cruza a la baja la línea de señal y el RSI está por encima del umbral de sobreventa.

## Detalles

- **Criterios de entrada**: Precio por encima del VWAP, MACD cruza al alza, RSI por debajo de sobrecompra.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Precio por debajo del VWAP, MACD cruza a la baja, RSI por encima de sobreventa.
- **Stops**: No.
- **Valores predeterminados**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: VWAP, MACD, RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
