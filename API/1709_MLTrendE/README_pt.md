# Estratégia MLTrendE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera na direção de uma média móvel ponderada (WMA) e opcionalmente aumenta posições quando o preço se move favoravelmente.

## Lógica

- Calcular uma WMA da série de velas selecionada.
- Se não houver posição aberta:
  - **Tipo de operação 0**: abrir uma posição comprada quando o preço de fechamento está acima da WMA, ou uma posição vendida quando está abaixo.
  - **Tipo de operação 1**: sempre abrir uma posição comprada.
  - **Tipo de operação 2**: sempre abrir uma posição vendida.
- Quando uma posição está aberta e atinge o alvo de lucro especificado, uma nova operação com volume escalado é adicionada.
- Assim que o número máximo de operações for atingido, toda a posição é fechada no próximo alvo de lucro.

## Parâmetros

- `Volume` – volume base de operação.
- `Multiplier1` – multiplicador de volume para a segunda operação.
- `Multiplier2` – multiplicador de volume para a terceira operação.
- `TakeProfit` – lucro em unidades de preço necessário para escalar ou fechar.
- `Map` – período da média móvel ponderada.
- `MaxTrades` – número máximo de operações consecutivas.
- `TradeType` – 0 seguidor de tendência, 1 forçar comprado, 2 forçar vendido.
- `CandleType` – período das velas analisadas.

## Notas

A estratégia usa apenas velas completadas e ordens de mercado. Não gerencia stops nem risco; use proteção de conta se necessário.
