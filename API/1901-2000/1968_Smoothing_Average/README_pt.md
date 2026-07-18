# Estratégia de Média de Suavização
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia opera em torno de uma média móvel simples (SMA) com um deslocamento de suavização adicional. Ela tenta explorar desvios de preço em relação à média móvel, entrando em posições quando o preço de fechamento cruza uma distância de deslocamento da média.

## Como funciona
- Calcular uma SMA do tipo de candle escolhido.
- Se não houver posição aberta:
  - Entrar em posição vendida quando o preço de fechamento estiver abaixo de `SMA + Smoothing`.
  - Entrar em posição comprada quando o preço de fechamento estiver acima de `SMA - Smoothing`.
- Para uma posição vendida aberta:
  - Fechar a posição quando o preço de fechamento subir acima de `SMA + Smoothing`.
- Para uma posição comprada aberta:
  - Fechar a posição quando o preço de fechamento cair abaixo de `SMA - Smoothing`.

A estratégia usa ordens a mercado e trabalha apenas com candles finalizados.

## Parâmetros
- **MA Period** – período de lookback para a SMA.
- **Smoothing** – deslocamento de preço adicionado ou subtraído da SMA ao gerar sinais.
- **Candle Type** – período dos candles usados nos cálculos.

## Notas
Esta conversão é baseada no script MQL4 original `smoothingaverage.mq4`.
