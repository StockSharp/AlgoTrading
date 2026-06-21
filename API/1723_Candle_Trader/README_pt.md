# Estratégia Candle Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Candle Trader** analisa a direção (de alta ou de baixa) das últimas quatro velas completadas para identificar oportunidades de reversão de curto prazo. Opera em um único instrumento e envia ordens a mercado com níveis predefinidos de take profit e stop loss.

## Lógica da estratégia

1. **Entrada comprada (direta)** – última vela de alta, as duas anteriores de baixa.
2. **Entrada comprada (continuação)** – última vela de alta, a anterior de baixa, as duas anteriores de alta. Esta regra só é ativada quando *Continuation* é `true`.
3. **Entrada vendida (direta)** – última vela de baixa, as duas anteriores de alta.
4. **Entrada vendida (continuação)** – última vela de baixa, a anterior de alta, as duas anteriores de baixa. Ativada apenas quando *Continuation* é `true`.
5. Se *Reverse Close* estiver habilitado e aparecer um novo sinal oposto à posição atual, a estratégia fecha a posição existente antes de abrir uma nova.
6. Todas as ordens são protegidas por valores fixos de take profit e stop loss medidos em passos de preço.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `Volume` | Volume da ordem para cada negociação. |
| `TakeProfitTicks` | Distância do take profit em passos de preço. |
| `StopLossTicks` | Distância do stop loss em passos de preço. |
| `Continuation` | Habilita os padrões de continuação para entradas adicionais. |
| `ReverseClose` | Fecha uma posição aberta antes de entrar na direção oposta. |
| `CandleType` | Período de velas usado para análise. |

## Notas

- A estratégia avalia apenas velas finalizadas.
- Usa ordens a mercado e cancela quaisquer ordens ativas antes de enviar novas.
- Os níveis de stop loss e take profit são aplicados via `StartProtection`.
- O tamanho da posição pode ser otimizado por meio do parâmetro `Volume`.
