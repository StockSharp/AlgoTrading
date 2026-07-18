# Estrategia de Operación Automática con Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza Bollinger Bands, RSI y el oscilador estocástico para abrir operaciones automáticamente durante una ventana de tiempo GMT especificada. Se abre una posición corta cuando la vela anterior cierra por encima de la banda superior de Bollinger Bands con el RSI por encima de 75 y el %K del estocástico por encima de 85. Se abre una posición larga cuando la vela cierra por debajo de la banda inferior con el RSI por debajo de 25 y el %K del estocástico por debajo de 155. Solo se permite una posición por dirección. Un trailing stop en puntos protege las posiciones abiertas.

## Parámetros

- `OpenBuy` – habilitar la apertura de posiciones largas.
- `OpenSell` – habilitar la apertura de posiciones cortas.
- `GmtTradeStart` – hora de inicio de trading en GMT (exclusiva).
- `GmtTradeStop` – hora de fin de trading en GMT (exclusiva).
- `BbPeriod` – período para Bollinger Bands.
- `RsiPeriod` – período para el indicador RSI.
- `StochKPeriod` – período %K para el oscilador estocástico.
- `StochDPeriod` – período %D para el oscilador estocástico.
- `StochSlowing` – factor de suavizado para el oscilador estocástico.
- `TrailingStop` – distancia del trailing stop en puntos.
- `CandleType` – marco temporal de velas utilizado para los cálculos.
