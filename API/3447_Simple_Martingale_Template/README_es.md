# Estrategia de plantilla Martingale simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica la idea original de MetaTrader "Plantilla Martingale simple" en StockSharp. Analiza velas terminadas de un período de tiempo configurable utilizando un par de promedios móviles simples (SMA). Un filtro de ruptura verifica si el cierre de la vela anterior rompe el máximo o el mínimo de una vela incluso anterior para confirmar la dirección. El tamaño de la posición sigue una secuencia de martingala: después de cada ciclo perdedor, el siguiente volumen comercial se multiplica, mientras que los ciclos rentables restablecen el volumen al tamaño base configurado.

## Lógica de trading
1. Suscríbete a velas del período de tiempo `CandleType`. En la generación de señales sólo participan las velas terminadas.
2. Calcule un SMA rápido y un SMA lento en el cierre de la vela.
3. Genera una señal de **compra** cuando:
   - el último cierre de vela terminado está por encima del rápido SMA,
   - el rápido SMA está por encima del lento SMA,
   - en la vela anterior, el SMA rápido estaba por debajo del SMA lento, y
   - el último cierre finalizado de la vela está por encima del máximo de la vela de hace dos barras.
4. Genere una señal de **venta** cuando se produzcan condiciones simétricas a la baja, incluido el cierre por debajo del mínimo de la vela de hace dos barras.
5. Cuando se activa una señal y no hay posiciones abiertas ni órdenes activas, envíe una orden de mercado utilizando el volumen de martingala calculado actualmente.
6. Adjunte niveles sintéticos de stop-loss y take-profit monitoreando velas futuras. Cuando el precio toque cualquiera de los niveles, cierre la posición abierta.
7. Después de que se cierra una posición y se actualiza el saldo de la cartera:
   - si el saldo aumentó, restablezca el volumen al valor `BaseVolume`;
   - si el saldo disminuyó, multiplique el último volumen comercial por `Multiplier` y alinéelo con el paso de volumen del instrumento.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `StopLossPoints` | Distancia desde la entrada hasta la parada de protección en puntos de precio. |
| `TakeProfitPoints` | Distancia desde la entrada hasta el objetivo de beneficios en puntos de precio. |
| `BaseVolume` | Tamaño de lote inicial para el ciclo martingala. |
| `Multiplier` | Factor aplicado al tamaño del lote anterior después de una pérdida. |
| `FastPeriod` | Longitud del SMA rápido utilizado para el sesgo direccional. |
| `SlowPeriod` | Duración del SMA lento para confirmación de tendencia. |
| `CandleType` | Plazo de velas procesadas por la estrategia. |

## Gestión monetaria
- La escalera martingala reacciona estrictamente a los cambios de equilibrio realizados. Las pequeñas fluctuaciones (±0,01 unidades monetarias) se ignoran para evitar ruido.
- Los volúmenes están alineados con el instrumento `VolumeStep`, `MinVolume` y `MaxVolume` para garantizar tamaños de pedido válidos.
- Los niveles de stop-loss y take-profit se monitorean en los extremos de las velas (alto/bajo) en lugar de colocar órdenes de intercambio, lo que refleja la implementación original de MQL que utilizaba salidas de mercado.

## Notas de uso
- Elija una combinación de marco temporal y símbolo que produzca suficientes velas históricas para que se formen ambas SMA antes de permitir el comercio.
- Ajuste `StopLossPoints` y `TakeProfitPoints` para que coincidan con el tamaño de marca del símbolo; representan recuentos de puntos, no unidades de precio.
- Considere probar diferentes multiplicadores y volúmenes base para controlar los requisitos de capital porque las secuencias de martingala crecen rápidamente.
- La estrategia exige que `StartProtection()` comience a integrarse con las funciones estándar de gestión de riesgos de StockSharp.
