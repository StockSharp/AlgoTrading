# ROC Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia ROC é uma porta StockSharp do consultor especialista MetaTrader armazenada em `MQL/26938/ROC.mq4`. Ele opera em um único símbolo e avalia a ação do preço usando uma cadeia de médias móveis lineares ponderadas (LWMA), um modelo de taxa de mudança personalizado (ROC), impulso de período de tempo mais alto e um filtro mensal MACD. Os recursos originais de gerenciamento de dinheiro, como ponto de equilíbrio, trailing stops baseados em pip, proteção de patrimônio e metas de lucro denominadas em dinheiro, são preservados.

## Lógica de entrada
1. A estratégia assina três fluxos de dados:
   - Velas de negociação primárias definidas pela propriedade `CandleType`.
   - Um período de tempo mais alto para o oscilador de impulso de 14 períodos (selecionado automaticamente de acordo com o período de negociação).
   - Velas mensais para o filtro de confirmação MACD.
2. Em cada vela de negociação finalizada, as seguintes condições devem ser satisfeitas para abrir uma posição:
   - O modelo personalizado ROC deve relatar uma tendência de alta (`Line4 < Line5`) para compras ou uma tendência de baixa (`Line4 > Line5`) para vendas.
   - O LWMA rápido calculado sobre o preço típico deve ser negociado acima do LWMA lento para compras e abaixo para vendas.
   - Qualquer uma das últimas três leituras de momentum obtidas no período superior deve exceder o limite de compra ou venda configurado (desvio absoluto de 100).
   - A linha principal mensal MACD deve ficar acima de sua linha de sinal para compras e abaixo para vendas.
   - O dimensionamento da posição respeita o limite `MaxTrades` e, opcionalmente, dimensiona o próximo volume de negociação após perdas consecutivas quando `IncreaseFactor` for maior que zero.

## Lógica de saída
- As ordens clássicas de stop-loss e take-profit são projetadas em MetaTrader pontos assim que o tamanho da posição muda.
- O bloco de ponto de equilíbrio opcional move o stop de proteção para o preço de entrada mais o deslocamento configurado quando a distância de disparo em pontos é atingida.
- Os trailing stops baseados em pip restringem o valor do stop em cada fechamento de vela.
- As verificações de gerenciamento de dinheiro fecham a posição quando uma meta de moeda ou meta percentual é atingida e podem rastrear o lucro flutuante detectando retrocessos maiores que `StopLossMoney` após o lucro exceder `TakeProfitMoney`.
- Um equity stop compara o rebaixamento flutuante com o patrimônio líquido mais alto registrado e liquida a posição quando o percentual permitido é excedido.
- Definir `ExitStrategy` como `true` executa a rotina de saída de emergência e fecha a posição atual no mercado.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `LotSize` | Volume base de negociação aberto em cada sinal. |
| `IncreaseFactor` | Recalcula o próximo volume após negociações consecutivas com perdas. |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimento dos filtros de tendência LWMA. |
| `PeriodMa0`, `PeriodMa1`, `BarsV`, `AverBars`, `KCoefficient` | Defina o modelo de tendência ROC customizado. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Desvio absoluto mínimo de 100 usado pelo filtro de momentum de período de tempo mais alto. |
| `StopLossSteps`, `TakeProfitSteps` | Distâncias de proteção iniciais expressas em MetaTrader pontos. |
| `TrailingStopSteps` | Trailing stop baseado em pip. |
| `UseBreakEven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Configure o módulo de equilíbrio. |
| `UseTpInMoney`, `TpInMoney`, `UseTpInPercent`, `TpInPercent` | Metas de lucro baseadas em dinheiro e porcentagem. |
| `EnableMoneyTrailing`, `TakeProfitMoney`, `StopLossMoney` | Parâmetros do módulo de rastreamento de dinheiro. |
| `UseEquityStop`, `TotalEquityRisk` | Configurações de proteção patrimonial. |
| `MaxTrades` | Número máximo de reduções por direção. |
| `ExitStrategy` | Força uma posição plana imediata quando ativado. |

## Notas
- O prazo mais alto para o indicador de momentum é derivado automaticamente do prazo de negociação para corresponder à instrução switch original no código MetaTrader.
- Todos os cálculos de indicadores usam o nível superior `Bind` API, portanto, nenhuma solicitação manual de dados é necessária.
- A estratégia é apenas de compensação: quando um novo sinal longo aparece enquanto se mantém vendido, a exposição curta é fechada primeiro antes de entrar longa, refletindo o comportamento do EA original em contas sem hedge.
