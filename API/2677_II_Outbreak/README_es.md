# Estrategia de Rompimiento II
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Rompimiento II es un sistema de ruptura de alta frecuencia escrito originalmente para MetaTrader 4. Combina un oscilador de timing propietario con un indicador de presión de volatilidad para entrar en movimientos direccionales fuertes, luego gestiona las operaciones usando trailing stops adaptativos y piramidación. Esta conversión reproduce la lógica original sobre la API de alto nivel de StockSharp y mantiene los mismos controles para filtros de spread, volatilidad y calendario.

## Lógica de trading convertida
### Oscilador de timing
* Cada nueva vela M1 contribuye con un "precio típico" (promedio de high, low y close multiplicado por 100) que alimenta la cascada de suavizado heredada.
* La cascada reconstruye la tubería original de media móvil anidada / diferencia (buffers dtemp/atemp) para producir un valor de timing de 0 a 100.
* Señal de compra: el valor de timing cruza hacia arriba sobre su lectura anterior (buffer[0] > buffer[1] con buffer[1] ≤ buffer[2]).
* Señal de venta: el valor de timing cruza hacia abajo (buffer[0] < buffer[1] con buffer[1] ≥ buffer[2]).

### Filtro de volatilidad
* Una desviación estándar de 10 períodos sobre precios de cierre debe mantenerse por debajo de `StdDevLimit`. Cuando se viola el límite, no se permiten nuevas posiciones y opcionalmente se registra una advertencia.
* Una puntuación de volatilidad personalizada replica la fórmula de amplitud × densidad de ticks original: usa la superposición entre la vela actual y la anterior y el número promedio de ticks por segundo. La puntuación debe superar el `VolatilityThreshold` configurable.

### Reglas de entrada
* La estrategia trabaja con un único par símbolo/marco temporal suministrado a través del parámetro `CandleType` (por defecto velas de 1 minuto).
* Cuando no hay posición abierta y el filtro de calendario permite el trading, el motor actualiza el tamaño del lote a través de `CalculateOrderVolume()` y verifica el spread actual contra `SpreadThreshold` (usando datos bid/ask de nivel 1).
* Se abre una posición larga si el oscilador de timing emite una señal de compra y la puntuación de volatilidad es válida. Una posición corta sigue la condición espejada. Al entrar, se coloca un stop estático a dos veces `TrailStopPoints` por debajo/encima del precio de ejecución.

### Piramidación y trailing
* El módulo de trailing se activa una vez que la posición agregada gana al menos `TrailStopPoints + int(Commission) + SpreadThreshold` puntos de beneficio no realizado.
* El stop se ajusta a `TrailStopPoints` detrás del último cierre (rastreado por separado para largos y cortos). Cualquier mejora mayor a un punto actualiza el precio de trailing.
* Mientras las condiciones de volatilidad, timing y spread permanezcan válidas, la estrategia puede piramidear nuevas órdenes cada `max(10, SpreadThreshold + 1)` puntos de beneficio adicional. Las nuevas órdenes deshabilitan el stop estático y dependen puramente de la lógica de trailing.

### Gestión de riesgo y capital
* El tamaño de la posición se recalcula antes de cada orden: `balance × MaximumRisk ÷ (500000 / AccountLeverage)` redondeado al paso de volumen del instrumento. Si la información del balance no está disponible, se usa `Volume` o el lote mínimo.
* Una verificación de margen simplificada aproxima la guardia original de MetaTrader (`volume × price / leverage × (1 + MaximumRisk × 190)`). Las órdenes se ignoran si el valor de la cuenta no puede cubrir esa cantidad.
* Después de activarse la piramidación, la estrategia monitorea la pérdida flotante. Cuando la caída no realizada supera `TotalEquityRisk` por ciento del valor de la cuenta, todas las posiciones se liquidan.

### Controles de calendario y spread
* El trading se detiene los viernes después de las 23:00 hora del servidor y durante los últimos días de trading del año (días del año 358, 359, 365 o 366) después de las 16:00.
* Cada entrada y adición verifica el spread bid/ask actual y omite la ejecución si supera el umbral configurado.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `Commission` | 4 | Comisión de lote completo en puntos usada al calcular el desplazamiento de activación del trailing. |
| `SpreadThreshold` | 6 | Spread máximo (en puntos) permitido para nuevas entradas o piramidación. |
| `TrailStopPoints` | 20 | Distancia del trailing stop en puntos; el stop inicial es el doble de este valor. |
| `TotalEquityRisk` | 0.5 | Porcentaje de pérdida de patrimonio de la cuenta que activa una salida forzada tras la piramidación. |
| `MaximumRisk` | 0.1 | Fracción del balance de la cuenta asignada a cada orden al dimensionar el volumen. |
| `StdDevLimit` | 0.002 | Desviación estándar máxima de 10 períodos para aceptar nuevas operaciones. |
| `VolatilityThreshold` | 800 | Puntuación de volatilidad mínima (amplitud × densidad de ticks) requerida para operar. |
| `AccountLeverage` | 100 | Apalancamiento de la cuenta usado en la aproximación del margen y el dimensionamiento de la posición. |
| `WarningAlerts` | true | Habilita el registro cuando el filtro de desviación estándar bloquea entradas. |
| `CandleType` | 1 minuto | Tipo de vela usado para todos los cálculos. |

## Indicadores
* `StandardDeviation(Length = 10)` sobre precios de cierre para el filtro de volatilidad.
* Oscilador de timing personalizado reproducido del EA original (implementado en línea sin objetos de indicador StockSharp).

## Notas de implementación
* El filtro de spread requiere datos de nivel 1 en vivo (`Security.BestBid`/`BestAsk`). Cuando el feed está ausente, la estrategia asume spread cero.
* Las verificaciones de margen y patrimonio son aproximaciones porque el EA original dependía de propiedades de cuenta y tamaños de contrato específicos de MetaTrader. Ajusta `AccountLeverage`, `MaximumRisk` o `Volume` para adaptarlos al modelo del broker.
* La conversión usa la API de alto nivel de StockSharp (suscripciones de velas con `Bind`) y mantiene todos los comentarios en inglés. No se genera un port de Python para esta estrategia.
