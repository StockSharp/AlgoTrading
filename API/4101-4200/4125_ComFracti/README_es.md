# Estrategia ComFracti
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

ComFracti es una estrategia direccional traducida del asesor experto MT4 "ComFracti". La lógica combina la confirmación fractal de múltiples marcos temporales con RSI y filtros estocásticos, mientras que los filtros opcionales de promedio móvil, parabólico SAR, canal y perceptrón controlan la alineación de tendencias. La implementación de C# comercializa una sola posición a la vez y evalúa señales en velas completadas utilizando StockSharp API de alto nivel.

## Lógica de trading

- **Señal primaria**
  - Confirma una configuración alcista cuando tanto el marco temporal actual como el marco temporal superior producen una señal fractal alcista.
  - Confirma una configuración bajista cuando ambos marcos temporales producen una señal fractal bajista.
  - RSI (3 períodos predeterminados en el período de tiempo superior) debe ubicarse por debajo de `50 - RsiLevelBuy` para posiciones largas o por encima de `50 + RsiLevelSell` para posiciones cortas cuando el filtro RSI está habilitado.
  - El oscilador estocástico (%K período 5 predeterminado con %D suavizado 3/3) debe estar por debajo de `50 - StochasticLevelBuy` para posiciones largas o por encima de `50 + StochasticLevelSell` para posiciones cortas cuando el filtro estocástico está habilitado.
- **Filtros opcionales**
  - **EMA pendiente**: el EMA en el período de tiempo del filtro debe aumentar para las posiciones largas y disminuir para las posiciones cortas.
  - **Parabolic SAR**: el valor SAR debe permanecer por debajo de la barra abierta para largos o por encima para cortos.
  - **Desglose del canal**: compara la barra anterior con un canal adaptable de estilo Donchian; Los mínimos anteriores deben permanecer por encima del piso del canal para las posiciones largas, mientras que los máximos anteriores deben permanecer por debajo del techo para las posiciones cortas.
  - **Perceptrón**: una suma ponderada de las diferencias recientes entre máximos y mínimos debe ser positiva para las posiciones largas y negativa para las posiciones cortas.
- **Gestión de posiciones**
  - Sólo hay una posición activa a la vez; la estrategia cierra la exposición existente antes de abrir una nueva operación en la dirección opuesta.
  - Las distancias fijas de stop-loss y take-profit se expresan en puntos del instrumento.
  - Un trailing stop opcional se mueve en dirección a las ganancias una vez que se alcanza el buffer de seguimiento (cuando `ProfitTrailing` es verdadero).
  - Cuando `CloseOnOppositeSignal` está habilitado, la estrategia sale temprano si aparece la señal principal opuesta.

## Gestión del riesgo

- El tamaño de la posición base es igual al parámetro `BaseVolume` (lotes 0,1 predeterminados). Cuando `AccountMicro` está habilitado, el volumen se divide por diez.
- Si `UseMoneyManagement` está habilitado, la estrategia arriesga `RiskPercent` del valor de la cuenta por operación, utilizando la distancia de stop-loss configurada y el valor del paso del instrumento para aproximar el tamaño de la posición. El volumen calculado está bloqueado por `MinimumVolume`.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `TakeProfitPoints`, `StopLossPoints` | Distancias de toma de ganancias y stop-loss en puntos del instrumento. |
| `UseTrailingStop`, `TrailingStopPoints`, `ProfitTrailing` | Controles de trailing stop (distancia y si el trailing requiere ganancias abiertas). |
| `BaseVolume`, `UseMoneyManagement`, `RiskPercent`, `AccountMicro`, `MinimumVolume` | Configuración del tamaño de posición. |
| `UseFractals`, `FractalShift*` | Habilita la confirmación fractal y define los desplazamientos de barras para inspeccionar en los marcos de tiempo actuales y superiores. |
| `UseRsi`, `RsiLevelBuy`, `RsiLevelSell`, `RsiType` | RSI filtra compensaciones y períodos de tiempo. |
| `UseStochastic`, `StochasticPeriod*`, `StochasticLevel*` | Stochastic períodos y umbrales del oscilador. |
| `UseMaFilter`, `MaPeriod` | EMA configuración del filtro en el período de tiempo del filtro. |
| `UsePsarFilter`, `PsarStep` | Parabolic SAR configuración del filtro. |
| `UseChannelFilter`, `ChannelLookback`, `ChannelK` | Parámetros del filtro de ruptura de canal. |
| `UsePerceptronFilter`, `PerceptronV1`–`PerceptronV4` | Pesos de filtro de perceptrón (0–100, centrado alrededor de 50). |
| `CandleType`, `HigherFractalType`, `FilterType` | Plazos de datos utilizados por la estrategia. |

## Notas

- La estrategia procesa únicamente velas terminadas, por lo que el comportamiento puede diferir ligeramente del asesor experto original impulsado por ticks.
- El rastreador de fractales reproduce la lógica fractal de cinco barras MT4 y permite al usuario cambiar qué barra histórica se evalúa, coincidiendo con los parámetros MT4 `sh1/ sh2`.
- La gestión del dinero se basa en la valoración de la cartera disponible dentro de StockSharp; cuando no hay valoración disponible, la estrategia vuelve al volumen base fijo.
