# Estratégia de Ordens Pendentes em Notícias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca um par de ordens stop pendentes ao redor do preço atual e as gerencia conforme o mercado evolui. É destinada à negociação durante divulgações de notícias onde movimentos bruscos são esperados.

## Como funciona

- Quando sem posição, a estratégia coloca:
  - Uma ordem de **buy stop** em `Ask + Step`.
  - Uma ordem de **sell stop** em `Bid - Step`.
- Ordens pendentes são reprecificadas a cada `TimeModify` segundos se o mercado se mover pelo menos `StepTrail`.
- Quando uma ordem é executada, a ordem pendente oposta é cancelada.
- Um stop loss protetor e um take profit opcional são criados com base no preço de entrada.
- O stop loss pode ser movido para o ponto de equilíbrio após um lucro definido e depois seguir o preço à medida que avança.

A estratégia opera com dados de Nível1 e não depende de nenhum indicador.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Step` | 10 | Distância em ticks para colocar as ordens stop pendentes. |
| `StopLoss` | 10 | Stop loss inicial em ticks. |
| `TakeProfit` | 50 | Take profit em ticks (0 desativa). |
| `TrailingStop` | 10 | Distância do trailing stop em ticks. |
| `TrailingStart` | 0 | Lucro em ticks antes de ativar o trailing. |
| `StepTrail` | 2 | Mudança mínima no preço do stop (em ticks) para enviar uma nova ordem stop. |
| `BreakEven` | false | Mover o stop para a entrada ao atingir `MinProfitBreakEven`. |
| `MinProfitBreakEven` | 0 | Lucro em ticks necessário para mover o stop ao ponto de equilíbrio. |
| `TimeModify` | 30 | Segundos entre tentativas de reprecificação de ordens pendentes. |

## Notas

- Ordens são gerenciadas usando a API de alto nível do StockSharp.
- A estratégia cancela ordens protetoras quando a posição é fechada.
- Apenas a versão em C# é fornecida; nenhuma implementação em Python está incluída.
