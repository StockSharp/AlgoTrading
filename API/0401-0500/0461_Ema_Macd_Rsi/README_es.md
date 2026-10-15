# Estrategia EMA MACD RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina el filtro de tendencia con EMA, cruces de MACD y niveles de RSI.

Compra cuando la EMA rápida está por encima de la EMA lenta, MACD cruza por encima de su línea de señal, y RSI está entre RsiBuyLevel y 70. Vende cuando la EMA rápida está por debajo de la EMA lenta, MACD cruza por debajo de su línea de señal, y RSI está entre 30 y RsiSellLevel.

## Detalles

- **Criterios de entrada**: Filtro de tendencia con EMA, cruce de MACD, nivel de RSI.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `RsiLength` = 14
  - `RsiBuyLevel` = 45m
  - `RsiSellLevel` = 55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, MACD, RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
