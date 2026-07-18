# Estratégia de Cruzamento Vortex com Confirmação de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o indicador Vortex para detectar reversões de tendência e confirma as entradas com uma média móvel suavizada. Uma operação comprada é aberta quando o Vortex positivo cruza acima do negativo e o preço está acima da linha de suavização. Uma operação vendida ocorre no cruzamento oposto abaixo da linha.

## Parâmetros
- **Vortex Length** – período para o cálculo do Vortex.
- **SMA Length** – comprimento da SMA base.
- **Smoothing Length** – comprimento da média móvel de suavização.
- **MA Type** – método de suavização.
- **Candle Type** – período dos candles processados.
