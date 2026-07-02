# Nueva estrategia FSCEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La nueva estrategia FSCEA es un sistema de seguimiento de tendencias basado en MACD que fue transferido del MetaTrader 4 asesor experto original `new_fscea.mq4`. La estrategia combina una confirmación cruzada clásica MACD con un filtro de pendiente EMA, objetivos estáticos de obtención de beneficios y un trailing stop para gestionar las posiciones abiertas. Opera con un solo símbolo a la vez y abre solo una posición en el mercado.

## Lógica de trading
### Entrada larga
- La línea principal MACD está por debajo de cero, pero cruza por encima de la línea de señal en la vela cerrada actual.
- La vela anterior todavía tenía la línea MACD debajo de la línea de señal (confirma el cruce).
- El valor absoluto de la línea MACD supera el umbral `OpenLevelPoints` (escalado por paso de precio).
- La pendiente EMA desplazada es positiva (`EMA_shifted_now > EMA_shifted_previous`).
- Actualmente no hay ninguna posición abierta.

### Entrada corta
- La línea principal MACD está por encima de cero, pero cruza por debajo de la línea de señal en la vela cerrada actual.
- La vela anterior todavía tenía la línea MACD encima de la línea de señal.
- La línea principal MACD supera el umbral `OpenLevelPoints` (escalado por paso de precio).
- La pendiente EMA desplazada es negativa (`EMA_shifted_now < EMA_shifted_previous`).
- Actualmente no hay ninguna posición abierta.

### Salida larga
- Se activa cuando MACD cruza por debajo de la línea de señal mientras permanece por encima de cero y el valor de MACD excede el umbral de `CloseLevelPoints`.
- O cuando el máximo de la vela toca el nivel virtual de obtención de beneficios (`entry + TakeProfitPoints * priceStep`).
- O cuando el mínimo de la vela alcanza el nivel del trailing-stop (actualizado dinámicamente a medida que el precio se mueve a favor).

### Salida corta
- Se activa cuando MACD cruza por encima de la línea de señal mientras permanece por debajo de cero y el valor absoluto de MACD excede el umbral de `CloseLevelPoints`.
- O cuando el mínimo de la vela toque el nivel virtual de obtención de beneficios (`entry - TakeProfitPoints * priceStep`).
- O cuando el máximo de la vela alcanza el nivel del trailing-stop (actualizado dinámicamente a medida que el precio se mueve a favor).

## Gestión del riesgo
- El take-profit se expresa en puntos del instrumento y se convierte en precio multiplicando por `Security.PriceStep`.
- El trailing stop funciona en puntos y se ajusta una vez que el beneficio flotante es mayor que la distancia de seguimiento.
- Sólo se puede abrir una posición a la vez, reflejando el comportamiento del asesor experto de MT4.
- La protección de posición se habilita a través del asistente integrado `StartProtection()`.

## Indicadores
- **MACD (12, 26, 9)** – el motor crossover principal. La magnitud del histograma proporciona los umbrales de entrada y salida.
- **EMA (TrendPeriod)** – se aplica a los precios de cierre. La comparación de pendientes utiliza un desplazamiento configurable (`TrendShift`) para emular el parámetro MT4 `ma_shift`.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `TakeProfitPoints` | 300 | Distancia al objetivo de ganancias en puntos. Convertido a precio utilizando el paso de precio del símbolo. |
| `TrailingStopPoints` | 20 | Tamaño del trailing stop en puntos. Se activa solo después de que el comercio se mueve a favor por más de esta distancia. |
| `OpenLevelPoints` | 3 | Se requiere una magnitud mínima de MACD (puntos) antes de permitir una nueva operación. |
| `CloseLevelPoints` | 2 | MACD magnitud (puntos) requerida para cerrar una operación a través de MACD cruce. |
| `TrendPeriod` | 10 | Longitud del filtro de tendencia EMA. |
| `TrendShift` | 2 | Desplazamiento horizontal (en barras) aplicado al EMA al evaluar su pendiente. Los valores más altos retrasan la confirmación de la tendencia. |
| `TradeVolume` | 0.1 | Volumen de órdenes predeterminado enviado con órdenes de mercado. |
| `CandleType` | plazo de 1 hora | Tipo de vela utilizado para los cálculos de indicadores; se puede cambiar para que coincida con el período de tiempo deseado. |

## Notas de implementación
- La estrategia solo procesa velas terminadas para mantener la lógica cercana a la versión MT4.
- El cambio EMA se emula almacenando en búfer las salidas del indicador y comparando los valores con una separación de `TrendShift` barras.
- El trailing stop y la toma de ganancias se implementan virtualmente (sin órdenes stop/límite reales) para mantenerse dentro de los requisitos de alto nivel API.
- El código se basa exclusivamente en la suscripción de vela de alto nivel API (`SubscribeCandles().BindEx(...)`) para cumplir con las pautas del repositorio.
