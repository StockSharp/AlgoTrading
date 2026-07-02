# Enviar estrategia de cierre de orden
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Send Close Order es una adaptación del asesor experto MetaTrader 4 de 2009 "SendCloseOrder" de Vladimir Hlystov. El script original dibuja cuatro líneas de tendencia manuales basadas en fractales de Bill Williams y abre o cierra órdenes de mercado cada vez que el precio toca uno de esos niveles proyectados. La versión StockSharp replica la lógica de decisión con una gestión de línea totalmente automatizada y funciona con cualquier serie de velas proporcionada por la plataforma.

## Lógica comercial

1. **Detección de fractales**: cada vela terminada actualiza una ventana deslizante de cinco barras. Una vez que la ventana está llena, la vela en el medio se compara con las condiciones fractales de Bill Williams. Los máximos y mínimos confirmados se almacenan cronológicamente.
2. **Reconstrucción de la línea de tendencia**
   - *Línea de venta* conecta los dos últimos fractales ascendentes que están separados por un fractal descendente, formando una pendiente de resistencia.
   - *El cierre #1* es la línea de venta desplazada hacia arriba en `15` pasos de precio (15 × `Security.PriceStep`) y actúa como el carril de salida largo.
   - *Línea de compra* conecta los dos últimos fractales descendentes que están separados por un fractal ascendente, formando una pendiente de soporte.
   - *Cierre #2* es la línea de compra desplazada hacia abajo en `15` pasos de precio y actúa como carril de salida corto.
3. **Evaluación de señal**: las cuatro líneas se extrapolan a la marca de tiempo de la vela terminada. Si el precio proyectado se encuentra dentro del rango máximo/bajo de la vela (con una pequeña tolerancia de dos pasos de precio), se activa la acción correspondiente.
4. **Gestión de pedidos**
   - Al tocar Cerrar #1 o Cerrar #2 se cierra inmediatamente toda la posición a través de `ClosePosition()`.
   - Al tocar la línea Vender o Comprar se abre una orden de mercado con volumen `TradeVolume`, siempre que la posición absoluta resultante no supere `MaxOrders × TradeVolume`. Cuando existe una posición opuesta, la orden la compensa primero y luego acumula una nueva entrada, reflejando el comportamiento de las cuentas de cobertura.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `EnableSellLine` | `true` | Permita operaciones cuando se alcance la línea de resistencia proyectada. |
| `EnableBuyLine` | `true` | Permitir operaciones cuando se alcance la línea de soporte proyectada. |
| `EnableCloseLongLine` | `true` | Permitir cerrar posiciones largas en la línea de resistencia desplazada (Cierre n.º 1). |
| `EnableCloseShortLine` | `true` | Permitir cerrar posiciones cortas en la línea de soporte desplazada (Cierre #2). |
| `MaxOrders` | `1` | Número máximo de entradas apiladas en la dirección actual. |
| `TradeVolume` | `0.1` | Volumen de cada orden de mercado individual. |
| `CandleType` | `1h` período de tiempo | Serie de velas utilizadas para cálculos fractales. |

## Diferencias versus la versión MetaTrader

- El puerto StockSharp recalcula las cuatro líneas cada vez que aparece un nuevo fractal. En MetaTrader el usuario tuvo que eliminar y volver a dibujar líneas de tendencia manualmente.
- La ejecución se basa en posiciones netas agregadas; El modelo de cartera predeterminado de StockSharp no admite cestas largas y cortas simultáneas.
- La detección táctil utiliza el máximo/mínimo de la vela terminada con una tolerancia de paso de precio en lugar de las cotizaciones instantáneas de oferta y demanda de los ticks.
- Los objetos del gráfico (líneas de tendencia y etiquetas) no se crean; la atención se centra en las señales comerciales.

## Notas de uso

- La estrategia puede ejecutarse en cualquier instrumento que proporcione velas y un `PriceStep` válido. Cuando `Security.PriceStep` es cero, el código vuelve a ser `0.0001`.
- Aumente `MaxOrders` para emular el comportamiento de apilamiento del EA original. Mantenga `TradeVolume` alineado con el tamaño de lote del instrumento para evitar el redondeo.
- El desplazamiento de línea se fija en el valor histórico de 15 puntos. Ajuste el código fuente si se modifica la entrada MetaTrader.

Solo se proporciona la implementación de C#. Se agregará una traducción de Python por separado si es necesario.
