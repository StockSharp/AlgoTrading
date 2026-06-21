# Estrategia de Reversión MACD Volume BBO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina el oscilador de volumen con los cruces de la línea cero del MACD y la comparación de señales.
Entra en largo cuando el MACD cruza por encima de cero con oscilador de volumen positivo y MACD por encima de su señal.
Las entradas cortas son simétricas. El stop loss utiliza el mínimo/máximo reciente y el take profit se basa en la relación riesgo/recompensa.

## Parámetros
- `VolumeShortLength` – período EMA corto para el volumen (predeterminado: 6)
- `VolumeLongLength` – período EMA largo para el volumen (predeterminado: 12)
- `MacdFastLength` – período de media rápida para el MACD (predeterminado: 11)
- `MacdSlowLength` – período de media lenta para el MACD (predeterminado: 21)
- `MacdSignalLength` – período de línea de señal para el MACD (predeterminado: 10)
- `LookbackPeriod` – barras para calcular el máximo/mínimo reciente (predeterminado: 10)
- `RiskReward` – ratio take profit / stop loss (predeterminado: 1.5)
- `CandleType` – marco temporal de las velas (predeterminado: 5 minutos)
