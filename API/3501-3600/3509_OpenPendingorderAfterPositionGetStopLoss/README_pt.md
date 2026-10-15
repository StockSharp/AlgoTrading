# Estratégia OpenPendingorderAfterPositionGetStopLoss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **OpenPendingorderAfterPositionGetStopLoss** transporta o MetaTrader 5 consultor especialista de mesmo nome para o StockSharp API de alto nível. Ele avalia continuamente a inclinação da linha Stochastic %K no período de tempo selecionado. Quando %K cai, ele coloca uma ordem de stop de venda abaixo do mercado, e quando %K sobe, ele coloca uma ordem de stop de compra acima do mercado. Cada entrada preenchida recebe imediatamente uma ordem protetora de stop-loss e take-profit. Se um stop-loss fechar a posição, a estratégia reinstala automaticamente a ordem pendente correspondente para que a grade de negociações de breakout seja restaurada sem esperar pela próxima vela.

## Regras de negociação
- Assine velas finalizadas do período configurado e calcule um oscilador Stochastic clássico (`KPeriod`, `DPeriod`, `Slowing`).
- Compare o valor atual de %K com o valor de duas barras atrás:
  - `%K(current) < %K(two bars ago)` → envie um stop de venda abaixo do melhor lance.
  - `%K(current) > %K(two bars ago)` → envie um stop de compra acima da melhor venda.
- As ordens pendentes são compensadas do mercado pelo spread atual mais o buffer `MinStopDistancePoints` definido pelo usuário, correspondendo à lógica MQL original.
- Assim que uma ordem pendente é preenchida, a estratégia envia um stop-loss de proteção (ordem stop) e um take-profit opcional (ordem limite).
- Quando o stop-loss de proteção é acionado, a ordem pendente correspondente é recriada imediatamente usando os preços de mercado mais recentes.
- As ordens protetoras são canceladas automaticamente quando a posição é fechada pelo take-profit ou quando a estratégia é interrompida.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `OrderVolume` | Volume de negociação em lotes para cada ordem pendente. |
| `StopLossPoints` | Distância de stop-loss em pontos de símbolo. Defina como 0 para desativar. |
| `TakeProfitPoints` | Distância de lucro em pontos de símbolo. Defina como 0 para desativar. |
| `MinStopDistancePoints` | Buffer de preço mínimo (em pontos) adicionado ao spread antes de colocar uma ordem pendente. |
| `MaxPositions` | Número máximo de posições simultâneas por direção (as contas de compensação usam efetivamente 0 ou 1). |
| `KPeriod` | Número de barras utilizadas para o cálculo de %K. |
| `DPeriod` | Comprimento da linha %D de suavização. |
| `Slowing` | Fator de suavização adicional aplicado a %K antes da comparação. |
| `PendingExpiry` | Vida útil opcional de ordens stop pendentes. Pedidos expirados são cancelados na próxima vela. |
| `CandleType` | Prazo usado para assinatura de velas e cálculos de indicadores. |

## Notas de implementação
- Todo gerenciamento de pedidos depende de ajudantes de alto nível, como `BuyStop`, `SellStop`, `SellLimit` e `BuyLimit` conforme exigido por `AGENTS.md`.
- Os valores do indicador são consumidos diretamente dentro do retorno de chamada `SubscribeCandles().BindEx(...)`, evitando quaisquer chamadas `GetValue`.
- A estratégia monitora eventos `MyTrade` para instalar e remover ordens de proteção, emulando a lógica `OnTradeTransaction` do Expert Advisor original.
- Os comentários dentro do código são escritos em inglês e o recuo é feito com tabulações, obedecendo às diretrizes do repositório.
