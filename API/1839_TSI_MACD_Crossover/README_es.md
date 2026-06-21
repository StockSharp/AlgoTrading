# Estrategia de Cruce TSI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa un sistema de cruce basado en el True Strength Index (TSI) y su línea de señal de media móvil exponencial.

La estrategia se suscribe a velas de 4 horas por defecto y calcula el TSI con longitudes de suavizado corto y largo configurables. Una EMA adicional produce la línea de señal. Se abre una posición larga cuando el TSI cruza por encima de la línea de señal; se abre una posición corta cuando el TSI cruza por debajo de la línea de señal. Las posiciones opuestas se cierran automáticamente en el cruce inverso.

- Indicadores: True Strength Index, Exponential Moving Average
- Parámetros:
  - `CandleType` – serie de velas a procesar.
  - `LongLength` – período de suavizado largo para TSI.
  - `ShortLength` – período de suavizado corto para TSI.
  - `SignalLength` – período de la línea de señal EMA.
