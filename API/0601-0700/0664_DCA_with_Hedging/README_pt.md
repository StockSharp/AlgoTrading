# Estratégia DCA com Hedging
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra comprado após três candles consecutivos fecharem acima do EMA e entra vendido após três candles consecutivos fecharem abaixo. Posições adicionais são adicionadas quando o preço se move contra a última entrada em um percentual determinado. Toda a posição é fechada quando o preço se move o percentual de take profit a partir do preço médio de entrada.

## Parâmetros
- Tipo de candle
- Comprimento do EMA
- Intervalo DCA %
- Take profit %
- Tamanho inicial da posição

