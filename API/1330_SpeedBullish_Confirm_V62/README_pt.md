# SpeedBullish Strategy Confirm V6.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Estratégia que combina filtro de tendência EMA, cruzamento do histograma MACD e limiar RSI. Filtros opcionais de ATR e volume melhoram a qualidade dos sinais.

## Condições de entrada
- Preço acima da EMA10 ou EMA15 para compras, abaixo para vendas.
- Histograma MACD cruzando acima de zero para compras, abaixo de zero para vendas.
- RSI maior ou menor que o nível especificado.
- Opcional: o ATR deve superar sua média móvel pelo multiplicador.
- Opcional: o volume deve superar a SMA pelo multiplicador.

## Condições de saída
- Sinal de entrada oposto.
- Take profit e trailing stop em pontos.
- Stop loss manual em pontos.
