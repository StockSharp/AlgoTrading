# Estrategia EMA LWMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
La **Estrategia EMA LWMA RSI** reproduce el asesor experto de MetaTrader "EMA LWMA RSI" en StockSharp. Compara dos medias móviles que usan el mismo precio aplicado y opcionalmente un desplazamiento hacia adelante, mientras un filtro de Índice de Fuerza Relativa confirma el momentum. El algoritmo solo reacciona a velas completamente terminadas del marco temporal configurado y opera una sola posición neta: cierra cualquier exposición opuesta antes de abrir una nueva orden en la dirección señalada. Las distancias de stop-loss y take-profit se configuran en pips y se escalan automáticamente al tamaño de tick del instrumento.

## Lógica de trading
1. Calcular una media móvil exponencial (EMA) y una media móvil ponderada lineal (LWMA) con longitudes individuales pero el mismo precio aplicado. Si `MaShift` es mayor que cero, ambas medias se desplazan hacia adelante por el número especificado de barras para reflejar el argumento "shift" de MetaTrader.
2. Procesar un RSI con su propio precio aplicado. La estrategia usa el umbral clásico de 50 para distinguir momentum alcista y bajista.
3. Cuando llega una vela terminada:
   - Se genera una señal de **compra** si el EMA cruza **por encima** del LWMA (el EMA anterior era mayor que el LWMA anterior, pero el EMA actual está por debajo del LWMA actual) y el valor RSI está **por encima de 50**.
   - Se genera una señal de **venta** si el EMA cruza **por debajo** del LWMA (el EMA anterior era menor que el LWMA anterior, pero el EMA actual está por encima del LWMA actual) y el valor RSI está **por debajo de 50**.
4. Las señales establecen indicadores de pendiente internos. Antes de revertir, la estrategia primero cierra la posición existente con `ClosePosition()`. Después de confirmar el llenado, inmediatamente envía una orden de mercado en la dirección solicitada. Esto refleja el asesor experto original que esperaba la confirmación de transacción antes de enviar la siguiente orden.
5. Las órdenes protectoras se inician vía `StartProtection`. Si un stop-loss o take-profit está desactivado (establecido en cero), esa parte se omite, coincidiendo con el comportamiento MQL.

## Notas de implementación
- La selección de precio aplicado soporta las opciones de MetaTrader (Cierre, Apertura, Máximo, Mínimo, Mediano, Típico, Ponderado, Promedio). El precio ponderado se calcula como `(Máximo + Mínimo + 2 * Cierre) / 4`, idéntico a `PRICE_WEIGHTED`.
- El dimensionamiento de pips multiplica automáticamente el `PriceStep` del instrumento por 10 para símbolos forex de 3/5 dígitos, asegurando que un pip equivalga a 10 puntos en cotizaciones fraccionarias.
- Las vinculaciones de indicadores dependen de la suscripción de velas de alto nivel de StockSharp. El manejo de desplazamiento usa indicadores `Shift` en lugar de indexación manual de buffers.
- El código mantiene indicadores booleanos para solicitudes de compra/venta pendientes. Previenen órdenes duplicadas mientras el comando anterior todavía está pendiente y se limpian cuando llegan los llenados o cuando la posición ya coincide con la señal.
- Los ayudantes de gráficos dibujan ambas medias móviles en el panel de precio y el RSI en un área separada para inspección visual.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `1h TimeFrame` | Serie de velas procesada por la estrategia. |
| `StopLossPips` | `int` | `150` | Distancia de stop-loss en pips. `0` desactiva el stop. |
| `TakeProfitPips` | `int` | `150` | Distancia de take-profit en pips. `0` desactiva el objetivo. |
| `EmaPeriod` | `int` | `28` | Período de la media móvil exponencial. |
| `LwmaPeriod` | `int` | `8` | Período de la media móvil ponderada lineal. |
| `MaShift` | `int` | `0` | Desplazamiento hacia adelante (barras) aplicado a ambas medias móviles. |
| `RsiPeriod` | `int` | `14` | Período de promediado del RSI. |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Precio aplicado enviado a EMA y LWMA. |
| `RsiAppliedPrice` | `AppliedPriceType` | `Weighted` | Precio aplicado usado por el RSI. |

## Uso
1. Adjuntar la estrategia al instrumento deseado y establecer `CandleType` para que coincida con el marco temporal usado en MetaTrader.
2. Ajustar las protecciones basadas en pips y la configuración de indicadores si el broker usa valores predeterminados diferentes.
3. Habilitar el trading una vez que la suscripción esté activa. La estrategia gestionará una posición a la vez y usará `ClosePosition()` antes de cambiar de dirección.

No se proporciona traducción en Python para esta estrategia todavía.
