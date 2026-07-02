# Estrategia de velas MP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia MP Candlestick** es una conversión del MetaTrader 5 Expert Advisor `mp candlestick.mq5` al marco de estrategia de alto nivel StockSharp. El sistema evalúa la dirección de las velas completadas y abre operaciones en la misma dirección mientras aplica una estricta gestión de riesgos. Admite distancias de stop-loss fijas expresadas en MetaTrader pips y colocación de stop-loss adaptativa derivada del rango verdadero promedio (ATR).

## Lógica de trading
1. La estrategia se suscribe a una única serie de velas configurables (predeterminado: velas de 1 hora).
2. En cada vela terminada:
   - Vela alcista (cierre por encima de apertura) → considere una posición larga.
   - Vela bajista (cierre por debajo de apertura) → considere una posición corta.
   - Se ignoran las velas Doji.
3. Antes de cualquier entrada, la estrategia calcula un precio de límite de pérdidas desde ATR o desde la distancia de pip fija. El precio de obtención de beneficios se calcula utilizando la relación riesgo-recompensa configurada.
4. Si el uso del margen se mantiene dentro del porcentaje permitido y el tamaño de la posición calculado es válido, la operación se abre en el mercado.
5. Mientras la posición está activa, la estrategia monitorea cada nueva vela para detectar:
   - El stop-loss o la toma de ganancias utilizan los extremos de las velas.
   - Ajuste final que mueve el stop hacia el punto de equilibrio cuando ATR stop están habilitados.
6. Una vez que la posición es plana, el proceso se reinicia con la siguiente vela terminada.

## Gestión de riesgos y dinero
- **Porcentaje de riesgo** define la fracción de capital arriesgada por operación. El tamaño de la posición se deriva de la distancia del precio entre la entrada y el stop-loss y el precio/valor del paso del instrumento.
- **Relación riesgo/recompensa** determina la distancia entre el precio de entrada y el objetivo de obtención de beneficios en relación con el riesgo inicial.
- **Uso de margen máximo** restringe la cantidad de margen estimado que puede consumir la nueva operación en comparación con el capital de la cartera actual.
- **Trailing Stop** se activa automáticamente cuando se utiliza la gestión de riesgos basada en ATR. Mueve el stop a mitad de camino hacia el objetivo de ganancias sin exceder el último cierre de vela, intentando bloquear las ganancias respetando las restricciones cambiarias.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `RiskPercent` | 1 | Porcentaje del capital de la cartera asignado como pérdida máxima para una sola operación. |
| `RiskRewardRatio` | 1.5 | Multiplicador aplicado a la distancia de riesgo inicial para definir el objetivo de obtención de beneficios. |
| `MaxMarginUsage` | 30 | Límite superior para el consumo de margen expresado como porcentaje del capital. |
| `StopLossPips` | 50 | Se corrigió el tamaño del stop-loss en MetaTrader pips cuando ATR está deshabilitado. |
| `UseAutoSl` | cierto | Habilita el tamaño de stop-loss de ATR (longitud 14) con multiplicador 1,5. |
| `CandleType` | plazo de 1 hora | Serie de velas utilizadas para señales y cálculo ATR. |

## Notas de implementación
- La estrategia se basa en StockSharp suscripciones de alto nivel (`SubscribeCandles`) y vinculación de indicadores (`AverageTrueRange`).
- El tamaño de la posición se alinea con el paso de volumen del instrumento y las restricciones de volumen mínimo y máximo.
- Las comprobaciones de márgenes reutilizan las sugerencias de margen de instrumentos disponibles (`MarginBuy`/`MarginSell`) y recurren a una estimación basada en el precio.
- Los niveles de stop-loss y take-profit se aplican internamente mediante el seguimiento de los máximos y mínimos de las velas, lo que garantiza un comportamiento coherente entre los corredores.
- Todos los comentarios del código están en inglés como lo exigen las pautas de conversión.

## Archivos
- `CS/MpCandlestickStrategy.cs`: implementación de la estrategia principal de C#.
- `README.md` — Documentación en inglés (este archivo).
- `README_zh.md` — Traducción al chino.
- `README_ru.md` — Traducción al ruso.
