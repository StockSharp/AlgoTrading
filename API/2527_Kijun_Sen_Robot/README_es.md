# Estrategia Kijun-Sen Robot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Kijun-Sen Robot** es una conversión directa del asesor experto de MetaTrader 5 "Kijun Sen Robot" a la API de estrategias de alto nivel de StockSharp. Opera por defecto en velas de 30 minutos y se centra en cruces del precio con la línea Kijun-sen de Ichimoku confirmados por una media móvil linealmente ponderada (LWMA) de 20 períodos. La estrategia mantiene la idea original del experto de operar solo durante las horas más activas, aplicando protección de posición con lógica dinámica de stop, break-even y trailing.

## Indicadores y datos
- **Ichimoku** con Tenkan, Kijun y Senkou Span B configurados en 6/12/24 períodos.
- **Media Móvil Linealmente Ponderada (LWMA)** de 20 barras para confirmación de pendiente y filtrado de distancia.
- **Velas de 30 minutos** (por defecto) para generación de señales. Se puede seleccionar cualquier otro marco temporal a través del parámetro `CandleType`.

## Lógica de trading
### Entrada larga
1. La vela atraviesa la línea Kijun desde abajo. La vela debe abrir por debajo de la línea, cerrar por encima, o tocarla durante la barra mientras el cierre anterior también estaba por debajo.
2. El Kijun actual es plano o sube comparado con dos barras atrás.
3. La LWMA está al menos `MaFilterPips` (convertido a unidades de precio) por debajo del nivel Kijun, manteniendo la línea base sobre la media móvil.
4. La pendiente de la LWMA es positiva (LWMA actual mayor que el valor anterior).
5. El horario de trading está dentro de `[TradingStartHour, TradingEndHour)`, por defecto 07:00–19:00 hora de la bolsa.

Cuando todas las condiciones se satisfacen y la estrategia no tiene posición neta larga, se envía una orden de compra a mercado (cualquier corto existente se cubre primero). El precio de entrada es el cierre de la vela.

### Entrada corta
1. La vela atraviesa la línea Kijun desde arriba (espejo de la lógica larga).
2. El Kijun es plano o cae relativo a dos barras atrás.
3. La LWMA está al menos `MaFilterPips` por encima del nivel Kijun.
4. La pendiente de la LWMA es negativa (LWMA actual menor que el valor anterior).
5. La entrada ocurre solo dentro de la ventana de trading permitida.

Se coloca una orden de venta a mercado (la exposición larga existente se cierra antes de abrir un corto).

### Gestión de posición y salidas
- **Stop-loss inicial** – colocado `StopLossPips` por debajo/encima del precio de entrada (convertido a unidades de precio mediante el paso de precio del instrumento). Esto reproduce el stop protector de la versión MQL.
- **Movimiento break-even** – una vez que el beneficio no realizado supera `BreakEvenPips`, el stop se mueve al precio de entrada más un pip (largo) o menos un pip (corto). El umbral se mide usando la misma lógica de conversión de pips.
- **Trailing stop** – después de que el precio avanza `TrailingStopPips`, el stop sigue al precio a esa distancia, solo en la dirección favorable.
- **Take-profit fijo** – objetivo opcional definido por `TakeProfitPips`. Poner en cero para deshabilitar.
- **Salida por pendiente Kijun** – si la LWMA gira contra la operación antes de que el stop se mueva más allá del break-even, la posición se cierra inmediatamente, coincidiendo con la salida de emergencia del experto original.
- **Filtro de tiempo** – las nuevas operaciones se ignoran fuera de la ventana configurada, pero las operaciones abiertas siguen gestionándose hasta cerrarse por las reglas anteriores.
- **Manejo de órdenes** – la estrategia StockSharp usa exclusivamente órdenes de mercado; la lógica compleja de entrada límite-vs-mercado del EA original se simplifica porque se usan datos de velas en vez de datos de tick.

Si tanto el nivel de stop-loss como el de take-profit se incumplirían dentro de la misma barra, el stop-loss tiene precedencia para mantenerse conservador sin información intrabar.

## Parámetros
| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `TenkanPeriod` | 6 | Longitud de Ichimoku Tenkan-sen. |
| `KijunPeriod` | 12 | Longitud de Ichimoku Kijun-sen. |
| `SenkouSpanBPeriod` | 24 | Longitud de Ichimoku Senkou Span B. |
| `LwmaPeriod` | 20 | Longitud del filtro de confirmación LWMA. |
| `MaFilterPips` | 6 | Distancia mínima LWMA-a-Kijun en pips. |
| `StopLossPips` | 50 | Distancia del stop protector inicial. |
| `BreakEvenPips` | 9 | Beneficio necesario para mover el stop al break-even. |
| `TrailingStopPips` | 10 | Distancia para el movimiento del trailing stop. |
| `TakeProfitPips` | 120 | Distancia opcional de take-profit fijo. |
| `TradingStartHour` | 7 | Primera hora de trading permitida (inclusive). |
| `TradingEndHour` | 19 | Última hora de trading permitida (exclusiva). |
| `CandleType` | Marco temporal de 30 minutos | Tipo de datos usado para la evaluación de señales. |

Todos los parámetros basados en pips se traducen en unidades de precio usando el `PriceStep` del instrumento. Los instrumentos con 3 o 5 dígitos decimales reciben automáticamente un factor de 10 para replicar el tamaño clásico de pip forex.

## Notas de implementación
- La conversión mantiene las variables de estado de la estrategia (comportamiento `longcross`, `shortcross`) a través de `_pendingLongLevel` y `_pendingShortLevel`, asegurando que las nuevas posiciones requieran un cruce Kijun fresco.
- Las comprobaciones intrabar como "último bid/ask" de la versión MT5 se aproximan con condiciones a nivel de vela (`Open`, `Close`, `High`, `Low`). Esto hace la lógica determinista para backtesting en StockSharp.
- La protección de posición usa `ClosePosition()` y seguimiento manual del stop en lugar de modificaciones de órdenes MT5. Los ajustes de break-even y trailing se ejecutan una vez por vela finalizada.
- El método auxiliar `ConvertPips` realiza la conversión pip-a-precio usando `Security.PriceStep` o `Security.MinPriceStep`, aplicando un multiplicador 10× para tamaños de tick de 3 o 5 decimales para emular la regla `digits_adjust` de MT5.
- Porque la estrategia está ligada a la API de alto nivel, los indicadores se vinculan vía `SubscribeCandles().BindEx(...)`, y los dibujos del gráfico se configuran automáticamente (velas, Ichimoku, LWMA, operaciones propias).

## Directrices de uso
1. Adjuntar la estrategia a un instrumento que soporte velas de 30 minutos (o configurar un `CandleType` diferente).
2. Configurar `Volume` en la instancia de la estrategia al tamaño de orden deseado antes de iniciar.
3. Opcionalmente ajustar los parámetros basados en pips para reflejar la volatilidad del instrumento o reproducir configuraciones optimizadas para pares de divisas específicos.
4. Ejecutar en el backtester de alto nivel o entorno en vivo; la estrategia aplicará la misma ventana de trading, reglas de stop y trailing que el experto original.
5. Monitorear el registro o gráfico para ver las actualizaciones de break-even y trailing. Todos los comentarios en el código están en inglés por claridad según lo solicitado.

La versión de Python se omite intencionalmente; solo la implementación en C# se proporciona en esta carpeta.
