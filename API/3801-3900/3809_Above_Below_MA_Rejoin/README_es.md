# Arriba Abajo Estrategia de reincorporación a MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Above Below MA Rejoin es una conversión StockSharp del MetaTrader 4 asesor experto "AboveBelowMA". El script original monitorea el gráfico de 15 minutos del GBP/USD y compara el precio actual con un promedio móvil exponencial de un período (EMA) calculado sobre el precio típico. Cuando el precio cotiza en el lado opuesto de un promedio ascendente o descendente, la estrategia intenta atenuar esa excursión y volver a unirse a la dirección subyacente del EMA. Este puerto mantiene intacta la estructura de la señal mientras aprovecha StockSharp API de alto nivel (`SubscribeCandles` + `Bind`).

## Lógica comercial
- Suscríbase al tipo de vela configurado (15 minutos de forma predeterminada) y proporcione un promedio móvil exponencial que utilice el precio típico `(High + Low + Close) / 3`.
- Realice un seguimiento de los valores EMA más recientes y anteriores para comprender la pendiente a corto plazo. Un sesgo alcista requiere que el EMA suba, mientras que un sesgo bajista requiere que baje.
- **Configuración larga:** cuando la vela abre al menos un paso de precio por debajo de EMA, cierra por debajo de EMA y el valor anterior de EMA es inferior al valor actual de EMA, cierre cualquier exposición corta y prepárese para comprar. Si no queda ninguna posición, envíe una orden de compra de mercado.
- **Configuración corta:** cuando la vela abre al menos un paso de precio por encima de EMA, cierra por encima de EMA y el valor anterior de EMA es mayor que el valor actual de EMA, cierre cualquier exposición larga y prepárese para vender. Si la posición es plana, envíe una orden de venta de mercado.
- Las órdenes se emiten únicamente sobre velas terminadas para evitar señales prematuras en barras parcialmente formadas.

## Tamaño de posición
- Los tamaños de la versión MetaTrader se comercializan utilizando `AccountFreeMargin / 10000` con un límite de 5 lotes. La implementación StockSharp ofrece un comportamiento equivalente: cuando `UseDynamicVolume` está habilitado, la estrategia divide el valor actual de la cartera por `BalanceToVolumeDivider` (predeterminado `10000`).
- El tamaño calculado está limitado por `MaxVolume`, lo que refleja el límite estricto de 5 lotes del asesor experto. Si el tamaño dinámico está deshabilitado, el parámetro `InitialVolume` se utiliza como volumen fijo.
- Todos los volúmenes están alineados con el paso de volumen del instrumento y las restricciones de volumen mínimo/máximo para evitar el rechazo por parte del corredor o simulador.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `EmaLength` | Período de la media móvil exponencial (el valor predeterminado es 1, que coincide con EA). |
| `CandleType` | Plazo utilizado para construir las velas que alimentan el EMA (predeterminado 15 minutos). |
| `InitialVolume` | Volumen de pedido fijo cuando el tamaño dinámico está deshabilitado. |
| `UseDynamicVolume` | Habilita el dimensionamiento de posiciones basado en cartera (`Balance / BalanceToVolumeDivider`). |
| `BalanceToVolumeDivider` | Divisor aplicado al valor de la cartera para emular `AccountFreeMargin / 10000`. |
| `MaxVolume` | Volumen máximo de órdenes permitido por la estrategia. |

## Notas
- La estrategia utiliza `ClosePosition()` antes de abrir una operación en la dirección opuesta, coincidiendo con la lógica MetaTrader que cierra órdenes opuestas a través de `CheckOrders`.
- Debido a que las señales se evalúan en velas terminadas, las entradas pueden ocurrir un poco más tarde que la versión MetaTrader basada en ticks. Este cambio mejora la estabilidad cuando se ejecutan pruebas retrospectivas o se opera en vivo con datos de velas.
- Asegúrese de que el valor seleccionado proporcione información significativa sobre `PriceStep`, `VolumeStep` y valoración de la cartera para que el bloque de volumen dinámico funcione como se esperaba.
