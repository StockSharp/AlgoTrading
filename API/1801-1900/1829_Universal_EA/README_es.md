# Estrategia Universal EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia traducida desde MQL4 "Universal_EA".

Este algoritmo utiliza el Oscilador Estocástico para determinar puntos de entrada.
Se abre una posición larga cuando la línea %K cruza por encima de la línea %D mientras
ambas están por debajo del umbral de sobreventa. Se abre una posición corta cuando %K
cruza por debajo de %D y ambas están por encima del umbral de sobrecompra. Las señales se
verifican solo en velas terminadas y las posiciones se abren con órdenes de mercado.

## Parámetros
- **%K Period** – período base usado para calcular %K.
- **%D Period** – período de suavizado para la línea %D.
- **Slowing** – suavizado adicional aplicado a %K.
- **Oversold** – nivel por debajo del cual el mercado se considera sobrevendido.
- **Overbought** – nivel por encima del cual el mercado se considera sobrecomprado.
- **Candle Type** – marco temporal o tipo de vela usado para el análisis.
