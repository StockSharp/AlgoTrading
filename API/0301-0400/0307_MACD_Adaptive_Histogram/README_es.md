# Estrategia de Histograma Adaptativo MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **MACD Adaptive Histogram** está construida alrededor del MACD con umbral de histograma adaptativo.

Las pruebas indican un retorno anual promedio de aproximadamente el 184%. Funciona mejor en el mercado cripto.

### Condiciones reales del histograma MACD (C# y Python)

Usa **velas terminadas de 15 minutos** y el histograma MACD(12,26,9) `H = MACD - Signal`. Para los últimos 20 valores de H se calculan SMA `A` y desviación estándar `D`. Con los indicadores formados y `D > 0`, desde plano **compra** cuando `H >= A + 2*D` y `H > 0`; **vende** cuando `H <= A - 2*D` y `H < 0`. Cierra largo con `H <= A`, corto con `H >= A`. Tras una orden de señal hay **16 velas terminadas** de espera. Las posiciones ejecutadas cuentan con **stop nativo del 2%**, no ATR. La rentabilidad citada arriba no se ha validado independientemente.



## Detalles
- **Criterios de entrada**: ver implementación para las condiciones del indicador.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `FastPeriod = 12`
  - `SlowPeriod = 26`
  - `SignalPeriod = 9`
  - `HistogramAvgPeriod = 20`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Histogram
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
