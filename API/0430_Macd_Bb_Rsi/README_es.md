# Estrategia MACD + Bollinger Bands + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta configuración compuesta busca retrocesos contra el momentum MACD prevaleciente que se extienden más allá de las Bandas de Bollinger. Cuando el MACD es positivo pero el precio cierra por debajo de la banda inferior con un RSI sobrevendido, la estrategia compra anticipando una continuación de la tendencia. Lo opuesto aplica para posiciones cortas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `MACD > 0` y `Close < LowerBand` y `RSI < 30`
  - **Corto**: `MACD < 0` y `Close > UpperBand` y `RSI > 70`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, Bollinger Bands, RSI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
