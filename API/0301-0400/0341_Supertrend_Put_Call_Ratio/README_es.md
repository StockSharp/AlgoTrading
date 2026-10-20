# Supertrend Put Call Ratio

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Reglas de negociación

Usar SuperTrend nativo de StockSharp con Period y Multiplier. Desde cero, comprar si Close > SuperTrend y PCR < media previa − PCRMultiplier × σ; vender corto si Close < SuperTrend y PCR > media previa + PCRMultiplier × σ. PCR es volumen put / volumen call observado. Cerrar largo con Close < SuperTrend y corto con Close > SuperTrend, independientemente del PCR. La igualdad no produce señal.

La media y la desviación estándar muestral usan las N observaciones consecutivas anteriores, excluyendo la actual: σ = sqrt(Σ(x − mean)² / (N − 1)). Se exige la ventana completa. No se generan valores aleatorios ni sustitutos derivados del precio.

## Datos de entrada

El CSV obligatorio lleva SecurityId,Time,Value. Time es la apertura UTC de la vela de señal (yyyy-MM-ddTHH:mm:ssZ). Los valores observados deben ser finitos, no negativos y únicos por instante. Solo se usa la observación actual. Un hueco vacía la ventana e impide entradas hasta completarla otra vez; las salidas siguen activas.

## Parámetros

| Parámetro | Valor predeterminado |
|---|---|
| `Period` | `10` |
| `Multiplier` | `3` |
| `PCRPeriod` | `20` |
| `PCRMultiplier` | `2` |
| `PCRHistoryFile` | obligatorio |
| `AtrPeriod` | `14` |
| `StopLossAtr` | `2` |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` |

## Ejecución

AtrPeriod = 14 usa ATR de Wilder. Al entrar desde cero se fija ATR(señal) × StopLossAtr. La protección local nativa con órdenes de mercado sigue las cotizaciones, toma como referencia la ejecución real y puede cerrar entre velas. Los cambios posteriores de ATR no mueven el stop. Cero lo desactiva para una ejecución nueva. La señal contraria cierra toda la posición real sin invertir ni aumentar en esa misma decisión.

## Verificación

Las ejecuciones usan únicamente el archivo BTC/TON disponible. Los informes, observaciones de opciones e historial de preparación explícitos son datos de prueba de la mecánica, no fundamentales ni historia de opciones reales. Las ventanas reducidas, dos grupos y marzo como mes anual son perfiles de prueba identificados, no validación de los valores largos originales. No se afirma rentabilidad.

