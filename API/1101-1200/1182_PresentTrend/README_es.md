# PresentTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Línea ATR causal controlada por RSI del cierre o MFI de precio típico y volumen. Los cruces confirmados con la línea de dos velas atrás abren, invierten completamente o cierran posiciones.

## Línea y señales

Solo velas terminadas, cinco minutos por defecto. ATR Wilder nativo y el oscilador seleccionado comparten `Length=14`, positivo. No se registra el oscilador no seleccionado. Se espera a que ambos estén formados:

- Oscilador ≥50: candidato `low - Multiplier * ATR`; línea = máximo del candidato y la línea anterior.
- Oscilador <50: candidato `high + Multiplier * ATR`; línea = mínimo del candidato y la línea anterior.
- Primera línea formada = candidato seleccionado, sin inventar historia. `Multiplier=1`, positivo.

ATR incluye saltos respecto al cierre anterior, empieza con media creciente y después Wilder. RSI suaviza ganancias/pérdidas del cierre por Wilder y conserva el resultado anterior si no cambia el cierre. MFI usa `typical=(high+low+close)/3` por volumen: típico creciente aporta flujo positivo, decreciente negativo e igual ninguno. Sobre las últimas `Length` velas, MFI = `100 * positiveSum / (positiveSum + negativeSum)`, o 100 sin flujo negativo.

El cruce largo bruto en t exige `line[t-1] <= line[t-3]` y `line[t] > line[t-2]`; el corto usa ≥ y < respectivamente. Requiere cuatro valores formados.

Confirma largo solo si existe un corto bruto anterior más reciente que el largo bruto anterior, o aún no existe largo anterior. Corto es simétrico. Después de comparar se registra cada señal bruta, incluidas las rechazadas para operar. La primera señal no opera sin una contraria observada. Las repeticiones del mismo lado necesitan una señal contraria intermedia.

## Ejecución y valores predeterminados

Con ambos lados habilitados, envía `Volume` desde cero o `Volume + abs(Position)` para invertir completamente una posición opuesta. Mantiene si ya ocupa el mismo lado. En solo largo, un corto confirmado cierra todo el largo sin abrir corto; solo corto es simétrico. Una dirección deshabilitada desde cero no actúa. Las órdenes pendientes bloquean duplicados, mientras línea e historia avanzan. No se aplazan señales rechazadas ni existe espera de seis horas.

Sin stop-loss/take-profit de precio independientes. La salida unilateral es una señal contraria confirmada, no una orden stop ATR de precio.

| Parámetro | Valor |
|---|---:|
| `Length` | 14, período compartido positivo |
| `Multiplier` | 1, distancia ATR positiva |
| `IsUseMfi` | false selecciona RSI; true MFI |
| `IsLongEnabled` / `IsShortEnabled` | true / true; al menos uno habilitado |
| `CandleType` | 5 minutos |

Las pruebas C# y Python calculan independientemente ATR, RSI/MFI, línea y cruces brutos/confirmados en el archivo BTC/TON. Verifican cada orden y posición real: momento, lado, inversión completa y cierre unilateral. Cubren ambos osciladores y señales separadas por menos de seis horas. No demuestran rentabilidad.
