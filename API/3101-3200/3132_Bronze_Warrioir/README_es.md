# Estrategia de Bronze Warrioir
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del experto MetaTrader 5 *Bronze Warrioir.mq5* a la API de alto nivel de StockSharp.
- Opera con un único símbolo usando velas completadas y combina CCI, Williams %R y un oscilador propietario "DayImpuls".
- Orientado a capturar impulsos de momentum cuando la pendiente de DayImpuls, los extremos de Williams %R y las lecturas de CCI se alinean.

## Conjunto de indicadores
- **Commodity Channel Index (CCI)** – CCI clásico con el `IndicatorPeriod` configurado. Las señales largas requieren un valor por debajo de `-CciLevel`; las señales cortas necesitan un valor por encima de `CciLevel`.
- **Williams %R** – aplicado en el mismo período. Un valor por encima de `WilliamsLevelUp` confirma territorio de sobrecompra, mientras que valores por debajo de `WilliamsLevelDown` confirman niveles de sobreventa.
- **Oscilador DayImpuls** – réplica del indicador personalizado incluido. Convierte cada cuerpo de vela en puntos (cierre menos apertura dividido entre el valor de punto del instrumento) y aplica dos medias móviles exponenciales consecutivas con el mismo período. Los valores crecientes indican presión alcista; los valores decrecientes indican presión bajista.

## Lógica de trading
1. **Protección del patrimonio** – antes de generar cualquier señal, la estrategia acumula el PnL flotante de la exposición actual. Si sube por encima de `ProfitTarget` o cae por debajo de `LossTarget`, todas las posiciones abiertas se cierran inmediatamente.
2. **Filtro de entrada** – las velas completadas son obligatorias. El algoritmo requiere un valor DayImpuls almacenado de la barra anterior para emular el look-back original usando `custom[1]`.
3. **Configuración corta** – activada cuando:
   - No hay exposición corta activa.
   - DayImpuls está por encima de `DayImpulsLevel` y es mayor que su valor anterior (momentum positivo).
   - Williams %R está por encima de `WilliamsLevelUp` (sobrecompra) y CCI es mayor que `CciLevel`.
   - Las órdenes usan `TradeVolume` más cualquier volumen largo abierto para revertir en una sola transacción dentro del modelo de netting de StockSharp.
4. **Configuración larga** – condiciones simétricas:
   - Sin exposición larga activa.
   - DayImpuls está por debajo de `DayImpulsLevel` y es menor que su valor anterior (momentum decreciente).
   - Williams %R está por debajo de `WilliamsLevelDown` y CCI es menor que `-CciLevel`.
   - Usa `TradeVolume` más cualquier volumen corto pendiente para una reversión completa cuando sea necesario.
5. **Reversiones tipo hedge** – cuando solo hay exposición en una dirección y el PnL flotante sale del rango `[-PredTarget / 2, PredTarget]`, el EA valida el paso martingala a través del parámetro `LotCoefficient`. En el port de StockSharp, la validación se preserva pero la ejecución real realiza una orden de cierre y reversión porque la plataforma mantiene posiciones netas en lugar de tickets independientes con hedge.

## Gestión de riesgos
- `StopLossPips` y `TakeProfitPips` se convierten en distancias de precio usando el `PriceStep` del instrumento. Para símbolos forex de 3 o 5 dígitos se aplica un factor adicional de 10 para emular los "pips" de MetaTrader.
- Ambos valores se pasan al helper de alto nivel `StartProtection`, que adjunta niveles automáticos de stop-loss y take-profit a la posición activa.
- La estrategia mantiene seguimiento interno de volumen largo/corto para que `GetOpenPnL` coincida con el cálculo de MetaTrader que suma `Commission + Swap + Profit` para cada ticket.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Volumen base de la orden en lotes. | `1` |
| `StopLossPips` | Stop protector en pips convertido a distancia de precio. | `50` |
| `TakeProfitPips` | Objetivo de beneficio en pips convertido a distancia de precio. | `50` |
| `IndicatorPeriod` | Período aplicado a CCI, Williams %R y DayImpuls. | `14` |
| `CciLevel` | Umbral absoluto de CCI para operaciones. | `150` |
| `WilliamsLevelUp` | Nivel de sobrecompra de Williams %R (valor negativo). | `-15` |
| `WilliamsLevelDown` | Nivel de sobreventa de Williams %R (valor negativo). | `-85` |
| `DayImpulsLevel` | Umbral de DayImpuls que separa regímenes alcistas/bajistas. | `50` |
| `ProfitTarget` | Objetivo de beneficio flotante en divisa de cuenta. | `100` |
| `LossTarget` | Límite de pérdida flotante en divisa de cuenta. | `-100` |
| `PredTarget` | Rango usado para activar reversiones de promediado. | `40` |
| `LotCoefficient` | Coeficiente de validación heredado del EA. | `2` |
| `CandleType` | Marco temporal usado para todos los indicadores. | Velas de `15m` |

## Notas de implementación
- El oscilador DayImpuls está integrado como clase de indicador interno y refleja la lógica original de suavizado doble EMA.
- Dado que las estrategias de StockSharp gestionan posiciones netas, los hedges simultáneos largo/corto de la versión MQL se emulan combinando volúmenes de cierre y apertura dentro de la misma orden de mercado.
- La estrategia solo funciona con velas completadas y usa `IsFormedAndOnlineAndAllowTrading()` para respetar el ciclo de vida global de la estrategia.
- Los precios promedio largo/corto se rastrean a través de `OnOwnTradeReceived` para que los cierres parciales y reversiones actualicen correctamente el PnL flotante.
