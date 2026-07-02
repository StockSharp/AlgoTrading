# Nina EA Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Nina EA es un seguidor de tendencias de una posición convertido del experto MetaTrader 4 "NinaEA". El robot original utiliza un indicador personalizado llamado **NINA** y opera cada vez que la diferencia entre los amortiguadores alcistas y bajistas del indicador cruza por encima o por debajo de cero. En la versión StockSharp, el indicador personalizado se reemplaza con el indicador **SuperTrend** incorporado, que también publica buffers alcistas y bajistas separados. Un cambio en la dirección de SuperTrend sirve como indicador de cruce por cero: cuando la tendencia se vuelve alcista, la estrategia compra, y cuando se vuelve bajista, vende.

La estrategia siempre mantiene como máximo una posición abierta. Una señal opuesta cierra inmediatamente la posición existente y establece una nueva operación en la nueva dirección. Se puede habilitar un stop-loss opcional expresado en puntos de precio para imitar la entrada original "StopLoss".

## Lógica de trading
1. Suscríbase a la serie de velas configuradas y calcule SuperTrend con el período y el multiplicador ATR suministrados.
2. Espere hasta que se formen tanto la estrategia como el indicador antes de reaccionar a las señales.
3. En cada vela completa:
   - Si se toca un precio stop protector, salga de la posición abierta en el mercado.
   - Si SuperTrend cambia de bajista a alcista, cierre cualquier exposición corta y compre con el volumen configurado.
   - Si SuperTrend cambia de alcista a bajista, cierre cualquier exposición larga y venda con el volumen configurado.
   - Almacene la dirección actual de SuperTrend para detectar el siguiente giro.

La lógica replica el comportamiento del experto MetaTrader, donde `nina = Buffer0 - Buffer1` y un cambio de signo impulsan tanto las salidas como las nuevas entradas.

## Gestión de posiciones y riesgos
- Sólo puede haber una posición activa a la vez; todas las operaciones invierten la dirección en lugar de acumular varias órdenes.
- Un stop-loss opcional en puntos de precio se calcula a partir del precio de cumplimiento. Para una operación larga, el stop se coloca debajo de la entrada y para una operación corta, se coloca encima de la entrada. Poner el parámetro a cero desactiva la parada.
- Se llama a `StartProtection()` para que se puedan configurar las protecciones integradas StockSharp si se desea.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Volume` | `0.1` | Volumen de pedidos utilizado para cada nueva entrada. |
| `AtrPeriod` | `10` | periodo ATR pasado al cálculo de SuperTrend (mapa el `PeriodWATR` original). |
| `AtrMultiplier` | `1` | multiplicador ATR para SuperTrend (asigna el `Kwatr` original). |
| `StopLossPoints` | `0` | Distancia de stop-loss opcional en puntos de precio. Zero mantiene el stop deshabilitado, idéntico al código MetaTrader que enviaba órdenes de mercado sin precio de stop. |
| `CandleType` | `TimeFrame(1 minute)` | Serie de velas que alimenta el indicador y la lógica comercial. |

## Notas de conversión
- El experto MetaTrader se basó en el indicador personalizado `NINA`. Sus dos amortiguadores se interpretaron como líneas SuperTrend alcistas/bajistas porque sólo su diferencia y su signo importaban para el trading. SuperTrend expone la misma información a través de su indicador `IsUpTrend`, lo que lo convierte en un reemplazo de alto nivel adecuado que no requiere manejo manual del búfer.
- La lógica de cierre de órdenes refleja el bucle `OrdersTotal()` del guión original: un cambio de tendencia primero favorece la posición actual y luego abre una operación en la nueva dirección.
- Las entradas MetaTrader no utilizadas (`highlow`, `cbars`, `from`, `maP`, `SMAspread`, `Slippage`) se omiten porque no influyen en las reglas comerciales del archivo original.

## Consejos de uso
1. Adjunte la estrategia a un valor y configure el período de vela que coincida con su prueba MetaTrader.
2. Ajuste el período ATR y el multiplicador para replicar el comportamiento del indicador original.
3. Aumente `StopLossPoints` si desea un límite de riesgo estricto; de lo contrario, déjelo en cero para salidas basadas puramente en señales.
