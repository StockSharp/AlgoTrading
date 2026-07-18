# Estrategia de Histograma Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto StockSharp del experto MQL original `Exp_Stochastic_Histogram`.
Utiliza el oscilador Stochastic para producir señales de trading contrarias en dos modos:

- **Levels** – aparece una señal cuando %K sale de las áreas de sobrecompra o sobreventa definidas por `HighLevel` y `LowLevel`.
- **Cross** – aparece una señal cuando %K cruza la línea %D. La operación se abre en la dirección opuesta al cruce.

Siempre que se recibe una nueva señal, la estrategia cierra la posición existente y abre una nueva en la dirección requerida.

## Parámetros

- `KPeriod` – período principal de %K.
- `DPeriod` – período de suavizado de %D.
- `Slowing` – suavizado adicional de %K.
- `HighLevel` – umbral superior para el modo Levels.
- `LowLevel` – umbral inferior para el modo Levels.
- `Mode` – Levels o Cross.
- `CandleType` – marco temporal de velas utilizado para los cálculos.

## Cómo funciona

Para cada vela completada, el oscilador Stochastic se actualiza y evalúa. En el modo **Levels** se abre una operación larga cuando %K regresa por debajo del nivel alto, y una operación corta cuando %K sube por encima del nivel bajo. En el modo **Cross** se abre una operación larga en cruces descendentes de %K por debajo de %D, mientras que los cruces ascendentes desencadenan operaciones cortas. La estrategia tiene como máximo una posición abierta en todo momento.
