# Estratégia Universal EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia traduzida do MQL4 "Universal_EA".

Este algoritmo usa o Oscilador Estocástico para determinar pontos de entrada.
Uma posição comprada é aberta quando a linha %K cruza acima da linha %D enquanto
ambas estão abaixo do limite de sobrevenda. Uma posição vendida é aberta quando %K
cruza abaixo de %D e ambas estão acima do limite de sobrecompra. Os sinais são
verificados apenas em velas fechadas e as posições são abertas por ordens a mercado.

## Parâmetros
- **%K Period** – período base usado para calcular %K.
- **%D Period** – período de suavização para a linha %D.
- **Slowing** – suavização adicional aplicada a %K.
- **Oversold** – nível abaixo do qual o mercado é considerado sobrevendido.
- **Overbought** – nível acima do qual o mercado é considerado sobrecomprado.
- **Candle Type** – período ou tipo de vela usado para análise.
