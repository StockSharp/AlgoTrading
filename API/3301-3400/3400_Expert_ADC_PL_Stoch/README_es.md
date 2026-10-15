# Estrategia Expert ADC PL Stoch Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Expert ADC PL Stoch** es una estrategia de patrón de velas convertida del MQL5 asesor experto original *Expert_ADC_PL_Stoch*. Busca formaciones de líneas de perforación alcistas y de nubes oscuras bajistas en velas terminadas y confirma las señales con la línea %D del oscilador Stochastic. El método sigue la tendencia cuando el mercado retrocede hacia un movimiento establecido y requiere que el oscilador esté en zonas extremas antes de abrir posiciones. Las salidas de posiciones se basan en cruces Stochastic de áreas extremas, lo que refleja la lógica de salida basada en votos del sistema fuente.

## Lógica de trading

1. Suscríbase a un tipo de vela configurable (predeterminado: período de 1 hora).
2. Para cada vela terminada, mantenga las últimas velas necesarias para la evaluación del patrón de velas y los valores recientes de Stochastic %D.
3. **Entrada larga**
   - El par de velas anterior debe formar un patrón de Línea Perforante:
     - La vela en la barra *t-1* es alcista con un cuerpo mayor que el tamaño promedio del cuerpo.
     - La vela en la barra *t-2* es bajista con un cuerpo mayor que el promedio.
     - The bullish candle gaps below the bearish low and closes back inside the bearish body while the overall trend is downward according to the close average.
   - The Stochastic %D value on bar *t-1* must be below the long entry threshold (default 30).
4. **Short Entry**
   - El par de velas anterior debe formar un patrón de Cobertura de Nubes Oscuras:
     - La vela en la barra *t-2* es alcista y tiene un cuerpo grande.
     - La vela en la barra *t-1* se abre por encima del máximo anterior y se cierra dentro del cuerpo alcista.
     - The mid-price of the bearish candle is above the moving average of closes, signalling an uptrend prior to the reversal.
   - El Stochastic %D en la barra *t-1* debe estar por encima del umbral de entrada corta (predeterminado 70).
5. **Condiciones de salida**
   - Las posiciones largas se cierran cuando el Stochastic %D en la barra *t-1* cruza por debajo de los umbrales superior (80) o inferior (20) en comparación con la barra *t-2*.
   - Las posiciones cortas se cierran cuando el Stochastic %D en la barra *t-1* cruza por encima de los umbrales inferior (20) o superior (80) en comparación con la barra *t-2*.
6. Todos los cálculos se realizan sobre velas terminadas; no intrabar processing is used.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CandleType` | Marco temporal de velas utilizadas para la detección de patrones. | 1 hora |
| `StochasticLength` | Base length for the Stochastic oscillator. | 47 |
| `StochasticKPeriod` | Longitud de suavizado para la línea %K. | 9 |
| `StochasticDPeriod` | Longitud de suavizado para la línea %D. | 13 |
| `StochasticSlow` | Factor de desaceleración adicional aplicado al oscilador. | 3 |
| `AverageBodyPeriod` | Número de velas utilizadas para medir el tamaño del cuerpo de referencia y el promedio de cierre. | 5 |
| `LongEntryThreshold` | Valor máximo de %D permitido antes de realizar operaciones largas. | 30 |
| `ShortEntryThreshold` | Valor mínimo de %D requerido antes de realizar operaciones cortas. | 70 |
| `ExitLowerThreshold` | Lower boundary used for exit crossovers. | 20 |
| `ExitUpperThreshold` | Upper boundary used for exit crossovers. | 80 |

## Gestión del riesgo

- La estrategia envía órdenes de mercado utilizando el volumen de la estrategia base (por defecto, 1 contrato/lote).
- No automatic protective orders are configured; Se puede agregar gestión de riesgos externa o `StartProtection` si es necesario.
- Only one position is managed at a time; las señales opuestas cierran la posición activa antes de abrir una nueva.

## Notas

- Average candle bodies and close averages are computed from historical candles to replicate the MQL5 vote logic closely.
- Los valores Stochastic se almacenan por barra terminada para evaluar las mismas compensaciones utilizadas en el asesor experto original.
- Las operaciones se abren y cierran solo cuando la estrategia está completamente formada y los controles de clase base permiten la negociación.
