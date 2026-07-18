# Estrategia de Ruptura de Canal Ketty
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
La Estrategia de Ruptura de Canal Ketty es una conversión directa a C# del asesor experto original Ketty.mq5. Construye un canal de precios a corto plazo durante una ventana pre-mercado configurable y espera a que el mercado se dispare fuera de ese rango. Cuando ocurre un disparo, la estrategia coloca una orden stop en el lado opuesto del canal con protección opcional de stop-loss y take-profit, reflejando el flujo de trabajo de órdenes pendientes implementado en el script MQL5.

## Lógica de Trading
1. **Reinicio diario** – En la primera vela de cada día de trading la estrategia borra órdenes pendientes (y órdenes de protección si no hay posición abierta) y reinicia las estadísticas del canal.
2. **Construcción del canal** – Entre `ChannelStartHour:ChannelStartMinute` y `ChannelEndHour:ChannelEndMinute` se rastrean el máximo más alto y el mínimo más bajo del `CandleType` seleccionado. El rango detectado representa el canal de ruptura para el resto del día.
3. **Precios de las órdenes** – El buy stop planeado es `channelHigh + OrderPriceShiftPips`, mientras que el sell stop planeado es `channelLow - OrderPriceShiftPips`. La conversión de pip a precio coincide con el robot original: cuando el instrumento tiene 3 o 5 decimales, un pip equivale a diez pasos de precio; de lo contrario se usa un solo paso de precio.
4. **Detección de señal** – Una vez que el canal está disponible y la hora actual está entre `PlacingStartHour` y `PlacingEndHour`, se inspecciona la vela terminada más reciente. Aparece una configuración de compra si el mínimo de la vela rompe por debajo del canal al menos `ChannelBreakthroughPips`. Aparece una configuración de venta cuando el máximo de la vela supera el canal la misma distancia.
5. **Gestión de órdenes pendientes** – Solo una orden pendiente está activa en cualquier momento. Tan pronto como se genera una señal, la orden pendiente anterior (si la hay) se cancela y se registra la nueva orden stop. Todas las órdenes pendientes se eliminan automáticamente después de `PlacingEndHour`.
6. **Órdenes de protección** – Después de que se ejecuta la orden pendiente, la estrategia envía inmediatamente el stop de protección correspondiente (si `StopLossPips` es positivo) y el objetivo de beneficio (si `TakeProfitPips` es positivo). Esas órdenes se cancelan cuando la posición está completamente cerrada.

## Parámetros
- `EntryVolume` – volumen predeterminado para nuevas órdenes.
- `StopLossPips` – distancia entre el precio de entrada y la orden stop de protección; establecer en cero para deshabilitar.
- `TakeProfitPips` – distancia entre el precio de entrada y la orden take-profit; establecer en cero para deshabilitar.
- `ChannelStartHour` / `ChannelStartMinute` – hora del día en que comienza el cálculo del canal.
- `ChannelEndHour` / `ChannelEndMinute` – hora del día en que termina el cálculo del canal. El canal puede extenderse más allá de la medianoche porque la implementación normaliza la ventana de tiempo.
- `PlacingStartHour` – hora del día en que las órdenes pendientes pueden comenzar a aparecer.
- `PlacingEndHour` – hora del día después de la cual se cancelan todas las órdenes pendientes.
- `ChannelBreakthroughPips` – buffer de ruptura que debe ser penetrado por la última vela antes de que se arme una orden stop.
- `OrderPriceShiftPips` – desplazamiento adicional añadido al borde del canal al colocar la orden stop pendiente.
- `VisualizeChannel` – cuando está habilitado la estrategia dibuja dos líneas horizontales que representan el canal actual en el gráfico.
- `CandleType` – marco temporal usado para construir y monitorear el canal.

## Notas Adicionales
- La estrategia asume que el instrumento opera continuamente; si faltan datos dentro de la ventana del canal, el sistema esperará nuevas velas antes de armar cualquier orden.
- Las órdenes de protección se registran usando órdenes stop/límite separadas después de que se ejecuta la entrada, porque StockSharp no adjunta SL/TP directamente a órdenes pendientes de la misma manera que MetaTrader.
- Asegúrese de que `EntryVolume` coincida con el paso de lote del bróker y que el `CandleType` seleccionado corresponda a un marco temporal líquido (el robot original fue diseñado para barras de un minuto).
