# Estrategia Aeron JJN Scalper EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port de alto nivel en StockSharp del asesor experto **Aeron JJN Scalper**. Observa velas terminadas, identifica situaciones específicas de reversión de dos barras y coloca órdenes stop simuladas en la apertura de la vela opuesta más reciente. Cuando el mercado alcanza el nivel de stop almacenado, la estrategia entra con una orden a mercado, aplica objetivos de riesgo basados en ATR y gestiona la operación con un trailing stop basado en pips.

Ideas clave:

* La dirección de la operación se decide por un patrón de reversión alcista/bajista de dos velas.
* Los niveles de entrada provienen del precio de apertura de la última vela fuerte en la dirección opuesta.
* Un valor ATR(8) medido en la barra de señal establece las distancias de stop-loss y take-profit.
* La lógica del trailing stop mueve el nivel de protección una vez que el precio avanza por los offsets de pip configurados.
* Los niveles pendientes expiran automáticamente después del número configurado de minutos.

## Reglas de trading
### Detección de señal
1. Trabajar solo con velas terminadas del marco temporal configurado (predeterminado: 1 minuto).
2. Calcular el tamaño del pip a partir del paso de precio del instrumento y multiplicar por 10 para precios de 3 o 5 decimales para imitar el comportamiento de pip de MetaTrader.
3. Mantener una ventana deslizante de las últimas 120 velas para buscar barras de referencia.
4. Detectar una **configuración larga** cuando:
   * La vela actual cierra por encima de su apertura (alcista), y
   * La vela anterior es bajista con tamaño de cuerpo mayor que `DojiDiff1Pips`.
   * Buscar hacia atrás la última vela bajista cuyo cuerpo supere `DojiDiff2Pips`; su precio de apertura se convierte en el nivel de buy stop.
5. Detectar una **configuración corta** cuando:
   * La vela actual cierra por debajo de su apertura (bajista), y
   * La vela anterior es alcista con tamaño de cuerpo mayor que `DojiDiff1Pips`.
   * Buscar hacia atrás la última vela alcista cuyo cuerpo supere `DojiDiff2Pips`; su precio de apertura se convierte en el nivel de sell stop.
6. Ignorar nuevas configuraciones si ya hay un nivel pendiente en la misma dirección, o si el valor ATR para la vela aún no está disponible.

### Gestión de niveles pendientes
* El nivel almacenado se trata como una orden stop pendiente. Se descarta si el precio permanece por debajo (largo) o por encima (corto) del disparador hasta que expira el tiempo `ResetMinutes`.
* Cuando el precio toca el nivel en una vela posterior (máximo ≥ nivel de compra o mínimo ≤ nivel de venta), la estrategia envía una orden a mercado dimensionada para cerrar cualquier exposición existente y añadir contratos `Volume`.
* Entrar en una posición larga limpia cualquier nivel corto pendiente y viceversa.

### Stop-loss, take-profit y trailing
* Al entrar, la estrategia registra el valor ATR(8) de la vela de señal.
  * Operaciones largas: stop-loss = `entry - ATR`, take-profit = `entry + ATR`.
  * Operaciones cortas: stop-loss = `entry + ATR`, take-profit = `entry - ATR`.
* En cada vela terminada la estrategia:
  * Verifica si el precio alcanzó el stop-loss o take-profit y sale con una orden a mercado si se toca.
  * Aplica trailing cuando el precio se ha movido al menos `TrailingStopPips + TrailingStepPips` a favor de la posición. El nuevo stop queda `TrailingStopPips` detrás del último cierre. El stop nunca retrocede.
* Si la posición se cierra manualmente, el estado interno se restablece automáticamente.

## Parámetros
| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `Volume` | 0.1 | Tamaño de posición neta utilizado para entradas; la estrategia añade la posición actual absoluta para cambiar de dirección cuando sea necesario. |
| `TrailingStopPips` | 5 | Distancia base del trailing stop (convertida a unidades de precio). |
| `TrailingStepPips` | 5 | Avance adicional requerido antes de mover el trailing stop nuevamente. |
| `ResetMinutes` | 10 | Tiempo de expiración para un nivel pendiente almacenado (minutos). |
| `DojiDiff1Pips` | 10 | Tamaño mínimo de cuerpo (en pips) para la vela de reversión que precede a la señal. |
| `DojiDiff2Pips` | 4 | Tamaño mínimo de cuerpo (en pips) para la vela usada como nivel de referencia de entrada. |
| `CandleType` | Marco temporal de 1 minuto | Tipo de datos de vela usado para cálculos. |

## Notas de implementación
* La estrategia opera puramente en velas terminadas y usa niveles en memoria en lugar de órdenes stop reales; cuando se viola el nivel, se envía inmediatamente una orden a mercado. Esto refleja el comportamiento original del EA dentro de la API de alto nivel de StockSharp.
* ATR(8) se calcula con `AverageTrueRange` y se almacena en caché para que las distancias de stop/objetivo originales permanezcan constantes para cada operación.
* La conversión de pips reproduce el ajuste de MetaTrader para cotizaciones de 3 y 5 dígitos. Si el instrumento carece de `PriceStep`, se usa un paso predeterminado de `1`.
* Se almacenan hasta 120 velas históricas para replicar el look-back original de `CopyRates` de 100 barras con algún margen de seguridad.
* No se proporciona port en Python para esta estrategia.

## Uso
1. Adjuntar la estrategia al instrumento y portafolio deseados.
2. Ajustar el marco temporal de velas, los offsets de pip y los filtros basados en ATR para adaptarlos al instrumento.
3. Iniciar la estrategia; rastreará señales, enviará órdenes a mercado cuando se toquen los niveles de activación y gestionará las salidas automáticamente.
