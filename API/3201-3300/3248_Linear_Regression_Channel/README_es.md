# Estrategia de Linear Regression Channel (Fibo)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es la conversión a StockSharp del asesor experto de MetaTrader **"linear regression channel"**. Opera en la dirección de la tendencia lineal de mayor temporalidad confirmada por un conjunto de medias móviles ponderadas, lecturas de Momentum y un filtro MACD mensual. Las reglas de gestión monetaria replican el comportamiento original con objetivos de beneficio flotantes, seguimiento de ganancias acumuladas, protección de punto de equilibrio y un stop de capital.

## Lógica de trading
1. **Temporalidad principal** – tipo de vela configurable (por defecto 15 minutos). Todos los cálculos de señales se ejecutan en esta temporalidad.
2. **Filtro de tendencia** – una LWMA rápida y una lenta calculadas sobre el precio típico. Las señales largas requieren que la LWMA rápida esté por encima de la lenta; las señales cortas requieren lo contrario.
3. **Confirmación de Momentum** – el indicador de Momentum se evalúa en una temporalidad superior que refleja el mapeo original de MetaTrader (M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1). Los últimos tres valores de Momentum se convierten a la distancia absoluta desde el nivel 100. Una configuración larga necesita que cualquiera de las tres distancias supere el umbral alcista, mientras que una configuración corta necesita que cualquiera de las tres supere el umbral bajista.
4. **Sesgo MACD mensual** – las velas mensuales impulsan un filtro MACD(12,26,9). Las operaciones largas solo se permiten cuando la línea principal del MACD está por encima de su línea de señal; las operaciones cortas requieren la relación contraria.
5. **Condición de entrada** – cuando todos los filtros se alinean y el trading está permitido, la estrategia abre una orden de mercado en la dirección correspondiente. La posición actual se cierra y se invierte cuando se produce una señal opuesta.

## Gestión de riesgos y operaciones
- **Stop-loss / take-profit fijo** – las distancias se definen en puntos del instrumento y se aplican a cada entrada. Si el máximo/mínimo de la vela perfora estos niveles, se cierra la posición.
- **Stop de seguimiento** – opcional; se activa una vez que la posición gana una cantidad configurable de puntos y sigue el mejor precio con el offset especificado.
- **Punto de equilibrio** – opcional; después de que el precio avanza la distancia del disparador, el nivel de stop se mueve al precio de entrada más/menos un offset para asegurar ganancias.
- **Take-profit de beneficio flotante** – objetivo monetario opcional. Cuando el beneficio flotante neto (expresado en moneda de cuenta) supera el umbral, se cierran todas las posiciones.
- **Take-profit basado en porcentaje** – objetivo opcional basado en el capital inicial en el momento en que inicia la estrategia.
- **Seguimiento monetario** – una vez que el beneficio flotante alcanza el disparador, la estrategia registra el beneficio máximo. Si el beneficio retrocede la cantidad de stop especificada, se cierra la posición.
- **Stop de capital** – protección contra drawdown opcional. Mientras la posición está perdiendo, si la pérdida flotante supera un porcentaje del pico de capital observado, la estrategia liquida la posición.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `Candle Type` | Temporalidad principal para la generación de señales. |
| `Fast LWMA` / `Slow LWMA` | Períodos para las medias móviles ponderadas linealmente rápida y lenta. |
| `Momentum Length` | Longitud de retrospectiva del Momentum en la temporalidad superior. |
| `Momentum Buy Threshold` / `Momentum Sell Threshold` | Distancia absoluta mínima desde 100 requerida para confirmación de Momentum alcista/bajista. |
| `Take Profit (points)` / `Stop Loss (points)` | Distancias de protección expresadas en puntos del instrumento. |
| `Use Trailing`, `Trailing Activation`, `Trailing Offset` | Configuración del stop de seguimiento. |
| `Use Break-even`, `Break-even Trigger`, `Break-even Offset` | Parámetros de lógica de punto de equilibrio. |
| `Max Trades` | Número máximo de entradas secuenciales permitidas durante la ejecución. |
| `Order Volume` | Volumen base para órdenes de mercado. |
| `Use Money TP`, `Money Take Profit` | Take-profit monetario flotante. |
| `Use Percent TP`, `Percent Take Profit` | Take-profit calculado como porcentaje del capital inicial. |
| `Enable Money Trailing`, `Money Trailing Trigger`, `Money Trailing Stop` | Seguimiento del beneficio flotante. |
| `Use Equity Stop`, `Equity Risk %` | Protección de stop-loss basada en capital. |

## Notas
- La estrategia mantiene solo una posición neta (larga o corta) y se invierte cuando llega una señal opuesta.
- Las suscripciones de Momentum y MACD agregan automáticamente las temporalidades superiores necesarias al feed de datos a través de `GetWorkingSecurities()`.
- Todos los comentarios dentro del código están en inglés según las directrices del repositorio.
