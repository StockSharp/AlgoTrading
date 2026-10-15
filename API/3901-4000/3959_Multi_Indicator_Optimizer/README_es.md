# Estrategia de optimización de indicadores múltiples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica la lógica de votación del experto MetaTrader **MultiIndicatorOptimizer** además del nivel alto StockSharp API. Cinco osciladores clásicos evalúan la vela terminada y aportan un voto ponderado al sentimiento agregado. Luego, la puntuación resultante se compara con los umbrales definidos por el usuario para decidir si la estrategia debe ir en largo, en corto o aplanar una posición existente.

## Lógica comercial

1. Bloque **MACD** – inspecciona el signo del histograma y la relación entre las líneas principal y de señal (ambas tomadas de la barra terminada anterior). La suma de estas dos señales se promedia y se multiplica por `MacdWeight`.
2. **Awesome Oscillator block**: mide si el oscilador está por encima o por debajo de la línea cero y si el impulso mejora en comparación con la barra anterior. El voto promedio se incrementa en `AoWeight`.
3. **Bloque OsMA**: verifica el signo del histograma MACD de la vela anterior y aplica `OsmaWeight`.
4. **Williams Bloque %R**: reacciona a los cruces de sobreventa/sobrecompra definidos por `WilliamsLowerLevel` y `WilliamsUpperLevel`. Un cruce hacia arriba desde la banda inferior vota alcista, mientras que un cruce hacia abajo desde la banda superior vota bajista. El resultado se multiplica por `WilliamsWeight`.
5. Bloque **Stochastic**: combina dos comprobaciones: un cruce de umbral de %K frente a `StochasticLowerLevel`/`StochasticUpperLevel` y una relación %K/%D. El promedio de ambas subseñales se multiplica por `StochasticWeight`.

La puntuación agregada se almacena en la columna `Signal` de los registros y se expone a través del campo `_lastSignal` dentro de la estrategia. El motor comercial evalúa la puntuación de la siguiente manera:

- `signal >= EntryThreshold`: cierra cualquier posición corta y abre/mantiene una posición larga.
- `signal <= -EntryThreshold`: cierra cualquier posición larga y abre/mantiene una posición corta.
- `abs(signal) <= ExitThreshold`: posición plana para evitar operar en condiciones de mercado neutrales.

Todos los cálculos funcionan en la vela terminada anterior para que coincida con la implementación MT4 original que usaba valores de indicador indexados (`shift = 1/2`).

## Parámetros

| Parámetro | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Plazo principal para todos los cálculos de indicadores. | velas H1 |
| `MacdFast` / `MacdSlow` / `MacdSignal` | EMA longitudes para el bloque MACD. | 12 / 26 / 9 |
| `MacdWeight` | Multiplicador de votos para el bloque MACD. Los valores negativos invierten el voto. | 1 |
| `AoShortPeriod` / `AoLongPeriod` | Longitudes de media móvil utilizadas por Awesome Oscillator. | 5 / 34 |
| `AoWeight` | Multiplicador de votos para el bloque Awesome. | 1 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | MACD configuración reutilizada para construir el histograma OsMA. | 12 / 26 / 9 |
| `OsmaWeight` | Multiplicador de votos para el bloque OsMA. | 1 |
| `WilliamsPeriod` | Longitud retrospectiva para Williams %R. | 14 |
| `WilliamsLowerLevel` / `WilliamsUpperLevel` | Límites de sobreventa/sobrecompra (en porcentaje). | -80 / -20 |
| `WilliamsWeight` | Multiplicador de votos para el bloque Williams. | 1 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Períodos del oscilador Stochastic y su suavizado interno. | 5 / 3 / 3 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | Umbrales de sobreventa/sobrecompra para %K. | 20 / 80 |
| `StochasticWeight` | Multiplicador de votos para el bloque Stochastic. | 1 |
| `EntryThreshold` | Voto absoluto mínimo requerido para abrir o revertir una posición. | 0,5 |
| `ExitThreshold` | Ancho de la zona neutra. Las posiciones se cierran cuando el valor absoluto de la señal cae por debajo de este valor. | 0.1 |

Todos los pesos pueden ser negativos para suprimir o invertir la contribución de un bloque, lo cual resulta conveniente durante las ejecuciones de optimización.

## Notas

- La estrategia se basa exclusivamente en el nivel alto API: `SubscribeCandles`, enlaces de indicadores y ayudantes `BuyMarket`/`SellMarket`.
- Cada votación de indicador utiliza solo velas completas, lo que garantiza que las decisiones se basen en datos confirmados.
- El tamaño de la posición está controlado por la propiedad base `Volume` de `Strategy`. Las órdenes de protección (stop loss/takeprofit) se pueden agregar externamente a través de `StartProtection` si es necesario.
- Se proporcionan comentarios extensos en inglés según lo solicitado para simplificar el mantenimiento adicional.
