# Estrategia cruzada (MQL 27596 conversión)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia cruzada** es una conversión directa del MetaTrader asesor experto `Cross.mq4` (entrada del repositorio `MQL/27596`). El EA original negoció un único promedio móvil exponencial (EMA) medido en los precios de apertura de la barra y aplicó niveles de toma de ganancias y stop loss de distancia fija. Este puerto StockSharp mantiene intacta la lógica comercial mientras utiliza funciones API de alto nivel, como suscripciones de velas, vinculación de indicadores y seguimiento de posiciones administradas.

## Lógica de trading
1. **Indicador**: una única media móvil exponencial (EMA) calculada a partir de los precios de cierre de las velas. El período es configurable y su valor predeterminado es 200, lo que coincide con la fuente MQL.
2. **Detección de señal**: en cada vela terminada, la estrategia compara la vela abierta con el valor EMA:
   - Una **señal alcista** ocurre cuando la vela se abre por encima del EMA después de abrir previamente en él o por debajo de él. Esto reproduce la llamada `Cross(0, Open[0] > EMA)` en el script MQL.
   - Una **señal bajista** ocurre cuando la vela se abre por debajo del EMA después de abrir previamente en él o por encima (`Cross(1, Open[0] < EMA)` en el código original).
3. **Gestión de posición**: cuando se activa una señal, la estrategia invierte completamente la posición actual:
   - Si aparece un cruce alcista mientras está plano o en corto, compra suficiente volumen para cubrir la exposición corta y abrir una nueva posición larga.
   - Si aparece un cruce bajista mientras está plano o largo, vende suficiente volumen para aplanar la exposición larga y establecer una posición corta.
4. **Control de riesgos**: después de entrar en una posición, la estrategia monitorea los máximos y mínimos de las velas para implementar salidas fijas de toma de ganancias y stop loss en unidades de paso de precio. Estas salidas emulan las llamadas `OrderSend` que configuran tanto `TakeProfit` como `StopLoss` en MetaTrader.

## Parámetros
| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `EMA Length` | 200 | Período del EMA utilizado para la detección cruzada. Debe ser mayor que cero. |
| `Take Profit (steps)` | 200 | Distancia al nivel de toma de ganancias medida en pasos de precio. Establezca en cero para desactivar el objetivo de ganancias. |
| `Stop Loss (steps)` | 100 | Distancia al tope de protección medida en pasos de precio. Establezca en cero para desactivar la parada. |
| `Candle Type` | marco de tiempo de 1 minuto | Fuente de datos de velas procesada por la estrategia. Puede cambiar a otros períodos de tiempo o tipos de velas personalizados admitidos por StockSharp. |

El volumen negociado está controlado por la propiedad `Volume` de la estrategia. Cuando llega una señal de reversión, la estrategia envía `Volume + |Position|` para garantizar que la exposición existente se cierre antes de abrir la nueva posición.

## Flujo de ejecución
1. `OnStarted` se suscribe a la serie de velas configurada y vincula el indicador EMA utilizando el asistente de alto nivel `Bind`.
2. El manejador omite las velas sin terminar y espera hasta que EMA esté completamente formado. Una vez listo,:
   - Gestiona la posición activa comparando los niveles de stop loss y takeprofit con los valores máximo/bajo de la vela.
   - Detecta cruces alcistas y bajistas basándose en el precio de apertura de la vela en relación con el EMA.
   - Emite órdenes de mercado para revertir la posición cuando aparece una nueva señal.
3. `OnNewMyTrade` rastrea el precio de entrada promedio y la dirección de la posición activa para que las comprobaciones de salida utilicen niveles precisos incluso al escalar las operaciones.
4. Se crean objetos de gráfico opcionales (si hay un gráfico disponible) para mostrar velas, la línea EMA y las operaciones ejecutadas.

## Detalles de gestión de riesgos
- **Stop Loss**: se calcula como `entry price ± stop steps × price step` dependiendo de la dirección. La estrategia sale inmediatamente cuando la vela mínima (larga) o máxima (corta) supera el nivel de stop.
- **Take Profit**: se calcula de manera similar utilizando los pasos de ganancias configurados. Alcanzar el objetivo cierra toda la posición durante la vela donde el máximo/mínimo cruza el umbral.
- **Protección de cuenta**: `StartProtection()` se invoca una vez al inicio para que la estrategia respete las reglas de protección global configuradas en los entornos StockSharp.

## Consejos de personalización
- Los plazos más cortos o la duración de EMA crean reversiones más frecuentes. Combínelo con mayores distancias de parada para evitar sacudidas.
- Para operar con múltiples símbolos, cree instancias de estrategia separadas con sus propios valores y tipos de velas.
- Al optimizar, mantenga la longitud de EMA y las distancias de parada/toma dentro de límites realistas para la volatilidad y el tamaño del tick del instrumento.

## Notas de conversión
- La matriz MQL `crossed[2]` está asignada a dos indicadores booleanos internos que persisten en las velas.
- La función MQL `OrderSend` está representada por los ayudantes `BuyMarket` y `SellMarket` de StockSharp, lo que garantiza que tanto la reversión como las nuevas entradas reflejen el comportamiento original.
- Los valores EMA se proporcionan a través de la devolución de llamada de enlace, evitando llamadas directas a `GetValue` como lo exigen las pautas del repositorio.

Si sigue estos detalles, podrá reproducir la estrategia MetaTrader original dentro de StockSharp manteniendo el control total sobre las fuentes de datos, la optimización de parámetros y los gráficos.
