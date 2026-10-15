# Estratégia Ergodic Ticks Volume OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia adapta o especialista MQL5 "Exp_Ergodic_Ticks_Volume_OSMA" para StockSharp. O especialista original usa um indicador personalizado para avaliar o momentum do volume de ticks. Nesta versão, o indicador personalizado é aproximado pelo histograma MACD.

A estratégia busca aumentos ou diminuições consecutivas no histograma:
- Dois passos ascendentes desencadeiam uma entrada comprada e fecham qualquer posição vendida.
- Dois passos descendentes desencadeiam uma entrada vendida e fecham qualquer posição comprada.

`StartProtection()` é usado para evitar conflitos com posições existentes quando a estratégia inicia.

## Parâmetros
- `FastLength` – período da EMA rápida para o MACD. Padrão: 12.
- `SlowLength` – período da EMA lenta para o MACD. Padrão: 26.
- `SignalLength` – período da EMA de sinal para o MACD. Padrão: 9.
- `CandleType` – período das velas, padrão 8 horas.

## Lógica de trading
1. Subscrever velas do `CandleType` selecionado.
2. Calcular o histograma MACD para cada vela finalizada.
3. Se o histograma cresce por duas barras consecutivas, fechar vendidos e comprar.
4. Se o histograma cai por duas barras consecutivas, fechar comprados e vender.
5. Continuar processando a cada nova vela.
