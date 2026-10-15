# Estratégia de Gráfico ao Vivo Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia emula um gráfico clássico de tijolos Renko e opera em mudanças de direção dos tijolos. Foi convertida do script MetaTrader **RenkoLiveChart_v600**.

## Lógica

A estratégia constrói tijolos Renko usando candles temporais concluídos. Quando o preço se move pelo menos o tamanho de caixa selecionado a partir do preço do último tijolo, um novo tijolo é formado. Uma posição comprada é aberta em um tijolo ascendente e uma posição vendida em um tijolo descendente.

## Parâmetros

- **Candle Type** – período dos candles de entrada usados para a construção dos tijolos.
- **Brick Size** – passo de preço que define a altura de um tijolo Renko.
- **Brick Offset** – deslocamento inicial em tijolos aplicado ao primeiro tijolo.
- **Show Wicks** – exibir mechas no gráfico ao desenhar candles.

## Notas

- As operações são executadas apenas em candles concluídos.
- A proteção de posição é iniciada automaticamente ao iniciar a estratégia.
- Esta implementação foca no comportamento central do Renko e ignora funcionalidades avançadas do script original, como o manuseio de arquivos externos.
