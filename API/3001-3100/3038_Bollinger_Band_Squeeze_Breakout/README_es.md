# Estrategia Bollinger Band Squeeze Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el asesor experto original de MetaTrader 4 "BOLINGER BAND SQUEEZE" usando la API de alto nivel de StockSharp. Busca períodos donde las Bollinger Bands se contraen y luego entra en operaciones una vez que las bandas se expanden, siempre que los filtros de momentum y tendencia confirmen el movimiento. La conversión mantiene la lógica de confirmación multi-marcos temporales y transforma los bloques de gestión monetaria en expresiones idiomáticas de StockSharp.

## Lógica de trading
1. **Squeeze y expansión de bandas**
   - Las Bollinger Bands (longitud 20, desviación 2 por defecto) se calculan en el marco temporal de trabajo.
   - El ancho de la vela completada más reciente se compara contra el ancho `RetraceCandles` barras atrás.
   - Un breakout válido requiere que la razón de ancho supere `SqueezeRatio`, señalando que el precio se expande fuera del squeeze.
2. **Filtro de tendencia**
   - Dos medias móviles ponderadas (WMA 6 y WMA 85 en precio típico) definen la tendencia inmediata. Las operaciones largas requieren que la WMA rápida esté por encima de la WMA lenta, las cortas lo contrario.
3. **Confirmación de momentum**
   - Un indicador de Momentum de marco temporal superior (longitud 14) verifica si el precio se desvía suficientemente del nivel 100. La desviación máxima de los últimos tres valores del marco temporal superior debe superar el umbral específico de dirección.
   - El marco temporal superior se selecciona automáticamente para coincidir con el mapeo usado en el script MT4 (p.ej., M15 → H1, H1 → D1, D1 → mensual). Los datos semanales también recurren a la confirmación mensual. Si no hay marco temporal superior disponible, el filtro de momentum se omite.
4. **Filtro macro**
   - Un MACD mensual (12/26/9) asegura que el momentum a largo plazo coincide con la dirección de la operación (línea MACD sobre la señal para largos, bajo para cortos).
5. **Reglas de entrada**
   - Largos: expansión de bandas, WMA rápida sobre WMA lenta, MACD mensual alcista, desviación de momentum del marco temporal superior por encima de `MomentumBuyThreshold`, y solapamiento estructural de velas (`candle[-2].Low < candle[-1].High`).
   - Cortos: expansión de bandas, WMA rápida bajo WMA lenta, MACD mensual bajista, desviación de momentum por encima de `MomentumSellThreshold`, y la condición de vela espejo (`candle[-1].Low < candle[-2].High`).
6. **Reglas de salida**
   - Las posiciones se cierran cuando el precio cierra en o más allá de la Bollinger Band exterior en la dirección de la operación (es decir, los largos salen en la banda superior, los cortos en la banda inferior), reflejando la implementación MT4.
   - `StartProtection()` habilita la infraestructura de órdenes protectoras de StockSharp para que se puedan agregar extensiones de stop-loss/take-profit si se requiere.

## Indicadores y suscripciones de datos
- Velas del marco temporal primario definidas por `CandleType`.
- Velas del marco temporal superior para confirmación de momentum (mapeadas automáticamente desde el marco temporal base).
- Velas mensuales para filtrado MACD (aproximación de 30 días).
- Indicadores: Bollinger Bands, dos Medias Móviles Ponderadas (precio típico), Momentum y MovingAverageConvergenceDivergenceSignal.

## Parámetros
| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `CandleType` | Velas de 15 minutos | Marco temporal de trabajo primario. |
| `BollingerPeriod` | 20 | Longitud de la Bollinger Band. |
| `BollingerWidth` | 2.0 | Multiplicador de desviación estándar de la Bollinger Band. |
| `SqueezeRatio` | 1.1 | Razón mínima de expansión de ancho entre bandas actuales e históricas. |
| `RetraceCandles` | 10 | Retrospectiva usada para comparación de squeeze. |
| `FastMaLength` | 6 | Longitud de la WMA rápida (precio típico). |
| `SlowMaLength` | 85 | Longitud de la WMA lenta (precio típico). |
| `MomentumLength` | 14 | Período de Momentum en el marco temporal superior. |
| `MomentumBuyThreshold` | 0.3 | Desviación mínima de 100 requerida para validar entradas largas. |
| `MomentumSellThreshold` | 0.3 | Desviación mínima de 100 requerida para validar entradas cortas. |

Todos los parámetros se exponen como valores `StrategyParam<T>` y pueden optimizarse dentro de StockSharp Designer o en tiempo de ejecución.

## Notas de implementación
- La estrategia usa `SubscribeCandles().BindEx(...)` para mantener el cableado del indicador declarativo y evita colecciones de indicadores manuales, según lo requerido por las directrices de la API de alto nivel.
- Las medias móviles ponderadas son impulsadas por precio típico dentro del callback de procesamiento de velas para preservar el comportamiento de los cálculos LWMA en el script MT4.
- Los valores de momentum del marco temporal superior se almacenan en una cola de tres elementos para imitar los retrocesos 1–3 de `iMomentum` del código original.
- Los valores MACD mensuales persisten en campos de clase para que cada vela del marco temporal primario tenga acceso al sesgo a largo plazo más reciente.
- Las salidas activadas por las bandas exteriores reemplazan los bloques de trailing stop/break-even MT4 mientras retienen la intención visual de cerrar cuando el precio toca la envolvente opuesta.
- La estrategia deja el dimensionamiento de órdenes a la base `Strategy.Volume`. Los giros de posición automáticamente compensan cualquier exposición existente añadiendo `Math.Abs(Position)` al volumen de la orden.
