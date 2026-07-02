# Pedro Mod Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una versión StockSharp del asesor experto **Pedroxxmod** MetaTrader 4. El EA original espera a que el mercado se mueva un
a unos pocos pips de un precio de referencia y luego abre una posición contraria. Los pedidos posteriores se promedian en la misma dirección.
siempre que el precio retroceda una distancia configurable. La implementación StockSharp mantiene el comportamiento intacto mientras expone
parámetros fuertemente tipados a través del nivel alto `Strategy` API.

## Lógica comercial

1. Suscríbase a las mejores cotizaciones de oferta/demanda de Nivel 1 y almacene en caché los valores más recientes.
2. Cuando no haya operaciones abiertas, almacene el precio de venta actual como nivel de entrada de referencia. Sólo se permite el comercio entre
`StartHour` y `EndHour`, y desde `StartYear` en adelante.
3. Si la mejor demanda aumenta `Gap` MetaTrader pips por encima de la referencia, envíe una orden de venta de mercado. Si cae `Gap` pips,
enviar una orden de compra de mercado. Los niveles protectores de stop-loss y take-profit se adjuntan automáticamente llamando
`SetStopLoss` / `SetTakeProfit` con las mismas distancias de pips que el asesor experto.
4. Una vez que se establece una dirección de canasta, la estrategia mantiene una lista FIFO de posiciones sintéticas para emular la cobertura.
estilo de MetaTrader. Siempre que el tamaño actual de la cesta sea inferior a `MaxTrades`, los pedidos promedio se agregan cuando el mejor pedido
devuelve dentro de `ReEntryGap` pips del último precio de entrada.
5. La administración del dinero puede usar el parámetro fijo `Lots` o asignar dinámicamente el volumen de acuerdo con la regla EA
`floor(Equity / 20000)`, limitado por `MaxLots`. Todos los volúmenes están normalizados con respecto al paso/mínimo/máximo de volumen del valor.
6. Las actualizaciones fuera de horario restablecen los anclajes de entrada internos para evitar operaciones espurias cuando comience la próxima sesión.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `Lots` | Volumen de orden fijo cuando la administración de dinero está deshabilitada. |
| `StopLoss` | Distancia de parada de protección en MetaTrader pips. Establezca en `0` para desactivar la parada. |
| `TakeProfit` | Distancia objetivo de ganancias en MetaTrader pips. Establezca en `0` para desactivar el objetivo. |
| `Gap` | Distancia en MetaTrader pips que la demanda debe alejarse de la referencia antes de abrir la primera operación. |
| `MaxTrades` | Número máximo de operaciones abiertas simultáneamente (tamaño de la cesta). |
| `ReEntryGap` | Distancia en MetaTrader pips que activa órdenes promedio en la dirección de la cesta. |
| `MoneyManagement` | Habilita la regla de volumen dinámico `floor(Equity / 20000)` cuando se establece en `true`. |
| `MaxLots` | Límite superior para el volumen calculado dinámicamente. |
| `StartHour` / `EndHour` | Ventana de negociación en la hora del servidor de Exchange (inclusive). |
| `StartYear` | Año natural a partir del cual se permite la negociación. Se ignoran los datos anteriores. |

## Notas

- La estrategia sólo consume datos de Nivel1 y no solicita velas. Por tanto, es ligero y reacciona inmediatamente a
cambios de cotización, al igual que el controlador de ticks MT4 `start()`.
- Las paradas y los objetivos dependen de los métodos auxiliares de `Strategy` para traducir distancias de MetaTrader pips en valores específicos del corredor.
niveles de precios. Asegúrese de que el lugar conectado exponga los valores `PriceStep`, `StepPrice` y `VolumeStep` correctos.
- El contador de cesta sintético permite que la estrategia imite cuentas de cobertura aunque StockSharp agregue la posición.
Los llenados parciales y las paradas se manejan mediante la devolución de llamada `OnPositionChanged` que mantiene las colas FIFO.
- La implementación de Python se omite intencionalmente según las pautas del repositorio.
