# Fib Hurst Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

CandleType intradía. Rango Fibonacci: máximo y mínimo de las LookbackPeriod velas diarias UTC completas previas, sin día de señal. Niveles L+0.618×(H−L) y L+0.382×(H−L). Hurst diario de HurstPeriod cierres previos: H = log(R/S)/log(N), R rango de desviaciones acumuladas de la media, S desviación estándar poblacional; varianza cero impide entrar. Comparar cierre previo y actual intradía contra el mismo nivel diario: cruce sobre 61.8% con H>0.5 compra, bajo 38.2% con H<0.5 vende corto. Volume solo desde cero. MaxTradesPerDay cuenta intentos de órdenes por fecha UTC histórica y MaxTotalTrades por ejecución. Protección nativa por cotizaciones fija SL = RiskPercent% y TP = RiskPercent×RiskReward% desde ejecución real. Sin reversión ni espera.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(15).TimeFrame() |
| `HurstPeriod` | 50 |
| `LookbackPeriod` | 20 |
| `MaxTradesPerDay` | 5 |
| `MaxTotalTrades` | 510 |
| `RiskPercent` | 2 |
| `RiskReward` | 2 |
| `DailyHistoryFile` | sin configurar |

## Datos de entrada

CSV opcional `DailyHistoryFile`: `Date,High,Low,Close`, fecha `yyyy-MM-dd`. Fechas únicas y 0 < Low ≤ Close ≤ High. Solo días anteriores al día intradía de señal. Velas diarias cerradas sustituyen su fecha; filas futuras no afectan decisiones previas.

## Verificación

Ambos lenguajes se verifican con BTC/TON incluido, fórmulas independientes, dirección/tamaño de órdenes, ejecuciones, posiciones y salidas. Datos fechados de precios/factores y calendarios verifican mecánica; OHLC/volúmenes trasladados no son historia plurianual. Dos grupos, TopN=1 y SMA=2 son perfiles explícitos pequeños. Ventanas largas y valores originales se comprueban aparte. Sin promesas de rentabilidad.
