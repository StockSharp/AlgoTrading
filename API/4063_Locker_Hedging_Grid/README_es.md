# Estrategia de cuadrícula de cobertura de casilleros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el asesor experto MetaTrader 4 **Locker.mq4**. Comienza cada ciclo con una compra de mercado y luego gestiona una red cubierta de órdenes de compra y venta. Siempre que el beneficio no realizado combinado de todas las operaciones abiertas alcanza una fracción fija del capital de la cuenta, se cierra cada posición y comienza un nuevo ciclo. Si la pérdida flotante excede la misma fracción en la dirección negativa, la estrategia agrega progresivamente órdenes de rescate a intervalos de puntos fijos, bloqueando las oscilaciones de precios alternando entradas largas y cortas.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `NeedProfitRatio` | Fracción del capital de la cartera que se debe ganar (o perder) antes de cerrar/agregar órdenes. `0.001` corresponde al 0,1% de la cuenta. | `0.001` |
| `InitialVolume` | Volumen de la primera orden de compra de mercado al comienzo de cada ciclo. | `0.5` |
| `StepVolume` | Volumen por cada orden de rescate que se agrega mientras la estrategia se encuentra en una fase de reducción. | `0.2` |
| `StepPoints` | Distancia en MetaTrader puntos entre órdenes de rescate. Convertido internamente a precio utilizando información de `Security.PriceStep` (pip). | `50` |
| `EnableRescue` | Habilita la cuadrícula de promedio cuando la pérdida flotante supera el umbral negativo. Si está deshabilitada, la estrategia solo realiza la operación inicial y espera obtener ganancias. | `true` |

## Lógica de trading

1. **Inicio del ciclo**
   - En la primera cotización comercial entrante se envía una compra de mercado con `InitialVolume`.
   - El precio de entrada se convierte en el punto de control de referencia, y tanto el rastreador de compra más alto como el de venta más bajo se restablecen a ese precio.

2. **Bloqueo de beneficios**
   - En cada tick, la estrategia suma las pérdidas y ganancias no realizadas de todos los tramos largos y cortos. Los tramos largos aportan `(price - averageBuyPrice) * longVolume`, mientras que los tramos cortos aportan `(averageSellPrice - price) * shortVolume`.
   - Una vez que el beneficio flotante alcanza `NeedProfitRatio * equity`, todas las posiciones se nivelan mediante órdenes de mercado opuestas. Un nuevo ciclo comienza después de que se confirman los llenados.

3. **Rejilla de rescate**
   - Cuando el beneficio no realizado cae por debajo de `-NeedProfitRatio * equity` y `EnableRescue` es verdadero, el sistema espera a que el precio se mueva `StepPoints` (convertido a distancia de precio). Cada nuevo máximo por encima del último punto de control genera otra compra en el mercado, mientras que cada nuevo mínimo programa una venta en el mercado. Los volúmenes siempre son iguales a `StepVolume`.
   - Los puntos de control y los extremos direccionales se actualizan después de cada orden de rescate, de modo que la próxima incorporación requiera otro aumento completo en el precio.

4. **Reinicio del ciclo**
   - Después de que los inventarios tanto largos como cortos caen a cero (confirmado a través de notificaciones comerciales propias), el punto de control y los extremos se restablecen al último precio comercial y la estrategia está lista para iniciar un nuevo ciclo con la compra inicial.

## Notas de implementación

- Utiliza `SubscribeTrades().Bind(ProcessTrade)` para trabajar con precios tick por tick, reflejando el MQL EA original que reaccionó a la oferta/demanda actual.
- Convierte MetaTrader "puntos" en precios de StockSharp mediante un tamaño de pip derivado de `Security.PriceStep`. Los símbolos citados con 3 o 5 decimales reciben el ajuste estándar *x10*.
- Realiza un seguimiento de los inventarios largos y cortos por separado dentro de `OnOwnTradeReceived`, lo que permite una exposición cubierta exactamente como la versión MT4 (las posiciones de compra y venta pueden coexistir).
- El capital de la cartera se estima a partir de `Portfolio.CurrentValue` con reservas a `CurrentBalance` o `BeginValue`. La primera lectura positiva se almacena en caché para que el umbral de ganancias permanezca estable incluso si el proveedor deja de informar el valor.
- Cada volumen de orden de mercado pasa por un asistente `AlignVolume` que respeta las restricciones `Security.VolumeStep`, `VolumeMin` y `VolumeMax`.

## Consejos de uso

- Asegúrese de que los metadatos del instrumento proporcionen un `PriceStep` correcto; de lo contrario, la conversión de punto a precio será inexacta y las distancias de la cuadrícula no coincidirán con el comportamiento de MetaTrader.
- Dado que la lógica de rescate refleja un promedio estilo martingala, elija `StepVolume` con cuidado y controle el riesgo. Aumentar tanto `StepPoints` como `StepVolume` reduce la cantidad de operaciones abiertas pero amplifica la exposición.
- Establezca `EnableRescue` en `false` para replicar una variante conservadora que simplemente espera a que la primera posición alcance el objetivo de ganancias sin promediar hacia abajo.
- Se deben realizar pruebas retrospectivas de los símbolos Forex con datos de ticks que coincidan con la granularidad original del EA.

## Diferencias con el experto MQL

- El script original intentaba cerrar pares de órdenes perfectamente compensadas cuando había más de ocho operaciones activas. Ese bloque nunca se ejecutó debido a un error en el filtro de tickets y se omitió.
- El recálculo de `StepLot` basado en pedidos preexistentes en la inicialización no se replica; Los volúmenes se controlan completamente a través de los parámetros expuestos en StockSharp.
- Los comentarios de pedidos, las ventanas emergentes de alerta y los indicadores de parada manual de EA no están presentes; la versión StockSharp se centra exclusivamente en la lógica comercial autónoma.
