# Precision Trading: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Cruces de EMA confirmados por dirección Hull, impulso RSI y volatilidad ATR/rango de precios. La descripción histórica se refiere al scalping de oro; el ejemplo utiliza el instrumento configurado y no demuestra resultados en oro.

## Señales y ejecución

Solo se usan velas terminadas, de cinco minutos por defecto. Los siete indicadores deben estar formados. La primera vela totalmente preparada guarda los valores anteriores de EMA/HMA sin entrar.

- Largo: EMA rápida anterior ≤ EMA lenta anterior, EMA rápida actual > EMA lenta actual, HMA actual > HMA anterior y RSI > `RsiLongThreshold` (55).
- Corto: EMA rápida anterior ≥ EMA lenta anterior, EMA rápida actual < EMA lenta actual, HMA actual < HMA anterior y RSI < `RsiShortThreshold` (45).
- Ambos requieren close y ATR positivos, `100 * ATR / close >= MinAtrPercent` (0.05) y `Highest(high) - Lowest(low) >= ATR * RangeAtrMultiplier` (3). La ventana de extremos incluye la vela actual.

Un cruce válido abre `Volume` desde posición cero o envía `Volume + abs(Position)` para invertir completamente la posición opuesta. La señal del mismo lado mantiene la posición. Una orden pendiente impide duplicados. Los valores anteriores EMA/HMA avanzan en cada vela preparada: un cruce rechazado no se aplaza. No hay espera de seis horas ni stop-loss/take-profit adicionales; un cruce opuesto válido invierte la posición.

## Indicadores y valores predeterminados

| Parámetro | Valor | Significado |
|---|---:|---|
| `FastLength` / `SlowLength` | 14 / 40 | EMA nativas del cierre; período rápido < lento |
| `HmaLength` | 55 | `WMA(floor(sqrt(L)), 2 * WMA(floor(L/2), close) - WMA(L, close))`; L ≥ 2 |
| `RsiLength` | 14 | RSI nativo con ganancias/pérdidas del cierre suavizadas por Wilder |
| `RsiLongThreshold` / `RsiShortThreshold` | 55 / 45 | Umbrales estrictos; `0 <= short < long <= 100` |
| `AtrLength` | 14 | ATR Wilder nativo, con saltos respecto al cierre anterior |
| `RangeLength` | 20 | Máximo high y mínimo low, incluida la vela actual |
| `MinAtrPercent` | 0.05 | Mínimo inclusivo de ATR como porcentaje del cierre; no negativo |
| `RangeAtrMultiplier` | 3 | Mínimo inclusivo del rango en unidades ATR; positivo |
| `CandleType` | 5 minutos | Velas terminadas para todos los indicadores |

WMA pondera de 1 para la muestra más antigua a la longitud para la más reciente. EMA comienza con la media aritmética nativa y después usa `2 / (length + 1)`. ATR empieza con la media de una ventana creciente y continúa con Wilder. Los períodos son positivos; Hull necesita también formar la WMA final de raíz cuadrada.

Las pruebas C# y Python calculan independientemente EMA, HMA, RSI, ATR y extremos móviles sobre el archivo BTC/TON existente. Verifican cada orden, inversiones completas, confirmaciones rechazadas y señales separadas por menos de seis horas. Comprueban comportamiento, no rentabilidad ni idoneidad para operaciones reales.
