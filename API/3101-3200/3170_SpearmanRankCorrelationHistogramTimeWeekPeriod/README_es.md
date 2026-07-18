# Estrategia de Spearman Rank Correlation Histogram Time Window
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el experto de MetaTrader **Exp_SpearmanRankCorrelation_Histogram_TimeWeekPeriod** en la API de alto nivel de StockSharp. Se suscribe a un único flujo de velas (por defecto: barras de 4 horas) y evalúa el histograma de correlación de rango Spearman publicado en el indicador MQL original. El color del histograma determina si la tendencia a corto plazo es alcista (valores por encima de cero) o bajista (valores por debajo de cero). Una ventana de negociación dedicada mantiene la actividad entre un rango configurable de día de la semana/hora, reflejando los controles `TimeTrade` del código fuente.

## Lógica de negociación
1. **Cálculo del indicador**
   - En cada vela terminada la estrategia almacena el precio de cierre y calcula la correlación de rango Spearman sobre `RangeLength` cierres.
   - El color del histograma se asigna exactamente como en el indicador: `4` cuando la correlación está por encima de `HighLevel`, `3` cuando está entre `0` y `HighLevel`, `1` cuando está entre `LowLevel` y `0`, `0` cuando está por debajo de `LowLevel`, y `2` cuando es exactamente cero.
   - Las señales se evalúan en la barra cerrada número `SignalBar` (por defecto: la barra que acaba de cerrar). La barra cerrada anterior se usa para detectar transiciones de color.

2. **Modos de operación** – el parámetro `TradeMode` controla cómo se interpretan los colores:
   - **Mode1** – abrir largos cuando el color salta por encima de `2` después de estar por debajo de `3`; abrir cortos cuando el color cae por debajo de `2` después de estar por encima de `1`. Cada color alcista también solicita un cierre corto, cada color bajista un cierre largo.
   - **Mode2** – abrir largos en color `4` (transición desde cualquier cosa por debajo de `4`), abrir cortos en color `0` (transición desde cualquier cosa por encima de `0`). Colores mayores que `2` cierran cortos; colores menores que `2` cierran largos.
   - **Mode3** – abrir largos en color `4` y cerrar cortos al mismo tiempo; abrir cortos en color `0` y cerrar largos simultáneamente.
   - Después de una entrada exitosa la estrategia impone un tiempo de espera igual a la longitud de la vela (la siguiente orden en la misma dirección se difiere hasta que la siguiente barra habría cerrado en MetaTrader).

3. **Gestión monetaria y tamaño de orden**
   - `MoneyManagement` combinado con `MarginMode` convierte fracciones de capital o riesgo en un volumen de orden. Los valores positivos siguen las reglas de gestión monetaria originales, cero recurre al `Volume` de la estrategia, y los números negativos se interpretan como un tamaño de lote fijo.
   - Los modos basados en riesgo (`LossFreeMargin`, `LossBalance`) requieren un `StopLossPoints` positivo. Si el stop es cero la estrategia recurre a `Volume` igual que el EA rechazaría la operación.

4. **Gestión de riesgo**
   - `StopLossPoints` y `TakeProfitPoints` se traducen en niveles de precio usando `Security.PriceStep`. Las salidas se comprueban en cada vela terminada usando el máximo/mínimo de la vela y todas las posiciones abiertas se revierten a plano cuando se toca un nivel.
   - `DeviationPoints` se preserva por completitud de la interfaz; las órdenes de mercado de StockSharp ignoran el valor.

5. **Ventana de negociación semanal**
   - Cuando `TimeTrade` es `true` la hora actual debe estar entre (`StartDay`, `StartHour`, `StartMinute`, `StartSecond`) y (`EndDay`, `EndHour`, `EndMinute`, `EndSecond`). Fuera de esa ventana todas las posiciones en el instrumento de la estrategia se cierran inmediatamente, coincidiendo con la salida de emergencia original.
   - La implementación asume que `StartDay` no es posterior a `EndDay`. Para sesiones superpuestas (por ejemplo viernes → lunes) ajustar los parámetros en consecuencia.

6. **Comportamiento misceláneo**
   - Al menos `RangeLength + SignalBar + 1` velas completadas deben estar disponibles antes de que se puedan generar señales.
   - `Direction` es un interruptor reservado del indicador MQL; se mantiene por paridad de parámetros pero no tiene efecto en este port.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `MoneyManagement` | Fracción de capital o tamaño de lote fijo para dimensionamiento de posición. | `0.1` |
| `MarginMode` | Interpretación de `MoneyManagement` (`FreeMargin`, `Balance`, `LossFreeMargin`, `LossBalance`, `Lot`). | `Lot` |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio. | `1000` |
| `TakeProfitPoints` | Distancia de take-profit en puntos de precio. | `2000` |
| `DeviationPoints` | Tolerancia de deslizamiento informativa en puntos. | `10` |
| `BuyOpen` / `SellOpen` | Habilitar abrir posiciones largas o cortas. | `true` |
| `BuyClose` / `SellClose` | Permitir cerrar posiciones largas o cortas en señales. | `true` |
| `TradeMode` | Modo de interpretación del histograma (`Mode1`, `Mode2`, `Mode3`). | `Mode1` |
| `TimeTrade` | Activar la ventana de negociación semanal. | `true` |
| `StartDay`, `StartHour`, `StartMinute`, `StartSecond` | Inicio de la ventana (día de la semana y hora). | `Martes`, `8`, `0`, `0` |
| `EndDay`, `EndHour`, `EndMinute`, `EndSecond` | Fin de la ventana (día de la semana y hora). | `Viernes`, `20`, `59`, `40` |
| `CandleType` | Marco temporal de las velas procesadas. | `H4` |
| `RangeLength` | Número de cierres usados por la correlación Spearman. | `14` |
| `MaxRange` | `RangeLength` máximo permitido (guardia de seguridad). | `30` |
| `Direction` | Indicador de indicador reservado, sin efecto en el port. | `true` |
| `HighLevel`, `LowLevel` | Umbrales superiores e inferiores del histograma. | `0.5`, `-0.5` |
| `SignalBar` | Número de barras cerradas hacia atrás al leer el buffer de color. | `1` |

Toda la demás configuración de la estrategia (selección de portafolio, asignación de seguridad, `Volume` base, reglas de riesgo) sigue el flujo de trabajo estándar de StockSharp.
