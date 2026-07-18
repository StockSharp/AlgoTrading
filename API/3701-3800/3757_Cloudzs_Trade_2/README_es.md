# Estrategia Cloudzs Trade 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Cloudzs Trade 2** es una StockSharp versión del MetaTrader 4 asesor experto `cloudzs_trade_2`. El robot original combina inversiones de oscilador estocástico con un filtro de confirmación de doble fractal y utiliza una lógica de seguimiento agresiva para proteger las posiciones abiertas. Esta versión de C# recrea el flujo de señales y las reglas de gestión comercial al tiempo que expone los parámetros como objetos `StrategyParam` para que puedan optimizarse o ajustarse desde la interfaz de usuario StockSharp.

La estrategia observa una única serie de velas (plazo de tiempo configurable) y evalúa dos condiciones independientes:

1. **Stochastic reversión**: se activa cuando la línea %D abandona una zona extrema (>= 80 para ventas, <= 20 para compras) al tiempo que confirma que %D cruzó la línea %K en la vela anterior, coincidiendo estrechamente con la lógica MQL original.
2. **Confirmación de doble fractal**: espera hasta que aparezcan dos señales fractales consecutivas del mismo tipo (dos fractales superiores para ventas o dos fractales inferiores para compras).

Si cualquiera de las condiciones genera una solicitud de compra o venta, la estrategia entra en esa dirección (siempre que no haya ninguna operación activa y la salida anterior haya sido en un día diferente). Cuando ya se está en una operación, se pueden utilizar las mismas condiciones para salir anticipadamente si `CloseOnOpposite` está habilitado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `LotSplitter` | Coeficiente utilizado para aproximar el volumen comercial del valor de la cuenta corriente. | `0.1` |
| `MaxVolume` | Límite superior para el volumen calculado (0 desactiva el límite). | `0` |
| `TakeProfitOffset` | Distancia de toma de ganancias fija en unidades de precio absoluto. | `0` |
| `TrailingStopOffset` | Distancia del trailing stop en unidades de precio. | `0.01` |
| `StopLossOffset` | Distancia fija de stop-loss en unidades de precio. | `0.05` |
| `MinProfitOffset` | Beneficio mínimo a mantener después de una excursión favorable una vez que se alcanzó `ProfitPointsOffset`. | `0` |
| `ProfitPointsOffset` | Se requiere una medida favorable antes de que se aplique `MinProfitOffset`. | `0` |
| `%K Period` / `%D Period` / `Slowing` | Stochastic configuración del oscilador. | `8 / 8 / 4` |
| `Method` | Identificador de método estocástico MT4 original (informativo, no utilizado porque StockSharp expone una única implementación). | `3` |
| `PriceMode` | Identificador de modo de precio MT4 original (solo informativo). | `1` |
| `UseStochasticCondition` | Habilite la generación de señales basada en estocástica. | `true` |
| `UseFractalCondition` | Habilite la generación de señales basadas en fractales. | `true` |
| `CloseOnOpposite` | Cerrar la posición activa cuando aparezca la señal contraria. | `true` |
| `CandleType` | Marco temporal/tipo de datos utilizado para los cálculos. | `15-minute time frame` |

## Señales comerciales
### Entrada larga
- La línea %D está por debajo o es igual a 20 y cruza por debajo de %K (que coincide con la comparación de velas anteriores de MT4).
- **O** se detectan dos fractales inferiores secuenciales.
- No hay ninguna posición abierta y la última salida se produjo en un día calendario diferente.

### Entrada corta
- La línea %D es superior o igual a 80 y cruza por encima de %K.
- **O** aparecen dos fractales superiores secuenciales.
- No hay ninguna posición abierta y la última salida se produjo en un día calendario diferente.

### Reglas de salida
- Se alcanzan niveles estrictos de stop-loss o take-profit (si están configurados).
- El trailing stop se mueve a favor de la operación y el precio toca el nivel de stop actualizado.
- Después de que la posición experimenta `ProfitPointsOffset` movimiento favorable, un retroceso a `MinProfitOffset` cierra la operación.
- Reversión anticipada opcional: si `CloseOnOpposite` es verdadero y se activa la señal opuesta, la operación se cierra.

## Gestión del riesgo
- Las distancias de stop-loss y take-profit imitan las compensaciones de pips sin procesar del código MT4 (interpretadas aquí como diferencias de precios).
- Los trailingstops se actualizan utilizando el precio de cierre y sólo se mueven en la dirección rentable.
- El parámetro `LotSplitter` intenta seguir la fórmula de volumen original, escalando el tamaño de la operación por valor de la cuenta y limitándolo con `MaxVolume`.

## Notas y limitaciones
- El StockSharp `StochasticOscillator` expone una única implementación de suavizado; por lo tanto, los parámetros `Method` y `PriceMode` se mantienen como referencia pero no cambian el comportamiento del indicador.
- El script MT4 original funcionó paso a paso. Este puerto evalúa las señales de las velas terminadas para alinearse con las mejores prácticas de StockSharp.
- El cálculo del volumen se basa en los valores de cartera disponibles; si no existe información de la cuenta, vuelve al valor `LotSplitter`.

## Uso
1. Agregue la estrategia a su proyecto StockSharp y seleccione el instrumento con el que desea operar.
2. Configure el período de tiempo de la vela y ajuste la configuración estocástica/fractal si es necesario.
3. Proporcione compensaciones realistas de stop-loss/take-profit que coincidan con el tamaño del tick del instrumento.
4. Inicie la estrategia en Designer, Runner o mediante API y supervise los mensajes de registro para obtener información de señales.
