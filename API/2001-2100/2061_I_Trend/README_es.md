# Estrategia I-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia I-Trend** es un algoritmo de seguimiento de tendencia convertido del experto MQL5 original `Exp_i_Trend`. Combina una media móvil con Bandas de Bollinger para identificar cambios de momentum. La estrategia calcula un valor personalizado *iTrend* y una línea de señal correspondiente, y abre o cierra posiciones cuando se producen cruces.

## Cómo Funciona

1. **Configuración de Indicadores**
   - Calcula una Media Móvil Exponencial (EMA) con período configurable.
   - Construye Bandas de Bollinger usando el mismo marco temporal y parámetros de desviación.
   - Deriva el valor *iTrend* como la diferencia entre el precio elegido y la línea de Banda de Bollinger seleccionada (superior, inferior o media).
   - Calcula una línea de señal como `2 * MA - (High + Low)`.
2. **Generación de Señales**
   - Cuando el iTrend cruza **por encima** de la línea de señal, la estrategia cierra posiciones cortas y abre una posición larga.
   - Cuando el iTrend cruza **por debajo** de la línea de señal, la estrategia cierra posiciones largas y abre una posición corta.
3. **Ejecución de Órdenes**
   - Las entradas y salidas se ejecutan a precio de mercado.
   - El tamaño de posición está definido por el parámetro de estrategia `Volume`.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `MaPeriod` | Período de la media móvil usada en los cálculos. |
| `BbPeriod` | Período de las Bandas de Bollinger. |
| `BbDeviation` | Desviación estándar para las Bandas de Bollinger. |
| `PriceType` | Tipo de precio usado para calcular el valor iTrend (Close, Open, High, Low, Median, Typical, etc.). |
| `BbMode` | Selecciona qué línea de Banda de Bollinger se usa (Upper, Lower, Middle). |
| `CandleType` | Marco temporal de las velas suministradas a la estrategia. |
| `Volume` | Volumen de órdenes para las entradas. |

## Notas

- La estrategia trabaja solo con velas completadas; las velas incompletas son ignoradas.
- Está diseñada con fines educativos y puede requerir ajustes para el trading en vivo.
