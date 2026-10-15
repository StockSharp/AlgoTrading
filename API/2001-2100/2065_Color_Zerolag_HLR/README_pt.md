# Estratégia Color Zerolag HLR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão em C# do especialista MQL5 `Exp_ColorZerolagHLR`. Combina múltiplos osciladores Hi-Lo Range (HLR) com diferentes comprimentos e pesos, depois aplica suavização exponencial para construir linhas de tendência rápidas e lentas. Cruzamentos entre estas linhas geram sinais de trading.

## Visão geral
- Constrói cinco valores HLR usando o máximo mais alto e o mínimo mais baixo durante períodos especificados.
- Pondera cada HLR e os soma para produzir uma linha de tendência rápida.
- Aplica suavização zero-lag para derivar uma linha de tendência lenta.
- Opera quando a linha rápida cruza a linha lenta.

## Parâmetros
- `Smoothing` – fator de suavização EMA.
- `Factor1`..`Factor5` – pesos para cada componente HLR.
- `HlrPeriod1`..`HlrPeriod5` – períodos de lookback para cálculos HLR.
- `BuyPosOpen`/`SellPosOpen` – permitem abrir posições compradas ou vendidas.
- `BuyPosClose`/`SellPosClose` – permitem fechar posições existentes.
- `CandleType` – período das velas.

## Indicadores
- Highest, Lowest (cinco pares cada).

## Lógica de trading
- Se a linha rápida anterior estava acima da linha lenta e agora cruza abaixo, a estratégia abre uma posição comprada e fecha qualquer vendida.
- Se a linha rápida anterior estava abaixo da linha lenta e agora cruza acima, a estratégia abre uma posição vendida e fecha qualquer comprada.

Use este modelo como ponto de partida e ajuste os parâmetros ou a gestão de risco de acordo com as suas necessidades.
