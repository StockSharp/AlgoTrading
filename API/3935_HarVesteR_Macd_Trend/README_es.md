# HarVesteR MACD Estrategia de tendencias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia HarVesteR es un sistema de seguimiento de tendencias convertido del asesor MetaTrader original. Combina MACD confirmación de impulso con dos promedios móviles simples que definen la dirección de la tendencia y gestionan las salidas finales. Un filtro opcional ADX mantiene la actividad comercial centrada en movimientos direccionales fuertes.

La configuración predeterminada refleja el Asesor Experto publicado: MACD(12, 24, 9), una gestión de 50 períodos SMA, un filtro de tendencia de 100 períodos SMA y una toma de ganancias por etapas que reduce a la mitad la posición una vez que el precio supera el doble del riesgo inicial.

## Lógica de trading
1. **Sesgo de tendencia**: el SMA de 100 períodos actúa como una puerta direccional. El precio que cierra por debajo de él arma la configuración larga, mientras que el cierre por encima de él arma la configuración corta. Una vez que se realiza una operación, la bandera se reinicia hasta que el precio vuelve a cruzar al lado opuesto, evitando entradas consecutivas sin un retroceso.
2. **MACD confirmación**: una señal es válida solo si la línea MACD está en el lado esperado de cero y estuvo en el lado opuesto al menos una vez dentro de las últimas velas *Barras de confirmación*. Esto replica el bucle original que buscaba un cambio de señal dentro de una ventana deslizante.
3. **Condiciones de entrada**: las operaciones largas requieren que el cierre de la vela más el desplazamiento configurado (en puntos de precio) estén por encima de ambas SMA, MACD sea positivo y (si está habilitado) ADX supere 50. Las operaciones cortas utilizan la lógica espejo con MACD negativo y un precio por debajo de ambas SMA.
4. **Stop inicial**: el stop-loss está anclado en el precio más bajo (para largos) o más alto (para cortos) de las últimas *Stop Bars* velas completadas, haciendo coincidir las llamadas MQL `iLowest`/`iHighest` con un desplazamiento de una barra.
5. **Gestión de posición**: cuando el precio recorre una distancia igual al *Multiplicador de riesgo* multiplicado por el riesgo inicial, la mitad de la posición se cierra y el stop se mueve al punto de equilibrio. La mitad restante sale cuando el precio retrocede lo suficiente como para que el SMA de 50 períodos cruce por encima (largo) o por debajo (corto) del cierre ajustado por compensación.
6. **Salida protectora**: cualquier vela que atraviese el precio stop almacenado cierra inmediatamente toda la posición.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Fast EMA` | Corto período EMA utilizado dentro del cálculo MACD. | 12 |
| `Slow EMA` | Largo período EMA utilizado dentro del cálculo MACD. | 24 |
| `Signal EMA` | Período de suavizado para la línea de señal MACD. | 9 |
| `MACD Confirmation Bars` | Velas máximas entre lecturas opuestas de MACD requeridas antes de una nueva entrada. | 6 |
| `Trend SMA` | Longitud de la gestión SMA que custodia las salidas finales. | 50 |
| `Filter SMA` | Longitud del SMA direccional utilizado para armar configuraciones largas/cortas. | 100 |
| `Offset (points)` | Compensación (en puntos de instrumento) sumada o restada al comparar el precio con las SMA. | 10 |
| `Stop Bars` | Número de velas pasadas consideradas al establecer el stop inicial. | 6 |
| `Risk Multiplier` | Multiplicador aplicado a la distancia de riesgo inicial para desencadenar la toma de ganancias parcial. | 2.0 |
| `Use ADX` | Habilita el filtro de intensidad de tendencia ADX>50. | Discapacitado |
| `ADX Period` | ADX mirada retrospectiva utilizada cuando el filtro está activo. | 14 |
| `Candle Type` | Serie de velas suministradas a los indicadores (por defecto, barras de 1 hora). | Plazo de 1H |

## Notas de implementación
- Las compensaciones de precios se traducen a precios absolutos a través de `Security.Step` (o `Security.PriceStep` cuando esté disponible). Si el valor no expone un paso, la estrategia vuelve a `0.0001`, coincidiendo con el comportamiento del asesor original centrado en FX.
- Las salidas parciales utilizan órdenes de mercado de tamaño equivalente a la mitad de la posición actual, lo que refleja la reducción de lote realizada en la implementación de origen MQL.
- `StartProtection()` está habilitado para garantizar que la protección de posición incorporada esté activa antes de que se realicen nuevas operaciones.
- El filtro ADX es opcional; cuando está deshabilitado, el algoritmo se comporta exactamente como el script histórico al sustituir ADX por un valor artificial de 60.

## Consejos de uso
1. Configure la propiedad `Volume` antes de iniciar la estrategia; define el tamaño de orden base utilizado durante las entradas y salidas parciales.
2. Alinee el `Candle Type` con su período preferido. La estrategia original se ajustó a datos horarios, pero se pueden explorar marcos más cortos mediante la optimización de parámetros.
3. La optimización de `MACD Confirmation Bars`, `Offset (points)` y `Risk Multiplier` normalmente tiene el mayor impacto en la tasa de ganancias y la frecuencia comercial.
