# Estrategia inteligente de seguimiento de tendencias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Smart Trend Follower** es una StockSharp versión del MetaTrader 5 asesores expertos *Smart Trend Follower*. el
El sistema original alterna entre un cruce de media móvil contraria y una configuración de seguimiento de tendencias que utiliza estocástico.
confirmación. Escala posiciones con un multiplicador de volumen similar a una martingala y mantiene una toma de ganancias/stop-loss compartida.
para cada cesta direccional. La versión StockSharp mantiene el mismo comportamiento mientras usa el nivel alto API (vela
suscripciones, vinculaciones de indicadores y órdenes de mercado).

## Lógica de señal
Hay dos motores de señal independientes disponibles y se pueden cambiar con el parámetro `SignalMode`:

1. **CrossMa** – replica el crossover contrario original. Cuando el rápido SMA cruza *debajo* del lento SMA (rápido < lento
pero antes rápido > lento) la estrategia abre o promedia posiciones largas. Cuando el rápido SMA cruza *por encima* del lento
SMA (rápido > lento pero antes rápido < lento) abre o promedia cortos.
2. **Tendencia**: sigue el modo de tendencia original que requiere confirmación del oscilador estocástico. Una señal alcista
aparece cuando el SMA rápido se mantiene por encima del SMA lento, la vela cierra más alto de lo que abrió y el valor estocástico %K
está en 30 o menos. Una señal bajista requiere rápido < lento, un cuerpo de vela bajista y %K estocástico en 70 o más.

Las señales se evalúan únicamente en velas terminadas. Cada vez que llega una nueva señal mientras las posiciones opuestas aún están abiertas, el
La estrategia primero liquida la canasta opuesta y solo luego procesa nuevas entradas para mantenerse alineada con la dirección de la canasta.
señal actual.

## Escala de posición
La estrategia reproduce la lógica de la martingala MQL:

- El primer pedido utiliza `InitialVolume` lotes.
- Cada pedido promedio adicional multiplica el volumen anterior por `Multiplier` (los valores ≤ 1 desactivan el crecimiento del volumen).
- Se permite una nueva orden promedio para la dirección activa solo después de que el mercado se haya movido `LayerDistancePips` pips de distancia
del mejor precio de entrada de la cesta actual (lleno largo más bajo o relleno corto más alto).
- Los volúmenes se normalizan utilizando los límites del instrumento `VolumeStep`, `VolumeMin` y `VolumeMax` cuando estén disponibles.

## Gestión del riesgo
Para cada cesta direccional, la estrategia rastrea un precio de equilibrio compartido (promedio ponderado por volumen de todos los rellenos):

- `TakeProfitPips` define la distancia entre el precio medio de entrada y la obtención de beneficios de la cesta. Las cestas largas salen cuando el
el máximo de la vela toca ese nivel, cestas cortas cuando el mínimo de la vela lo alcanza. Establezca en 0 para deshabilitar los objetivos de obtención de beneficios.
- `StopLossPips` refleja el comportamiento de las salidas de protección. Las cestas largas se cierran cuando el mínimo de la vela cae por debajo del stop,
cestas cortas cuando la vela alta cruza por encima de ella. Establezca en 0 para desactivar la parada de protección.

Las órdenes de salida se ejecutan mediante órdenes de mercado cuando la siguiente vela terminada confirma que se ha alcanzado el nivel. el
La estrategia mantiene los indicadores `_longExitRequested` y `_shortExitRequested` para evitar envíos de salida duplicados mientras se completan.
aún pendiente.

## Parámetros
| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `SignalMode` | enumeración (`CrossMa`, `Trend`) | `CrossMa` | Selecciona el motor de señal (cruce contrario o tendencia con filtro estocástico). |
| `CandleType` | `DataType` | plazo de 30 minutos | Serie de velas primarias utilizadas para cálculos y generación de señales. |
| `InitialVolume` | decimales | `0.01` | Tamaño base del pedido en lotes para la primera entrada de cualquier cesta. |
| `Multiplier` | decimales | `2` | Multiplicador de volumen aplicado a cada orden promedio adicional. |
| `LayerDistancePips` | decimales | `200` | Distancia mínima de pips desde la mejor entrada antes de agregar otra orden en la misma dirección. |
| `FastPeriod` | entero | `14` | Período de la media móvil simple rápida. |
| `SlowPeriod` | entero | `28` | Periodo de la media móvil simple lenta (debe ser mayor que `FastPeriod`). |
| `StochasticKPeriod` | entero | `10` | Longitud retrospectiva de la línea %K del oscilador estocástico. |
| `StochasticDPeriod` | entero | `3` | Longitud de suavizado para la línea estocástica %D. |
| `StochasticSlowing` | entero | `3` | Suavizado adicional aplicado a %K antes del cálculo de %D. |
| `TakeProfitPips` | decimales | `500` | Distancia en pips desde la entrada promedio donde se coloca la toma de ganancias de la canasta. Establezca 0 para desactivar. |
| `StopLossPips` | decimales | `0` | Distancia de parada de protección en pips. Establezca 0 para desactivar la parada brusca. |

## Notas de implementación
- El tamaño del pip se deriva del instrumento `PriceStep` y `Decimals`, coincidiendo con la noción MetaTrader de "punto" (p. ej.
0,0001 para cotizaciones FX de 5 dígitos).
- El seguimiento de posición utiliza dos listas de objetos `PositionEntry` para reflejar la contabilidad por ticket de MetaTrader. Las entradas son
estilo FIFO reducido cuando las operaciones opuestas cierran parte de una canasta.
- Todos los cálculos de indicadores se basan en el enlace de alto nivel API (`SubscribeCandles().BindEx(...)`) de StockSharp. Sin llamadas manuales
a `GetValue` son obligatorios y los indicadores nunca se inyectan en `Strategy.Indicators`.
- La estrategia llama a `StartProtection()` al inicio, lo que permite a StockSharp gestionar módulos globales de control de riesgos (punto de equilibrio,
controles de margen, etc.).
- Debido a que StockSharp consolida posiciones netas por dirección, las posiciones opuestas se cierran por completo antes de que se realicen nuevas entradas.
evaluado. Esto mantiene la implementación determinista y estrechamente alineada con el comportamiento original de EA.

## Archivos
- `CS/SmartTrendFollowerStrategy.cs` – Implementación en C# de la estrategia utilizando la API de alto nivel de StockSharp.
