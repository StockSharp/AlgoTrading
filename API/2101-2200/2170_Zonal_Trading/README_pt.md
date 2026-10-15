# Estratégia de Trading Zonal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Awesome Oscillator (AO) e o Accelerator Oscillator (AC) para capturar mudanças no momentum do mercado.

## Lógica
- Comprar quando tanto AO quanto AC sobem acima de seus valores anteriores e pelo menos um deles girou para cima a partir da barra anterior enquanto ambos os osciladores são positivos.
- Vender quando tanto AO quanto AC caem abaixo de seus valores anteriores e pelo menos um deles girou para baixo a partir da barra anterior enquanto ambos os osciladores são negativos.
- Fechar posição comprada quando AO e AC giram para baixo.
- Fechar posição vendida quando AO e AC giram para cima.

## Parâmetros
- **Candle Type** – série de velas fonte para os cálculos.
- **Take Profit** – valor fixo de take-profit em unidades de preço.

A estratégia opera uma única posição por vez usando ordens a mercado.
