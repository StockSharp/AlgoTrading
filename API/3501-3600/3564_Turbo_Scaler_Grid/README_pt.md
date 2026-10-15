# Estratégia de grade Turbo Scaler
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Turbo Scaler Grid é uma implementação StockSharp de alto nível do consultor especialista MQL5 "Turbo Scaler Grid Pending". A estratégia se concentra no gerenciamento de grades de stop pendentes em torno de níveis de preços predefinidos, protegendo dinamicamente as posições abertas com ponto de equilíbrio e lógica de trailing e supervisionando o patrimônio da conta para fechar posições quando os limites de lucro ou perda são atingidos.

A lógica funciona em vários períodos de tempo simultaneamente:

- Um período de tempo de disparo configurável observa sinais de proximidade de preço que ativam a grade pendente.
- Velas adicionais de 30 minutos, 2 horas e diárias fornecem confirmação para gatilhos condicionais opcionais.
- Os dados de nível 1 fornecem os valores de compra/venda mais recentes usados para posicionar ordens pendentes e gerenciar trailing stops.

## Regras de negociação
1. **Grade pendente**
   - As ordens de compra e venda são colocadas a partir de preços âncora configuráveis (`BuyStopEntry` e `SellStopEntry`).
   - Os pedidos são espaçados por `PendingStepPoints` e limitados por `PendingQuantity`.
   - O gatilho de preço verifica as velas recentes no período de gatilho para confirmar que o preço se aproximou do nível âncora com impulso suficiente.
   - O gatilho de condição valida filtros adicionais de vários períodos de tempo (intervalos de blocos diários, direção de velas H2 e M30 e nível médio) antes de colocar ordens pendentes.
2. **Proteção de posição**
   - O stop loss inicial é calculado a partir de `StopLossPoints` (ou substituições de preço fixo).
   - Quando o preço avança `BreakevenTriggerPoints`, o stop é movido para o preço de entrada mais `BreakevenOffsetPoints` (para posições compradas) ou menos o deslocamento (para posições vendidas).
   - Um trailing stop é ativado somente após o ponto de equilíbrio ser atingido, sendo atualizado quando o preço excede o stop anterior em `TrailMultiplier * TrailPoints`.
3. **Supervisão patrimonial**
   - A estratégia monitora o PnL flutuante e força a liquidação da posição se o rebaixamento exceder `MaxFloatLoss` (escalonado para o volume do pedido selecionado).
   - Um gatilho de lucro flutuante bloqueia ganhos colocando uma linha de patrimônio interno em `EquityBreakeven` e seguindo-a por `EquityTrail` quando o lucro ultrapassar `EquityTrigger`.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `StopLossPoints` | Distância inicial de stop-loss em pontos. |
| `BreakevenTriggerPoints` | Pontos necessários para ativar o movimento de equilíbrio. |
| `BreakevenOffsetPoints` | Compensação adicionada ao preço de entrada quando o stop é movido para o ponto de equilíbrio. |
| `TrailPoints` | Distância usada para trilhar após o ponto de equilíbrio. |
| `TrailMultiplier` | Multiplicador aplicado antes de um novo trailing stop ser definido. |
| `BuyStopLossPrice` / `SellStopLossPrice` | Preços de parada fixa opcionais para posições longas/curtas. |
| `BuyStopEntry` / `SellStopEntry` | Preços base para as grades de stop pendentes. |
| `OrderVolume` | Volume por ordem pendente. |
| `PendingQuantity` | Número máximo de ordens pendentes ativas. |
| `PendingStepPoints` | Distância entre ordens pendentes consecutivas. |
| `TriggerCandleType` | Série de velas usada para a lógica de gatilho de preço. |
| `PendingPriceTrigger` | Ativa o gatilho de proximidade de preço. |
| `PendingConditionTrigger` | Ativa o gatilho de confirmação de vários períodos. |
| `OrderBuyBlockStart` / `OrderBuyBlockEnd` | Bloco baixo diário usado para validar configurações longas. |
| `OrderSellBlockStart` / `OrderSellBlockEnd` | Bloco alto diário usado para validar configurações curtas. |
| `MaxFloatLoss` | Perda flutuante máxima permitida (escalonada por volume). |
| `EquityBreakeven` | Nível de patrimônio mantido após a ativação do gatilho de lucro. |
| `EquityTrigger` | Lucro flutuante necessário para criar o bloqueio de capital. |
| `EquityTrail` | Distância final aplicada ao bloqueio de patrimônio. |

## Notas
- O volume do pedido é dimensionado para corresponder ao comportamento original do EA (`0.01` lotes são tratados como a etapa base).
- Todos os comentários dentro do código são escritos em inglês, enquanto este documento fornece uma descrição detalhada para uma integração rápida.
- A estratégia usa apenas APIs StockSharp de alto nível (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop`, `SellMarket`, `BuyMarket`) de acordo com os requisitos do projeto.
