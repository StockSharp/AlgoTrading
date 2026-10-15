# Estrategia Abrir Cerrar (ID 3996)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el MetaTrader4 experto `open_close.mq4`. Funciona en un solo instrumento y compara la apertura y el cierre de la última vela con la anterior. Cuando no hay ninguna posición activa, se desvanecen los fuertes movimientos de una barra (patrones de brecha y reversión). Mientras está en una operación, cierra la posición cuando el patrón se invierte o cuando se supera un umbral de protección de pérdidas flotantes.

## Lógica de trading
### Reglas de entrada
- Se comercializa solo cuando se ha procesado la vela anterior (la guardia `Volume[0] == 1` original).
- Entrada larga: la vela actual se abre por encima de la apertura anterior **y** cierra por debajo del cierre anterior. La estrategia compra el volumen configurado en el mercado.
- Entrada corta: la vela actual se abre por debajo de la apertura anterior **y** cierra por encima del cierre anterior. La estrategia vende en corto en el mercado.

Sólo una posición puede estar activa en cualquier momento. Las nuevas señales se ignoran hasta que se cierra la posición abierta.

### reglas de salida
1. **Protección contra riesgos:** el PnL flotante se mide a partir del precio de entrada promedio. Si la pérdida no realizada supera `MaximumRisk × Portfolio.CurrentValue`, la estrategia cierra inmediatamente la posición. La versión original MQL utilizaba `AccountMargin`, que aquí se aproxima con la mejor valoración de cartera disponible.
2. **Inversión de patrón:**
   - Las posiciones largas se cierran cuando la siguiente vela continúa hacia abajo (`open < previous open` y `close < previous close`).
   - Las posiciones cortas se cierran cuando la siguiente vela continúa hacia arriba (`open > previous open` y `close > previous close`).

## Dimensionamiento de posiciones
- El tamaño del pedido predeterminado se deriva de `MaximumRisk`. La estrategia multiplica el valor de la cuenta disponible por `MaximumRisk` y divide el resultado por `1000`, imitando el cálculo MetaTrader de `AccountFreeMargin * MaximumRisk / 1000`.
- Si la información de la cuenta no está disponible, se utiliza el parámetro alternativo `InitialVolume`.
- Después de más de una operación perdedora consecutiva, el tamaño del lote se reduce en `volume × losses / DecreaseFactor`, reproduciendo el bucle MetaTrader en el historial de operaciones cerradas.
- Se aplica un volumen negociable mínimo de `0.1` lotes antes de alinear la cantidad con el paso de volumen del instrumento y los límites de intercambio.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `InitialVolume` | `decimal` | `0.1` | Tamaño de lote alternativo utilizado cuando la información sobre el patrimonio no está disponible. |
| `MaximumRisk` | `decimal` | `0.3` | Fracción del valor de la cuenta que controla tanto el tamaño de la posición como la pérdida flotante máxima tolerada. |
| `DecreaseFactor` | `decimal` | `100` | Factor de reducción aplicado después de más de una operación perdedora consecutiva. |
| `CandleType` | `DataType` | `15m` período de tiempo | Serie de velas utilizadas para evaluar el patrón. |

## Notas de implementación
- La estrategia se suscribe a la serie de velas seleccionada y procesa **solo velas terminadas**, que coinciden con la condición `Volume[0] > 1` en el experto original.
- El PnL flotante se estima a partir de la posición actual de la estrategia y el último precio de cierre porque StockSharp no expone las métricas `AccountProfit` y `AccountMargin` de MetaTrader.
- Las pérdidas consecutivas se rastrean a través de operaciones completadas, lo que permite que `DecreaseFactor` se comporte como el bucle original a lo largo del historial de operaciones.
- La alineación del volumen respeta `Security.VolumeStep`, `MinVolume` y `MaxVolume` para seguir siendo compatible con los requisitos de intercambio.
- Los gráficos se completan con velas y operaciones propias cuando hay un área del gráfico disponible para la depuración visual.

## Consejos de uso
- Elija un intervalo de vela que coincida con el utilizado en MetaTrader al calibrar el experto original.
- Ajuste `MaximumRisk` y `DecreaseFactor` para ajustar la agresividad de la regla de tamaño de lote.
- Debido a que la estrategia es contraria, funciona mejor en instrumentos que exhiben frecuentes sobreextensiones de una sola barra y movimientos de retroceso.
