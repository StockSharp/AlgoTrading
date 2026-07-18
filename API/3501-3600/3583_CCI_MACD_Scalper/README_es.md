# CCI MACD Revendedor
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El CCI MACD Scalper traslada el MetaTrader 5 asesores expertos "CCI + MACD Scalper" a la StockSharp estrategia de alto nivel API. La conversión mantiene la pila de indicadores original (un filtro de tendencia EMA, un disparador de línea cero CCI y una verificación de divergencia {{PH009})) mientras traduce la lógica de administración del dinero a convenciones StockSharp. Las órdenes se dimensionan a partir del capital de la cartera, los stop se rechazan cuando la distancia es demasiado estrecha y un trailing stop opcional puede asegurar ganancias cerrando parcialmente posiciones después del primer ajuste. Un tiempo de reutilización de cinco velas evita que la estrategia vuelva a entrar inmediatamente después de una ejecución, replicando el comportamiento del temporizador MQL.

## Lógica estratégica
### Indicadores y procesamiento de datos.
* **Velas**: un período de tiempo configurable impulsa cada cálculo. Las señales se evalúan exclusivamente en velas completas para evitar repintarlas.
* **EMA(34)** – la media móvil exponencial del precio de cierre actúa como filtro direccional. Las posiciones largas requieren que el último cierre se ubique por encima del valor EMA anterior, las posiciones cortas requieren un cierre por debajo de él.
* **CCI(50)** – se utiliza como activador de impulso. La estrategia espera un cruce de línea cero que ocurrió en las dos velas terminadas más recientes (la vela actual confirma la configuración pero no participa en la comparación lógica).
* **MACD(12,26,9)** – las líneas principal y de señal MACD deben permanecer en el mismo lado de cero durante las dos velas anteriores. La entrada requiere que la línea de señal MACD cruce la línea principal a favor de la posición entre esas dos barras (cruce alcista para largos, cruce bajista para cortos).
* ** Amortiguadores de oscilación **: los últimos cinco máximos y mínimos de velas terminadas forman la referencia de stop-loss. Las posiciones largas se anclan al mínimo más bajo, las posiciones cortas al máximo más alto, coincidiendo exactamente con las llamadas de MetaTrader `iLowest/iHighest` con un desplazamiento de una barra.

### Reglas de entrada
* **Control de sesión**: se permite operar solo cuando la hora de cierre de la vela cae dentro de `[MinHour, MaxHour]` en la hora de la terminal local.
* **Enfriamiento**: después de cada entrada completa, el sistema espera cinco duraciones de velas antes de permitir una nueva operación, reflejando `EventSetTimer` del código original.
* **Configuración larga**
  * No hay posición larga activa (`Position <= 0`).
  * Precio de cierre por encima del valor EMA anterior.
  * CCI cruzó de negativo a positivo en las dos velas cerradas más recientes.
  * El cruce MACD se produjo por debajo de cero durante las mismas dos barras (la señal subió por encima de MACD).
  * El stop loss colocado en el mínimo más reciente satisface la restricción de distancia mínima.
* **Configuración corta**
  * No hay posición corta activa (`Position >= 0`).
  * Precio de cierre por debajo del valor EMA anterior.
  * CCI cruzó de positivo a negativo en las dos últimas velas completadas.
  * MACD se produjo un cruce por encima de cero (la señal cayó por debajo de MACD).
  * El stop loss en el máximo del swing respeta el requisito de distancia mínima.

### Gestión de riesgos y operaciones
* **Tamaño de posición dinámico**: el tamaño de la operación se deriva del `RiskPercent` configurado del capital de la cartera. El riesgo por contrato se calcula a partir de la distancia del límite de pérdidas, el paso del precio del valor y el valor del paso. El resultado se ajusta al paso de volumen del instrumento y se fija entre el volumen mínimo y máximo.
* **Stop Loss / Take Profit**: el stop loss utiliza el extremo de oscilación elegido y se rechaza cuando la distancia es inferior a `MinimalStopLossPoints`. La toma de ganancias es igual a `entry ± RiskReward × stopDistance`, lo que coincide con el cálculo de recompensa-riesgo de EA.
* **Trailing stop (opcional)**: cuando está habilitado, el stop se mueve `TrailingStopPoints` una vez que el precio cierra lo suficientemente lejos del stop anterior. El primer ajuste final desencadena una salida parcial que cierra la mitad del volumen original, reflejando fielmente la implementación de MetaTrader.
* **Salidas protectoras**: en el caso de las posiciones largas, la posición se cierra si el precio perfora el nivel de parada (mínimo de la vela) o alcanza el nivel de obtención de beneficios (máximo de la vela). Los cortos reflejan la lógica utilizando máximos y mínimos de velas respectivamente.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `CandleType` | Plazo que impulsa los cálculos del indicador. | velas de 15 minutos |
| `RiskPercent` | Porcentaje del capital de la cartera arriesgado en cada operación. | 2% |
| `RiskReward` | Multiplicador de recompensa-riesgo para el nivel de obtención de beneficios. | 1.5 |
| `EmaPeriod` | Longitud del filtro de tendencia EMA. | 34 |
| `CciPeriod` | Longitud del índice del canal de productos básicos. | 50 |
| `MinHour` | Hora más temprana (inclusive) en la que se pueden abrir nuevas operaciones. | 0 |
| `MaxHour` | Última hora (inclusive) en la que se pueden abrir nuevas operaciones. | 24 |
| `MinimalStopLossPoints` | Distancia mínima permitida entre la entrada y el stop loss expresada en puntos de precio. | 100 |
| `UseTrailingStop` | Habilita el módulo trailing stop y toma de ganancias parcial. | Discapacitado |
| `TrailingStopPoints` | Distancia del trailing stop medida en puntos de precio. | 100 |

## Notas adicionales
* La conversión de precio-punto se basa en el valor `PriceStep`. Los símbolos sin un paso válido retroceden a una distancia de una unidad de precio.
* El capital de la cartera se obtiene de `Portfolio.CurrentValue` y vuelve a caer a `BeginValue` cuando la valoración actual no está disponible. Si faltan ambos, la estrategia vuelve a la propiedad base `Volume`.
* No existe un puerto Python para esta estrategia; sólo la versión C# está incluida en el paquete API.
