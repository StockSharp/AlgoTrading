# La estrategia RSI más sencilla (ID 4204)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertido del MetaTrader 4 asesor experto **"Más fácil RSI"** ubicado en `MQL/9827/Easiest_RSI.mq4`.

## Descripción general

El EA original abre operaciones cuando el índice de fuerza relativa (RSI) sale de las zonas de sobreventa/sobrecompra y, opcionalmente, agrega hasta dos posiciones adicionales en la misma dirección a medida que el precio sigue moviéndose favorablemente. Cada orden utiliza el mismo volumen, un stop-loss fijo y un trailing-stop que avanza en pequeños pasos una vez que la operación genera grandes ganancias.

Este puerto StockSharp mantiene el comportamiento en el nivel de estrategia:

- RSI(14) calculado en la serie de velas configuradas impulsa las señales.
- Las operaciones largas se activan cuando RSI cruza hacia arriba el umbral de sobreventa; Los cortos aparecen en cruces a la baja a través del umbral de sobrecompra.
- El escalado de posición imita la lógica de promedio MT4 al agregar una nueva orden cada vez que el precio avanza en `StepPips`, limitado por `MaxEntries`.
- Los stop iniciales y trailing se gestionan internamente con distancias de precios medidas en pips (ajustadas automáticamente para cotizaciones de divisas de 4/5 dígitos).
- Todo el estado (RSI historial, últimos precios de entrada, topes dinámicos) se almacena en campos primitivos para seguir las pautas del marco.

## Parámetros

| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `LotSize` | `1` | Volumen de cada orden de mercado. |
| `StopLossPips` | `50` | Parada de protección inicial en pips (establecida en cero para desactivarla). |
| `TrailingStopPips` | `50` | Distancia del trailing-stop en pips; cero desactiva el seguimiento. |
| `StepPips` | `20` | Movimiento mínimo favorable antes de agregar una posición adicional. |
| `RsiPeriod` | `14` | RSI longitud. |
| `OversoldLevel` | `30` | RSI nivel que debe cruzarse hacia arriba para activar entradas largas. |
| `OverboughtLevel` | `70` | RSI nivel que debe cruzarse hacia abajo para activar entradas cortas. |
| `MaxEntries` | `3` | Número máximo de entradas secuenciales por dirección (que coinciden con el límite de MT4). |
| `CandleType` | `TimeFrame(5m)` | Tipo de vela/período de tiempo utilizado para calcular RSI. |

Todas las distancias expresadas en pips se convierten a precios absolutos utilizando el valor del instrumento `Step`. Para símbolos FX de 5 dígitos, el asistente duplica el paso para que entradas como `50` equivalgan a 5,0 pips, reflejando la guía original EA.

## Lógica de trading

1. **Detección de señales**: la estrategia solo observa velas terminadas. Almacena las dos últimas lecturas de RSI para replicar las llamadas MT4 `iRSI(..., 1)` y `iRSI(..., 2)`. Atraviesa el fuego `OversoldLevel` o `OverboughtLevel` una vez que se cierra la nueva vela.
2. **Entradas principales**: cuando se produce un cruce plano y alcista, se envía una orden de compra en el mercado; los cruces bajistas cuando están planos activan una orden de venta.
3. **Avanzando**: mientras una posición está abierta, la estrategia compara el último cierre/máximo (largo) o cierre/mínimo (corto) con el precio del último llenado. Cada vez que el precio se mueve al menos `StepPips` a favor, se envía una nueva orden con tamaño `LotSize`, hasta `MaxEntries` posiciones totales en esa dirección.
4. **Stop-loss**: en cada ejecución, se recalcula un stop inicial como el precio de la posición menos/más `StopLossPips`. La parada agregada mantiene la distancia más lejana (más conservadora) para que toda la posición quede protegida.
5. **Tracking**: una vez que avanza la operación, el stop se acerca más utilizando el máximo de la vela (largos) o el mínimo (cortos). Un pequeño búfer equivalente a cinco pasos de precio mínimo emula el requisito de MT4 `OrderStopLoss() + 5*Point` antes de que se mueva el stop.
6. **Salir**: cuando el precio alcanza el nivel de parada gestionada, la posición se cierra en el mercado. No se utiliza ningún objetivo de beneficio más allá del trailing stop.

## Notas de implementación

- Las órdenes se envían a través de los asistentes de órdenes de mercado y canalización de alto nivel `SubscribeCandles().Bind(...)` (`BuyMarket` / `SellMarket`).
- La estrategia mantiene `_longOrderPending` / `_shortOrderPending` y banderas de salida para evitar inundar el intercambio con solicitudes duplicadas mientras una orden de mercado espera confirmación.
- `StartProtection` no se invoca porque toda la lógica de protección está codificada explícitamente para coincidir con el comportamiento de MT4.
- Debido a que StockSharp trabaja con posiciones netas, el trailing stop se aplica a la exposición agregada. Esto significa que cuando hay varias entradas abiertas, todos los lotes salen juntos una vez que se toca la parada combinada. El EA original movió la parada de cada orden individualmente; el enfoque agregado mantiene el control del riesgo pero puede cerrar la cesta un poco antes. La diferencia está documentada para mayor transparencia.

## Consejos de uso

1. Asigne la seguridad y el conector deseados, luego configure `CandleType` para que coincida con el período de tiempo que desea negociar (por ejemplo, velas EURUSD de 5 minutos como en los comentarios de la fuente).
2. Ajuste los parámetros basados en pips de acuerdo con la volatilidad del instrumento. Recuerde multiplicar los valores predeterminados por 10 si prefiere trabajar en puntos sin procesar para cotizaciones de 5 dígitos, reflejando la guía MT4.
3. Opcional: modifique `MaxEntries` y `StepPips` para administrar la agresividad con la que la estrategia promedia las operaciones ganadoras.
4. Primero ejecute la estrategia en operaciones en papel para validar las conversiones de pips y el comportamiento de seguimiento en los símbolos de su corredor.

## Archivos

- `CS/EasiestRsiStrategy.cs` – Implementación de la estrategia.
- `README.md` – Este documento.
- `README_zh.md` – Traducción al chino.
- `README_ru.md` – traducción al ruso.

La implementación de Python se omite intencionalmente según lo solicitado.
