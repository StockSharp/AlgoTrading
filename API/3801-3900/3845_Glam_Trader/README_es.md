# Glam Trader (Confirmación de múltiples períodos de tiempo)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el asesor experto original MetaTrader "GLAM Trader" combinando información de tres períodos de tiempo:

- Un **EMA(3)** rápido en el gráfico de 15 minutos captura el sesgo de tendencia a corto plazo.
- Un **filtro de Laguerre** con gamma 0,7 aplicado a velas de 5 minutos mide si el precio cotiza por encima o por debajo de su trayectoria suavizada.
- El **Awesome Oscillator** en las velas horarias proporciona una verificación de impulso alineada con la definición de Bill Williams'.

Sólo cuando los tres componentes están de acuerdo la estrategia abre una operación, con el objetivo de filtrar el ruido que aparecería cuando cualquier período de tiempo se evalúa de forma aislada.

## Lógica de trading
1. **Preparación de datos**
   - Las velas de 15 minutos alimentan un `ExponentialMovingAverage` con una longitud `EmaPeriod` (predeterminado 3).
   - Las velas de 5 minutos alimentan un `LaguerreFilter` con suavizado `LaguerreGamma`.
   - Las velas de 60 minutos alimentan un `AwesomeOscillator`.
   - Para cada período de tiempo, se almacena el último cierre de vela finalizado para reproducir la comparación original entre indicador y precio.
2. **Condiciones de entrada**
   - **Largo**: el EMA está por encima del cierre actual de 15 minutos, Laguerre está por encima del último cierre de 5 minutos y Awesome Oscillator está por encima del último cierre por hora.
   - **Corto**: cada uno de los tres indicadores debe situarse por debajo de su cierre correspondiente.
3. **Gestión de riesgos**
   - Separe las distancias de stop-loss y take-profit (expresadas en puntos de instrumento) para operaciones largas y cortas.
   - Los trailingstops se activan una vez que el precio recorre al menos la distancia de seguimiento especificada más allá del precio de entrada. El stop se mueve en la dirección de la tendencia sin retroceder.
   - Todas las acciones de protección (take-profit, stop-loss, trailing stop) cierran la posición completa con órdenes de mercado, reflejando la implementación de MQL.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño del pedido para nuevas posiciones. | 0.1 |
| `PrimaryCandleType` | Periodo utilizado para la señal EMA y principal. | velas de 15 minutos |
| `LaguerreCandleType` | Plazo analizado por el filtro de Laguerre. | velas de 5 minutos |
| `AwesomeCandleType` | Marco de tiempo analizado por Awesome Oscillator. | velas de 60 minutos |
| `EmaPeriod` | EMA duración en el período de tiempo principal. | 3 |
| `LaguerreGamma` | Parámetro gamma para el filtro de Laguerre. | 0,7 |
| `LongStopLossPoints` | Distancia de stop-loss para operaciones largas, en puntos. | 20 |
| `ShortStopLossPoints` | Distancia de stop-loss para operaciones cortas, en puntos. | 20 |
| `LongTakeProfitPoints` | Distancia de obtención de beneficios para operaciones largas, en puntos. | 50 |
| `ShortTakeProfitPoints` | Distancia de obtención de beneficios para operaciones cortas, en puntos. | 50 |
| `LongTrailingPoints` | Distancia de seguimiento para operaciones largas, en puntos. | 15 |
| `ShortTrailingPoints` | Distancia de seguimiento para operaciones cortas, en puntos. | 15 |

## Notas
- La estrategia se suscribe a tres flujos de velas independientes y mantiene solo los valores finales más recientes, evitando los buffers de historial manuales.
- Todos los comentarios y mensajes de registro permanecen en inglés para mayor claridad, de acuerdo con las convenciones del proyecto.
- Ajuste los parámetros de riesgo basados en puntos de acuerdo con el `PriceStep` del instrumento para que los niveles de protección reflejen el tamaño del tick del corredor.
