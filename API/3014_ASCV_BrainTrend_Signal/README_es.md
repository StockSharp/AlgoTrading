# Estrategia de Señal ASCV BrainTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Señal ASCV BrainTrend** es una conversión del experto de MetaTrader que opera sobre señales del indicador BrainTrend1. La versión de StockSharp se basa en vinculaciones de indicadores de alto nivel para combinar el Average True Range (ATR), el Oscilador Estocástico y la Jurik Moving Average (JMA) con el fin de detectar reversiones de impulso y colocar operaciones con stops de protección opcionales.

## Idea Central

1. Calcular el ATR para medir la volatilidad actual y definir una banda de confirmación dinámica.
2. Suavizar los precios de cierre con una Jurik Moving Average y comparar el valor actual con el valor de dos barras atrás.
3. Cuando la diferencia suavizada es mayor que `ATR / 2.3`, actualizar el estado de la lógica BrainTrend:
   - `%K` del Oscilador Estocástico por debajo de **47** cambia el sistema a una posible configuración corta.
   - `%K` por encima de **53** cambia el sistema a una posible configuración larga.
4. Una señal de la barra anterior se ejecuta en la siguiente vela completada. Las señales pueden invertirse con el parámetro **Reverse Signals**.
5. Los niveles de stop-loss, take-profit y trailing-stop se definen en pips (múltiplos del paso de precio del instrumento).

## Reglas de Entrada y Salida

- **Entrada larga**: La barra anterior emitió una señal de compra y la estrategia no está ya en largo. El tamaño de la orden equivale a `Volume + abs(posición actual)`, de modo que los cortos se cierran antes de abrir el nuevo largo.
- **Entrada corta**: La barra anterior emitió una señal de venta y la estrategia no está ya en corto.
- **Stop-loss**: Colocado en `precio de entrada ± StopLossPips * paso de precio`. Si el precio supera el nivel de stop dentro de la siguiente vela, la posición se cierra a mercado.
- **Take-profit**: Take profit opcional en `precio de entrada ± TakeProfitPips * paso de precio`.
- **Trailing-stop**: Habilitado cuando tanto `TrailingStopPips` como `TrailingStepPips` son mayores que cero. Después de que el precio se mueve `TrailingStopPips + TrailingStepPips` a favor de la operación, el stop sigue el movimiento por `TrailingStopPips`.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `AtrPeriod` | Período de promedio del ATR para estimación de volatilidad. | 14 |
| `StochasticPeriod` | Período base para el Oscilador Estocástico. | 12 |
| `JmaLength` | Longitud de suavizado de la Jurik Moving Average. | 7 |
| `StopLossPips` | Distancia de stop-loss en pips (pasos de precio). | 15 |
| `TakeProfitPips` | Distancia de take-profit en pips. | 46 |
| `TrailingStopPips` | Distancia de trailing stop en pips. | 0 (deshabilitado) |
| `TrailingStepPips` | Movimiento favorable mínimo requerido antes del trailing. | 5 |
| `ReverseSignals` | Invertir señales de compra/venta. | false |
| `CandleType` | Marco temporal de trabajo, por defecto velas de 15 minutos. | 15m |

## Notas

- Todos los cálculos de indicadores se realizan en velas terminadas para evitar ruido a mitad de barra.
- Si el instrumento no proporciona `MinPriceStep`, se usa un paso predeterminado de `0.0001` al convertir distancias de pips.
- La estrategia dibuja velas, el oscilador estocástico y el JMA en el gráfico para monitoreo.
- Los trailing stops replican la lógica original de MetaTrader: solo se mueven en la dirección de la operación y requieren que se cumplan los umbrales de distancia y paso.

## Consejos de Uso

- Ajustar `AtrPeriod` y `StochasticPeriod` para adaptarse a la volatilidad del instrumento operado.
- Aumentar los parámetros de riesgo basados en pips cuando se operen activos con tamaños de tick más grandes (p. ej., futuros) para evitar salidas inmediatas.
- Habilitar `ReverseSignals` para reproducir el modo inverso del Asesor Experto original.
- Combinar con controles de riesgo del bróker si se involucra trading con dinero real.
