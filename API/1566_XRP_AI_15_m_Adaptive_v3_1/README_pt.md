# Estratégia Adaptativa XRP AI de 15 m v3.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera XRP em velas de 15 minutos usando um filtro de tendência de período temporal superior. Seleciona entre pequenos recuos, descargas de volume médias ou grandes explosões de momentum, e aplica stops, alvos baseados em ATR, stop trailing e uma saída baseada em tempo.

## Parâmetros
- **Risk Mult** – multiplicador ATR para o stop inicial.
- **Small TP** – multiplicador ATR para take profit em um pequeno recuo.
- **Med TP** – multiplicador ATR para take profit em uma descarga de volume média.
- **Large TP** – multiplicador ATR para take profit em uma grande explosão de momentum.
- **Volume Mult** – multiplicador de volume SMA-20 para detectar picos.
- **Trail Percent** – percentagem ATR do stop trailing a partir do preço mais alto.
- **Trail Arm** – ganho aberto em múltiplos ATR antes de ativar o trailing.
- **Max Bars** – número máximo de velas de 15 minutos para manter uma posição.
- **Candle Type** – tipo de vela usado para os cálculos principais.
- **Trend Candle Type** – tipo de vela usado para o filtro de tendência.
