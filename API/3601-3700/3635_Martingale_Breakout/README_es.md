# Martingale Estrategia de ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Martingale estrategia de ruptura** es una StockSharp adaptación del MetaTrader asesor experto `MartinGaleBreakout.mq5`. el sistema
espera velas de ruptura anormalmente grandes y coloca una orden de mercado única en la dirección de ruptura. Mientras que el EA original
rastrea un "número mágico" para gestionar sus posiciones, la implementación de StockSharp depende del contexto de la estrategia, por lo que el comportamiento
es efectivamente el mismo cuando la estrategia se ejecuta de forma aislada.

El algoritmo se centra en dos ideas centrales:

1. **Detección de ruptura**: la estrategia examina el tamaño de cada vela terminada y la compara con el rango promedio de la
diez velas anteriores. Cuando el rango actual es tres veces mayor que el promedio y la vela cierra con fuerza en el
dirección de la ruptura, se produce una señal comercial.
2. **Recuperación al estilo Martingale**: la estrategia realiza un seguimiento de las pérdidas y ganancias flotantes. Siempre que el PnL no realizado alcance el
umbral de pérdida configurado, cierra inmediatamente todas las posiciones abiertas y aumenta el siguiente objetivo de ganancias para que la siguiente operación
intenta recuperar la pérdida. Una vez que se alcanza el objetivo aumentado, los umbrales se restablecen a los valores originales.

El puerto mantiene todos los parámetros de administración de dinero del código MQL5, incluido el porcentaje de saldo reservado para margen, el
objetivos de pérdidas y ganancias basados en porcentajes, y el multiplicador que amplía la distancia de obtención de beneficios durante la fase de recuperación.

## Lógica comercial

1. Suscríbase a la serie de velas configuradas y espere las velas terminadas.
2. Calcule el rango de la vela (`High - Low`) y mantenga un búfer de tamaño fijo con los diez rangos anteriores para determinar el
promedio de referencia utilizado para la detección de rupturas.
3. Calcule el PnL flotante siguiendo los precios de entrada promedio para los lados largo y corto. Si el PnL no realizado excede el
objetivo de ganancias o supera el umbral de stop-loss, cierre inmediatamente todas las posiciones y restablezca el estado de recuperación como en el
asesor experto original.
4. Omita la colocación de órdenes mientras la estrategia ya tenga una posición o cuando el estado de conexión no permita operar.
5. Cuando aparezca una vela de ruptura alcista, dimensione la orden para que la ganancia esperada coincida con el objetivo actual. la toma de ganancias
La distancia en los pasos de precio se multiplica durante la recuperación, exactamente como el parámetro `TP_Points_Multiplier` del EA.
6. Valide el volumen calculado con los límites del instrumento (mínimo, máximo y paso) y asegúrese de que se cumpla el margen requerido.
no excede la asignación de saldo configurada o los fondos gratuitos disponibles. Si se respetan las restricciones, envíe un
orden de compra de mercado.
7. Repita el mismo proceso para rupturas bajistas, enviando en su lugar una orden de venta de mercado.

La combinación de estas reglas recrea el comportamiento del sistema MetaTrader original, incluida la transición hacia y desde
del modo de recuperación después de un evento de stop-loss.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `TakeProfitPoints` | Distancia entre el precio de entrada y el precio de obtención de beneficios expresado en incrementos de precio. | `50` |
| `BalancePercentAvailable` | Porcentaje máximo del saldo de la cuenta que se puede reservar para margen en una sola operación. | `50` |
| `TakeProfitPercentOfBalance` | Beneficio objetivo expresado como porcentaje del saldo actual. | `0.1` |
| `StopLossPercentOfBalance` | Tamaño del stop-loss expresado como porcentaje del saldo actual. | `10` |
| `RecoveryStartFraction` | Fracción del stop-loss utilizado antes de cambiar al modo de recuperación. | `0.1` |
| `RecoveryPointsMultiplier` | Multiplicador aplicado a la distancia de obtención de beneficios durante la recuperación. | `1` |
| `CandleType` | Fuente de datos de velas utilizada por la estrategia (período de tiempo, velas de tick, etc.). | `15-minute time frame` |

## Notas adicionales

- El cálculo del volumen replica el MetaTrader ayudante `CalcLotWithTP`. Deduce el tamaño del lote necesario para alcanzar el precio actual.
objetivo de ganancias para un movimiento de precio determinado y luego normaliza el resultado al paso de volumen del instrumento.
- Las comprobaciones de márgenes se realizan con el mismo espíritu que `CheckVolumeValue` y el filtro de porcentaje de saldo utilizado en MQL
versión. Las órdenes se rechazan cuando el margen requerido excede la parte permitida del saldo o los fondos libres informados por
la cartera.
- La estrategia cancela todas las órdenes activas antes de aplanar las posiciones para que el comportamiento coincida con el ayudante `CloseAllOrders` de
el asesor experto original.
- El búfer de rango interno almacena solo diez valores y equivale a iterar sobre `iHigh`/`iLow` en la fuente EA. No
Se requieren datos históricos más allá de las últimas diez velas.
