# Estratégia Pendente DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia StockSharp reproduz o comportamento do consultor especialista MetaTrader "DeMarker Pending 2.5". O bot avalia o oscilador DeMarker em um período de tempo configurável e, quando níveis extremos são ultrapassados, coloca uma ordem pendente na direção do rompimento. A ordem pode ser uma ordem de parada ou de limite compensada por um número fixo de pontos. A filtragem opcional da janela de negociação e a expiração automática mantêm as ordens pendentes alinhadas com o comportamento original do especialista.

## Lógica de negociação
- Assine a série de velas selecionada e calcule o indicador DeMarker com período `DemarkerPeriod`.
- Detecte cruzamentos dos limites inferior (`DemarkerLowerLevel`) e superior (`DemarkerUpperLevel`) usando os valores de vela finalizados atuais e anteriores.
- Quando o nível inferior for cruzado para cima, coloque uma configuração longa na fila; quando o nível superior for cruzado para baixo, coloque uma configuração curta na fila.
- Converta configurações em ordens pendentes ao preço `Close ± PendingIndentPoints * PriceStep`, usando ordens stop no modo breakout ou ordens limitadas para entradas de pullback dependendo de `Mode`.
- Anexe níveis de stop-loss e take-profit à ordem pendente compensando o preço de entrada em `StopLossPoints` e `TakeProfitPoints` pontos.
- Cancele ou reutilize pedidos pendentes mais antigos de acordo com `ReplacePreviousPending` e `SinglePendingOnly` antes de registrar um novo.
- Remova pedidos pendentes automaticamente assim que seu tempo de vida de `PendingExpirationMinutes` expirar.
- Ignore sinais fora da janela intradiária quando `UseTimeWindow` estiver ativado. Cada barra é processada apenas uma vez, portanto, no máximo uma nova ordem pendente é criada por barra e direção.

## Gerenciamento de pedidos
- Todas as entradas são criadas como ordens pendentes (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).
- Cada ordem pendente carrega seus próprios preços de stop-loss e take-profit para que a posição seja protegida imediatamente após a ativação.
- Os pedidos pendentes são cancelados no vencimento, quando substituídos por novas configurações ou quando o estado do pedido muda para um status inativo (preenchido, cancelado, rejeitado).

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `Volume` | Volume do pedido em lotes. |
| `StopLossPoints` | Distância entre o preço de entrada e o stop loss em pontos. |
| `TakeProfitPoints` | Distância entre o preço de entrada e o take-profit em pontos. |
| `PendingIndentPoints` | Compensação entre o preço de mercado e a ordem pendente. |
| `PendingExpirationMinutes` | Vida útil de cada ordem pendente em minutos (0 desativa a expiração). |
| `Mode` | Tipo de ordem pendente (stop para rompimentos ou limite para pullbacks). |
| `SinglePendingOnly` | Se ativado, impede a colocação de mais de uma ordem pendente ativa. |
| `ReplacePreviousPending` | Cancela ordens pendentes ativas antes de emitir uma nova. |
| `DemarkerPeriod` | Período de lookback do oscilador DeMarker. |
| `DemarkerUpperLevel` | Limite do DeMarker que aciona configurações de venda. |
| `DemarkerLowerLevel` | Limite do DeMarker que aciona configurações de compra. |
| `CandleType` | Prazo utilizado para assinatura de velas e avaliação de indicadores. |
| `UseTimeWindow` | Ativa a filtragem de horário intradiário. |
| `StartTime` | Início da janela de negociação intradiária. |
| `EndTime` | Fim da janela de negociação intradiária. |

## Notas
- O especialista original inclui gerenciamento sofisticado de dinheiro e rotinas de trailing stop. Esta porta mantém a geração de sinal e o tratamento de ordens pendentes, mas simplifica o dimensionamento da posição para um único parâmetro `Volume` fixo.
- StockSharp anexa preços de stop-loss e take-profit no momento do registro do pedido; dependendo da corretora, pode ser necessário verificar se as ordens stop e limit suportam esses níveis de proteção.
- Certifique-se sempre de que as distâncias baseadas em pontos sejam compatíveis com o `PriceStep` do símbolo negociado. Defina `PendingIndentPoints`, `StopLossPoints` e `TakeProfitPoints` com valores que atendam aos requisitos de distância mínima do corretor.
