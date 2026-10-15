# Estrategia Get Rich or Die Trying GBP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia StockSharp reproduce el comportamiento del experto MetaTrader «Get Rich or Die Trying GBP». Se centra en el activo solapamiento entre las sesiones de Nueva York y Londres y espera un breve estallido de desequilibrio direccional en velas de 1 minuto. El algoritmo cuenta cuántas de las últimas barras cerraron por debajo de su apertura (etiquetadas como "up" en el código original) frente al número que cerró por encima de su apertura. Cuando los conteos difieren, la estrategia busca una oportunidad para operar contra el lado más débil durante los primeros cinco minutos de las ventanas de tiempo elegidas.

El sistema siempre opera una sola posición a la vez. Aplica un enfriamiento de 61 segundos después de cada entrada, lleva tanto un take-profit primario fijo como un objetivo secundario más ajustado, y opcionalmente sigue el stop una vez que el precio se mueve suficientemente a favor. Todas las distancias se expresan en pips, convertidas internamente usando el paso de precio del valor (con un multiplicador ×10 para cotizaciones de 3 y 5 decimales) para que la lógica coincida con la implementación MT5 original.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Más velas con `Open > Close` que con `Open < Close` sobre las últimas `CountBars` velas de 1 minuto, tiempo actual dentro de los primeros cinco minutos de `22:00 + AdditionalHour` o `19:00 + AdditionalHour`, sin posición abierta, y el enfriamiento de 61 segundos cumplido.
  - **Corto**: Más velas con `Open < Close` que con `Open > Close` bajo las mismas restricciones de tiempo y enfriamiento.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Take-profit primario en `TakeProfitPips` desde la entrada y stop-loss en `StopLossPips`.
  - Salida anticipada cuando el beneficio flotante alcanza `SecondaryTakeProfitPips`.
  - Stop trailing opcional que se activa una vez que el precio avanza más allá de `TrailingStopPips + TrailingStepPips`, desplazando el stop por `TrailingStopPips` respetando el paso de trailing.
- **Stops**: Stop-loss fijo, take-profit fijo, take-profit secundario y stop trailing opcional.
- **Filtro de tiempo**: Opera solo durante los primeros cinco minutos después de las horas ajustadas 19:00 y 22:00.
- **Enfriamiento**: Espera al menos 61 segundos después de cada entrada antes de permitir una nueva operación.
- **Valores predeterminados**:
  - `StopLossPips` = 100
  - `TakeProfitPips` = 100
  - `SecondaryTakeProfitPips` = 40
  - `TrailingStopPips` = 30
  - `TrailingStepPips` = 5
  - `CountBars` = 18
  - `AdditionalHour` = 2
  - `MaxPositions` = 1000
  - `CandleType` = marco temporal de 1 minuto
- **Notas**:
  - `MaxPositions` se conserva por compatibilidad con el experto original, pero este port mantiene solo una posición activa a la vez.
  - La conversión de pips se adapta automáticamente a símbolos FX de 3 y 5 decimales multiplicando el paso de precio por 10.
  - La lógica del stop trailing refleja la versión MT5: no se mueve hasta que el precio mejora más allá de la distancia de trailing y el paso de trailing.
