# Estrategia de Ruptura de Cruce Doble de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia reproduce el asesor experto MetaTrader "DoubleMA Crossover" dentro del framework de StockSharp. La lógica monitorea una media móvil rápida y una lenta, espera un cruce direccional, y luego requiere una confirmación de ruptura antes de entrar al mercado. El algoritmo gestiona solo una posición a la vez e incluye comportamiento opcional de trailing stop que imita los tres modos de trailing originales.

## Cómo funciona

1. **Detección de señal** – Se calculan dos medias móviles simples (predeterminados: 2 y 5) en la serie de velas seleccionada. Un cruce alcista ocurre cuando el promedio rápido cruza por encima del lento y viceversa para un cruce bajista.
2. **Confirmación de ruptura** – Después de un cruce, la estrategia almacena un nivel de ruptura definido en pasos de precio (`BreakoutPips`). Se abre una posición solo cuando el precio alcanza ese nivel en una vela posterior, replicando el comportamiento de la orden stop de la versión MQL.
3. **Gestión de posición** – Solo se permite una única posición. Mientras una operación está activa, la estrategia monitorea el stop-loss, el take-profit y el tipo de trailing stop configurado. Los rastreadores internos emulan la ejecución del lado del bróker para mantener el comportamiento determinista en backtests.
4. **Filtro de sesión** – El trading puede restringirse a una ventana de tiempo específica (`StartHour`..`StopHour`). La estrategia aún gestiona operaciones abiertas fuera de la ventana pero no crea nuevos niveles de ruptura cuando el filtro bloquea el trading.
5. **Trailing stops** – Se soportan tres modos de trailing: trailing inmediato con la distancia de stop inicial, trailing después de una distancia personalizada, y la lógica de tres niveles con cambios de breakeven igual que el EA original.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Períodos de las medias móviles simples rápida y lenta. |
| `BreakoutPips` | Distancia en pasos de precio añadida al cierre de la vela de señal para definir el disparador de ruptura. |
| `StopLossPips`, `TakeProfitPips` | Stop protector y take profit opcional en pasos de precio. Establecer take profit en cero para deshabilitarlo. |
| `UseTrailingStop` | Habilita la gestión del trailing stop. |
| `TrailingMode` | Tipo de trailing: Type1 usa la distancia de stop original, Type2 espera una distancia personalizada (`TrailingStopPips`), Type3 usa los tres niveles MQL. |
| `TrailingStopPips` | Distancia para el trailing de Type2. |
| `Level1TriggerPips`, `Level1OffsetPips` | Primer nivel de disparo y offset para el trailing de Type3 (mueve el stop a breakeven por defecto). |
| `Level2TriggerPips`, `Level2OffsetPips` | Segundo nivel de disparo y offset para el trailing de Type3. |
| `Level3TriggerPips`, `Level3OffsetPips` | Tercer nivel de disparo y offset para el trailing de Type3 (convierte a un trailing stop clásico). |
| `UseTimeLimit`, `StartHour`, `StopHour` | Habilita el filtro de sesión de trading y define el rango de horas inclusivo. |
| `CandleType` | Serie de velas usada para los cálculos de señal. |
| `TradeVolume` | Volumen de orden expresado en lotes. |

## Modos de Trailing Stop

- **Type1** – Mueve el stop usando la distancia de stop-loss original una vez que el precio avanza esa cantidad.
- **Type2** – Espera hasta que el precio se mueva `TrailingStopPips` antes de trailing, luego bloquea la ganancia a esa distancia.
- **Type3** – Usa tres niveles: los dos primeros desplazan el stop por los offsets definidos, y el tercero convierte a un trailing stop continuo usando el cierre actual y `Level3OffsetPips`.

## Consejos de uso

- Alinear `BreakoutPips` con el tamaño de tick del instrumento para mantener el mismo comportamiento que el asesor experto MetaTrader.
- Revisar el filtro de sesión para que coincida con los horarios de trading; el predeterminado permite entradas entre las 11:00 y las 16:00 hora local.
- Deshabilitar el filtro de tiempo (`UseTimeLimit = false`) para instrumentos de 24/7.
- Al probar el trailing de tipo 3, asegurarse de que los valores de offset no sean mayores que sus niveles de disparo correspondientes; de lo contrario, el stop puede permanecer detrás del precio de entrada.
