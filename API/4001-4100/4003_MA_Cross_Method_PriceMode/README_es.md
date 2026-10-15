# Estrategia de modo de precio de método cruzado MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **MA Cross Method PriceMode** es una adaptación directa StockSharp del MetaTrader 4 experto "MA_cross_Method_PriceMode". Combina dos medias móviles configurables y reacciona cada vez que la media rápida cruza la media lenta. Ambas líneas exponen las entradas originales de MetaTrader: período, método de suavizado (SMA, EMA, SMMA, LWMA), precio aplicado (cierre, apertura, máximo, mínimo, mediana, típico, ponderado) y desplazamiento horizontal. La estrategia funciona con cualquier instrumento que proporcione velas regulares basadas en el tiempo.

## Indicadores
- **Promedio móvil rápido**: longitud, método y fuente de precio configurables. El parámetro de cambio MetaTrader se reproduce almacenando en el buffer los valores del indicador completados y leyendo las barras del valor `FirstShift`.
- **Promedio móvil lento**: longitud, método y fuente de precio configurables con la misma emulación de turno mediante almacenamiento en búfer.

## Lógica de trading
1. La estrategia se suscribe al tipo de vela seleccionado y procesa solo velas terminadas para evitar el repintado dentro de la barra.
2. Para cada barra cerrada, alimenta ambas medias móviles con sus respectivos precios aplicados.
3. Cuando ambos promedios producen valores finales, la estrategia evalúa dos condiciones:
   - **Cruz alcista** – la MA rápida estaba por debajo o igual a la MA lenta en la barra anterior y se mueve por encima de ella en la barra actual.
   - **Cruz bajista** – la MA rápida estaba por encima o igual a la MA lenta en la barra anterior y se mueve por debajo de ella en la barra actual.
4. En un cruce alcista, la estrategia compra `OrderVolume` contratos. Si hay una posición corta abierta, el tamaño de la orden aumenta automáticamente para cubrir la posición corta y establecer la nueva exposición larga.
5. En un cruce bajista, la estrategia vende `OrderVolume` contratos. Si una posición larga está abierta, el tamaño de la orden aumenta para cerrarla antes de establecer la posición corta.
6. Se invoca `StartProtection()` para que se puedan agregar StockSharp módulos de protección si se desea (por ejemplo, asistentes de parada de pérdidas o de equilibrio).

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `FirstPeriod` | Período de la media móvil rápida. | `3` |
| `SecondPeriod` | Período de la media móvil lenta. | `13` |
| `FirstMethod` | Método de suavizado utilizado para la media móvil rápida (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `SecondMethod` | Método de suavizado utilizado para la media móvil lenta. | `LinearWeighted` |
| `FirstPriceMode` | Precio aplicado para la media móvil rápida (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPriceMode` | Precio aplicado para la media móvil lenta. | `Median` |
| `FirstShift` | Desplazamiento horizontal (en barras) aplicado a la media móvil rápida. | `0` |
| `SecondShift` | Desplazamiento horizontal (en barras) aplicado a la media móvil lenta. | `0` |
| `OrderVolume` | Volumen de orden base utilizado para nuevas posiciones. | `0.1` |
| `CandleType` | Tipo de vela/plazo de tiempo procesado por la estrategia. | velas de 5 minutos |

## Diferencias en comparación con la versión MQL
- La iteración de la orden MetaTrader (`OrdersTotal`, `OrderSelect`, `OrderClose`) se reemplaza por el uso directo de la propiedad StockSharp `Strategy.Position` y las órdenes de mercado dimensionadas para revertir la exposición cuando sea necesario.
- El indicador de "nueva barra" MetaTrader no es necesario: `ProcessCandle` se ejecuta exactamente una vez por vela terminada, lo que garantiza el mismo comportamiento una vez por barra sin sondeo a nivel de tick.
- El manejo de turnos MA se implementa con buffers compactos que contienen los últimos valores `shift + 2` para cada promedio. Esto refleja el desplazamiento del indicador sin depender de referencias anteriores prohibidas del indicador (`GetValue`).
- La estrategia es independiente del corredor; Se pueden adjuntar ayudantes de gestión de riesgos a través de `StartProtection()` en lugar de los argumentos fijos de parada/límite MetaTrader.

## Notas de uso
- Elija la duración de la vela que coincida con el período de tiempo original (por ejemplo, M5 o H1). Se pueden proporcionar períodos de tiempo personalizados editando `CandleType` en los parámetros de la estrategia.
- Establecer `FirstShift` o `SecondShift` en un valor positivo retrasa el cruce efectivo en esa misma cantidad de barras completadas, al igual que la entrada de desplazamiento horizontal en MetaTrader.
- El modo de precio `Weighted` reproduce la fórmula `(High + Low + 2 * Close) / 4` de MetaTrader. Los modos mediano y típico siguen las definiciones estándar `(High + Low) / 2` y `(High + Low + Close) / 3`.
- Dado que cada orden es una orden de mercado, asegúrese de que la configuración de la cuenta tolere el volumen y el deslizamiento solicitados.
