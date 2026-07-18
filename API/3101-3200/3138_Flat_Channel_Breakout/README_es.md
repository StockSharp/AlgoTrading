# Estrategia de Flat Channel Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Flat Channel Strategy** es una traducción en C# del asesor experto MetaTrader 5 *Flat Channel (edición de barabashkakvn)*. Mantiene el flujo de trabajo original: una desviación estándar suavizada resalta las compresiones de volatilidad, los precios más altos y más bajos dentro de la compresión definen un canal horizontal, y se colocan órdenes de stop pendientes justo fuera de ese rango. Cuando el mercado rompe, la estrategia se une al movimiento con niveles de stop-loss y take-profit predefinidos y puede opcionalmente trailear el stop a medida que la posición gana beneficio.

## Cómo funciona

1. **Detección de compresión de volatilidad** – Un indicador `StandardDeviation` con longitud `StdDevPeriod` se suaviza por una corta `SimpleMovingAverage` de `SmoothingLength`. Siempre que la serie suavizada imprime `FlatBars` valores consecutivos no crecientes, el mercado se trata como plano y las banderas de las órdenes se recargan.
2. **Construcción del canal** – Una vez confirmado un plano, la estrategia solicita el máximo más alto y el mínimo más bajo durante las últimas `max(ChannelLookback, FlatBars + 1)` velas usando los indicadores integrados `Highest`/`Lowest`. La altura del canal se filtra por `ChannelMinPips`/`ChannelMaxPips` después de convertir pips en unidades de precio a través de `PipSize` (o el tamaño del tick detectado cuando el parámetro se deja en cero).
3. **Órdenes pendientes** – Si la posición actual es plana y el trading está permitido, la estrategia envía una orden de compra stop en `high + IndentPips` y una orden de venta stop en `low − IndentPips`. Cada orden recuerda los niveles protectores calculados en el momento del envío.
4. **Ejecución del Breakout** – Cuando una orden pendiente se ejecuta, la orden pendiente opuesta se cancela automáticamente. El precio ejecutado se convierte en el ancla de entrada para la lógica de trailing stop y las distancias de stop-loss/take-profit memorizadas se activan.
5. **Gestión de posición** – La posición activa se supervisa en cada vela completada. Si el precio toca el nivel de stop-loss o take-profit, la estrategia emite una salida de mercado. Cuando `TrailingStopPips` es mayor que cero, el stop se arrastra hacia adelante una vez que el precio de cierre se mueve al menos `TrailingStopPips + TrailingStepPips` desde el precio de ejecución.
6. **Filtro de sesión** – Cuando `UseTradingHours` está habilitado, la lógica de Breakout solo funciona entre `StartHour` (inclusive) y `EndHour` (exclusivo). Las sesiones nocturnas son compatibles permitiendo `StartHour > EndHour`.

## Gestión de riesgo

- **Protección dinámica o fija** – Configure `StopLossPips` / `TakeProfitPips` en valores positivos para usar distancias fijas (en pips). Mantenerlos en cero cambia a dimensionamiento dinámico basado en la altura del canal y los coeficientes `DynamicStopMultiplier` / `DynamicTakeMultiplier`.
- **Trailing stop** – Habilite `TrailingStopPips` para seguir el movimiento una vez que la operación está en beneficio. La lógica de trailing respeta `TrailingStepPips` para evitar micro ajustes.
- **Límite de posición** – `MaxPositions` limita la exposición agregada a `MaxPositions × TradeVolume`. Si se alcanza ese umbral, no se envían nuevas órdenes pendientes hasta que la exposición disminuya.
- **Filtros direccionales** – `UseBuy` y `UseSell` permiten que la estrategia opere en modos solo-Breakout, solo-Breakout descendente o bidireccional.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `TradeVolume` | `1` | Volumen enviado con cada orden pendiente. |
| `PipSize` | `0.0001` | Anulación manual del tamaño del pip. Dejar en cero para usar el tamaño del tick del instrumento (con ajuste automático de 3/5 dígitos). |
| `StdDevPeriod` | `46` | Lookback para la `StandardDeviation` base. |
| `SmoothingLength` | `3` | Longitud de la media móvil aplicada a la serie de volatilidad. |
| `FlatBars` | `3` | Número de valores de volatilidad suavizada consecutivos no crecientes requeridos para recargar las órdenes de Breakout. |
| `ChannelLookback` | `5` | Velas usadas para medir el máximo más alto y el mínimo más bajo una vez detectado un plano. Se compara automáticamente con `FlatBars + 1`. |
| `ChannelMinPips` | `15` | Altura mínima del canal (en pips). Establezca en `0` para deshabilitar el límite inferior. |
| `ChannelMaxPips` | `105` | Altura máxima del canal (en pips). Establezca en `0` para deshabilitar el límite superior. |
| `DynamicStopMultiplier` | `1` | Multiplicador de altura del canal para el cálculo dinámico del stop-loss cuando `StopLossPips = 0`. |
| `DynamicTakeMultiplier` | `1` | Multiplicador de altura del canal para el cálculo dinámico del take-profit cuando `TakeProfitPips = 0`. |
| `StopLossPips` | `0` | Distancia fija del stop-loss en pips. Anula la fórmula dinámica cuando es positivo. |
| `TakeProfitPips` | `0` | Distancia fija del take-profit en pips. Anula la fórmula dinámica cuando es positivo. |
| `IndentPips` | `0` | Desplazamiento adicional (en pips) más allá de los límites del canal para órdenes pendientes. |
| `TrailingStopPips` | `5` | Distancia del trailing stop en pips. Establezca en `0` para deshabilitar el trailing. |
| `TrailingStepPips` | `5` | Paso mínimo (en pips) requerido para mover el trailing stop. |
| `UseBuy` | `true` | Habilitar Breakouts largos (orden de compra stop). |
| `UseSell` | `true` | Habilitar Breakouts cortos (orden de venta stop). |
| `MaxPositions` | `5` | Número máximo de volúmenes base permitidos en la posición agregada. |
| `UseTradingHours` | `true` | Habilitar el filtro de sesión de trading. |
| `StartHour` | `0` | Hora de inicio de sesión (inclusive). |
| `EndHour` | `23` | Hora de fin de sesión (exclusivo). |
| `CandleType` | `H1` | Serie de velas usada para cálculos (por defecto marco temporal de 1 hora). |

## Notas

- La estrategia opera exclusivamente en velas completadas a través de la API de alto nivel `SubscribeCandles().Bind(...)`, coincidiendo con el comportamiento determinista esperado del EA original.
- Los precios protectores se normalizan a través de `Security.ShrinkPrice` para respetar los tamaños de tick de la bolsa.
- Cuando ambas órdenes pendientes están activas y una de ellas se ejecuta, la orden opuesta se cancela inmediatamente para que solo pueda estar abierta una posición de Breakout a la vez.
