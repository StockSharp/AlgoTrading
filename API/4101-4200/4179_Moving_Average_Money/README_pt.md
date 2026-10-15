# Estratégia de dinheiro médio móvel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia é uma conversão StockSharp do consultor especialista MetaTrader "Moving Average Money". Ele avalia velas concluídas e reage quando a barra anterior cruza uma média móvel simples deslocada. O sistema suporta negociações longas e curtas e mantém todas as decisões sincronizadas com a assinatura de vela de alto nível API.

## Lógica de negociação
- Uma média móvel simples com comprimento configurável e mudança visual é calculada a partir dos preços de fechamento.
- Apenas velas finalizadas são processadas para evitar pedidos duplicados dentro de uma barra.
- **Entrada curta:** quando a vela anterior abre acima da média móvel deslocada e fecha abaixo dela.
- **Entrada longa:** quando a vela anterior abre abaixo da média móvel deslocada e fecha acima dela.
- A estratégia não faz pirâmide de posições; qualquer exposição aberta na direção oposta é fechada antes de estabelecer uma nova negociação.

## Gestão de risco
- A distância do stop loss em unidades de preço é derivada de `MaximumRiskPercent`. O valor atual da carteira, o nível de preço do instrumento e o preço do nível são utilizados para converter a percentagem de risco escolhida em níveis de preço.
- O spread de compra/venda é subtraído da distância baseada no risco sempre que as melhores cotações estão disponíveis.
- Os níveis de lucro são definidos como `stopDistance * ProfitLossFactor`.
- Os níveis de parada e alvo são monitorados em velas concluídas. Quando qualquer um dos níveis é atingido, a posição é achatada com uma ordem de mercado.

## Parâmetros
- `CandleType` – período de tempo usado para detecção de sinal.
- `MovingPeriod` – comprimento da média móvel simples.
- `MovingShift` – número de velas totalmente formadas usadas para deslocar a média móvel para a direita.
- `MaximumRiskPercent` – porcentagem do valor atual do portfólio que define a perda máxima por negociação.
- `ProfitLossFactor` – multiplicador aplicado à distância de parada para calcular a distância de take-profit.
- `TradeVolume` – volume base do pedido para novas entradas (as restrições de etapa de volume são respeitadas automaticamente).

## Notas de implementação
- A estratégia monitora as posições abertas por meio de manipuladores de eventos de alto nível (`OnOwnTradeReceived`) para reinicializar stops e metas após o preenchimento.
- Se os dados de mercado não possuírem cotações ou avaliação de carteira, novas entradas são ignoradas para evitar ordens sem o devido controle de risco.
- O deslocamento da média móvel é emulado com um buffer interno para que a lógica corresponda à versão MetaTrader.
