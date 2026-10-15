# Estrategia de Cruce de Dos iMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el clásico asesor experto de MetaTrader 5 **"Crossing of two iMA"** a la API de alto nivel de StockSharp. Opera cuando dos medias móviles configurables se cruzan y puede requerir opcionalmente confirmación de una tercera media móvil que actúa como filtro direccional. La implementación mantiene la flexibilidad original al soportar dimensionamiento de posiciones manual o basado en riesgo, offsets estilo entrada pendiente y un trailing stop con paso definido por el usuario.

La conversión procesa señales en el cierre de cada candle finalizado, replicando cómo el experto MQL5 espera una nueva barra. El comportamiento de órdenes pendientes (`PriceLevelPips`) se simula internamente monitoreando máximos y mínimos de candles, por lo que no se envían órdenes stop/límite reales. Un trigger pendiente largo se activa cuando la barra alcanza el precio elegido para entradas buy stop o baja al precio para entradas buy limit, y la misma lógica simétrica se aplica para configuraciones cortas.

## Reglas de trading

- **Indicadores**
  - Media móvil `First` (periodo, desplazamiento y método son configurables).
  - Media móvil `Second` (también completamente configurable).
  - Media móvil `Third` opcional usada como filtro (`UseThirdMovingAverage = true`).
- **Criterios de entrada**
  - **Cruce primario (barras 0 y 1)**
    - **Largo**: la primera MA cruza por encima de la segunda MA en la barra actual mientras estaba por debajo en la barra anterior. Si el filtro está activo, la tercera MA debe mantenerse por debajo de la primera MA para validar la ruptura larga.
    - **Corto**: la primera MA cruza por debajo de la segunda MA y, si el filtro está habilitado, la tercera MA debe mantenerse por encima de la primera MA.
  - **Cruce de respaldo (barras 0 y 2)**
    - Realiza una búsqueda adicional hacia atrás para capturar cruces rápidos ocurridos entre las dos barras anteriores. La estrategia ignora esta señal si ya se abrió otra operación dentro de las últimas tres barras (igual que la búsqueda de historial de MQL5).
- **Dirección**: tanto largo como corto.
- **Stops y objetivos**
  - El stop loss y take profit se expresan en pips. Se convierten a offsets de precio basados en el tamaño del tick del instrumento y se ajustan para precios de 3/5 dígitos igual que el EA original.
  - El trailing stop se activa solo cuando `TrailingStopPips > 0`. Mueve el stop por la distancia de trailing una vez que el precio avanza al menos `TrailingStepPips` más allá del nivel de stop anterior.
- **Modo de orden pendiente (`PriceLevelPips`)**
  - `0`: entrar inmediatamente a mercado.
  - `< 0`: simular órdenes stop (buy stop por encima del precio, sell stop por debajo). El stop loss y take profit se desplazan en el mismo offset.
  - `> 0`: simular órdenes límite (buy limit por debajo del precio, sell limit por encima). Los niveles de protección se desplazan en consecuencia.

## Gestión de capital

- `UseFixedVolume = true` replica el modo de lote manual del EA. La estrategia simplemente usa `Volume` (y cierra posiciones opuestas antes de abrir una nueva).
- Cuando `UseFixedVolume = false`, la estrategia asigna riesgo como `Portfolio.CurrentValue * RiskPercent / 100`. El tamaño de la orden se convierte en `riskAmount / stopDistance`. Si no se proporciona stop loss (`StopLossPips = 0`), la distancia de riesgo calculada es cero, por lo que la estrategia se niega a abrir una posición — idéntico al comportamiento original de `MoneyFixedRisk` que devuelve cero lotes.

## Lógica de trailing

- Las posiciones largas tracen el stop a `Close - TrailingStopPips * pipValue` una vez que el precio se ha movido al menos `TrailingStepPips` más allá del stop anterior. El valor de trailing siempre se mueve hacia arriba y nunca afloja el stop.
- Las posiciones cortas reflejan este comportamiento moviendo el stop a `Close + TrailingStopPips * pipValue` cuando el precio avanza suficientemente a su favor.
- El take profit y el stop inicial se verifican antes de los ajustes de trailing, asegurando que las salidas coincidan con las prioridades del EA original.

## Parámetros predeterminados

- Primera MA: longitud `5`, desplazamiento `3`, método `Smoothed`.
- Segunda MA: longitud `8`, desplazamiento `5`, método `Smoothed`.
- Filtro de tercera MA: habilitado, longitud `13`, desplazamiento `8`, método `Smoothed`.
- Controles de riesgo: stop loss `50` pips, take profit `50` pips, trailing `10` pips con paso de `4` pips.
- Gestión de capital: `UseFixedVolume = true`, `RiskPercent = 5` para el modo de dimensionamiento alternativo.
- Offset pendiente: `0` pips (ejecución a mercado).
- Tipo de candle: marco temporal de 1 minuto (puede cambiarse para coincidir con el periodo del gráfico original).

## Notas de implementación

- Los parámetros `shift` de la media móvil retrasan los valores de señal exactamente por el número configurado de barras, por lo que el trazado en gráficos StockSharp coincide con el desplazamiento visual MT5.
- La estrategia almacena solo el estado mínimo requerido (actual, anterior y dos barras atrás) para satisfacer la lógica "barras [0], [1], [2]" de MQL5. No se recrean colecciones históricas más allá de ese buffer.
- Las entradas pendientes se borran cuando aparece una nueva señal, replicando la llamada `DeleteAllOrders()` del EA.
- Dado que StockSharp ejecuta órdenes de forma asíncrona, el precio de entrada registrado para cálculos de trailing y objetivo usa el precio de trigger previsto. Los backtests por tanto reproducen la lógica del EA original en datos de candles sin depender de fills a nivel de tick.
