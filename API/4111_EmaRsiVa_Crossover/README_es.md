# EMA RSI Estrategia de cruce adaptativo de volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación directa del asesor experto MetaTrader **EA_MARSI_1-02**. Intercambia cruces entre dos copias de
Indicador personalizado *EMA_RSI_VA* de Integer, un promedio móvil adaptable a la volatilidad impulsado por el índice de fuerza relativa (RSI).
Cada vez que la línea lenta cruza la línea rápida, el motor invierte la posición neta, reproduciendo el "volteo en cruce" original.
comportamiento respetando las mejores prácticas de manejo de pedidos de StockSharp.

## Mecánica del indicador

El paquete MQL original se envía con un indicador personalizado llamado `EMA_RSI_VA`. Calcula un precio suavizado EMA cuya efectividad
la longitud está modulada por la distancia de RSI desde su valor neutro. El puerto StockSharp introduce el
`EmaRsiVolatilityAdaptiveIndicator` clase que replica la fórmula con precisión:

1. Calcule RSI en la fuente `AppliedPrice` seleccionada con el período `RSIPeriod`.
2. Mida la distancia RSI desde 50 (`|RSI - 50| + 1`), que actúa como indicador de volatilidad.
3. Deducir un multiplicador adaptativo
`multi = (5 + 100 / RSIPeriod) / (0.06 + 0.92 * dist + 0.02 * dist^2)`.
4. Multiplique el período EMA configurado por este multiplicador para obtener una longitud dinámica `pdsx`.
5. Aplique la recursión estándar EMA con factor de suavizado `2 / (pdsx + 1)` utilizando el precio aplicado de la vela como entrada.

Las excursiones grandes de RSI acortan la ventana de suavizado y hacen que la línea reaccione más rápido; un piso RSI alarga la ventana y humedece
ruido. Tanto la línea lenta como la rápida exponen el conjunto completo de modos de precios admitidos por `StockSharp.Messages.AppliedPrice`.

## Reglas comerciales

- **Detección de señal**
  - *Venta / corto*: lento anterior < rápido anterior **y** lento actual ≥ rápido actual.
  - *Comprar / largo*: lento anterior > rápido anterior **y** lento actual ≤ rápido actual.
- **Ejecución**
  - La estrategia sólo analiza velas terminadas de la serie de velas configuradas.
  - Cuando se produce una señal, envía una orden de mercado del tamaño de cerrar la exposición existente y abrir la nueva dirección.
  - Los límites de cambio se respetan a través de `Security.MinVolume`, `Security.VolumeStep` y `Security.MaxVolume`.
- **Reversiones**
  - Las órdenes se netean de modo que una sola llamada `SellMarket` o `BuyMarket` tome la posición a través de la línea cero, igualando la
MQL comportamiento donde una señal opuesta inmediatamente invierte la operación.

## Gestión de riesgos

- `TakeProfitPoints` y `StopLossPoints` replican los campos TP/SL del asesor experto (expresados en puntos de precio). cuando cualquiera
el valor es distinto de cero, la estrategia inicia el administrador de protección de StockSharp con compensaciones de precios absolutas y `useMarketOrders = true`
para reflejar el bucle de modificación de límite/detención `OrderSend` original.
- `UseBalanceMultiplier` implementa la palanca `use_Multpl`. Cuando está activo, el volumen efectivo de la orden se convierte en
`Volume * PortfolioEquity / MaxDrawdown` con una abrazadera defensiva para restringir el intercambio.
- La llamada a la clase base `StartProtection()` aún se ejecuta para que los módulos de riesgo externos puedan adjuntar resultados finales o de equilibrio
lógica si es necesario.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Volume` | `0.1` | Tamaño base de la orden de mercado antes de aplicar cualquier multiplicador de saldo. |
| `TakeProfitPoints` | `0` | Distancia de toma de ganancias en puntos del instrumento; `0` desactiva el tramo de obtención de beneficios. |
| `StopLossPoints` | `0` | Distancia de stop-loss en puntos del instrumento; `0` desactiva la parada de protección. |
| `UseBalanceMultiplier` | `false` | Permite un tamaño de posición proporcional al saldo idéntico a `use_Multpl` en EA. |
| `MaxDrawdown` | `10000` | Denominador del multiplicador de saldo; corresponde al `Max_drawdown` del EA. |
| `SlowRsiPeriod` | `310` | RSI búsqueda retrospectiva de la línea lenta EMA_RSI_VA. |
| `SlowEmaPeriod` | `40` | Longitud base EMA para la línea lenta antes de la adaptación RSI. |
| `SlowAppliedPrice` | `Close` | Modo de precio reenviado al indicador lento. |
| `FastRsiPeriod` | `200` | RSI búsqueda retrospectiva de la línea rápida EMA_RSI_VA. |
| `FastEmaPeriod` | `50` | Longitud base EMA para la línea rápida antes de la adaptación RSI. |
| `FastAppliedPrice` | `Close` | Modo de precio reenviado al indicador rápido. |
| `CandleType` | `TimeFrame(1m)` | Serie de velas utilizadas para los cálculos. |

## Notas de implementación

- El puerto está escrito con el nivel alto API (`SubscribeCandles().Bind(...)`) de StockSharp para evitar bucles de indicadores manuales.
- Solo se procesan velas completadas que coincidan con las llamadas `CopyBuffer(..., 1, 2, ...)` en la fuente MQL.
- La normalización de volumen utiliza `Security.MinVolume`, `Security.VolumeStep` y `Security.MaxVolume`, lo que evita pedidos no válidos en
intercambios reales.
- Se omite intencionalmente una versión de Python según lo solicitado; el directorio solo contiene la implementación y documentación de C#.

El comportamiento resultante refleja la fuente EA al tiempo que expone StockSharpparámetros amigables y controles de riesgo adecuados para
Designer, Runner o cualquier host personalizado creado en StockSharp API.
