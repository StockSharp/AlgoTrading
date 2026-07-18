# Estrategia Multi-timeframe EMA + BB + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina dos medias móviles exponenciales, Bollinger Bands y RSI para operar rebotes. Las operaciones largas ocurren cuando el precio cierra por encima de la EMA rápida después de tocar la banda inferior. Las operaciones cortas se activan cuando el precio cierra por debajo de la EMA rápida después de perforar la banda superior y RSI está por encima de 50.

La toma de ganancias opcional cierra la posición después de un número de barras definido por el usuario si el precio se mueve favorablemente. El sistema es lo suficientemente flexible para el trading swing o intradía y admite habilitar o deshabilitar los lados largo y corto de forma independiente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre por encima de la EMA rápida con un mínimo perforando la banda inferior de Bollinger Bands.
  - **Corto**: Cierre por debajo de la EMA rápida con un máximo perforando la banda superior y RSI > 50.
- **Criterios de salida**:
  - Largo: RSI sube por encima del nivel de sobreventa.
  - Corto: El precio cierra por debajo de la banda inferior.
- **Indicadores**:
  - Dos EMAs (períodos 10 y 55)
  - Bollinger Bands (longitud 20, multiplicador 2)
  - RSI (longitud 14, sobreventa 71)
- **Stops**: Objetivo de ganancia opcional después de X barras; sin stop-loss fijo.
- **Valores predeterminados**:
  - `Ma1Period` = 10
  - `Ma2Period` = 55
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `RSILength` = 14
  - `RSIOversold` = 71
  - `XBars` = 12
- **Filtros**:
  - Reversión a la media con filtro de tendencia
  - Marco temporal: Configurable
  - Indicadores: EMA, Bollinger Bands, RSI
  - Stops: Opcional
  - Complejidad: Moderado
