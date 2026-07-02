# Estratégia de grade de elásticos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista MetaTrader 4 **RUBBERBANDS_2.mq4**.
- Executa uma grade simétrica em torno do preço atual usando as melhores cotações de compra/venda em vez de velas.
- Mantém livros contábeis separados para exposição longa e curta para que o comportamento corresponda à implementação do MT4 coberto.
- Implementa controles de lucros e perdas em nível de sessão e um modo de suspensão/parada manual idêntico às entradas originais.

## Lógica de negociação
1. A estratégia assina `SubscribeLevel1()` e reage a cada alteração do melhor lance e do melhor pedido.
2. Dois extremos flutuantes (`_upperExtreme` / `_lowerExtreme`) capturam o preço de venda mais alto e mais baixo alcançado desde a última redefinição. Eles são inicializados a partir de parâmetros quando `UseInitialValues` for verdadeiro, caso contrário, o primeiro preço de venda recebido será usado.
3. Quando não há negociações abertas e o horário do servidor atinge o primeiro tick de um minuto (o segundo é igual a zero), a estratégia solicita uma compra e uma venda no mercado. Isso reflete o comportamento do MT4, onde os sinalizadores de compra/venda são definidos a cada minuto enquanto o livro está vazio.
4. Cada vez que o preço de venda avança `GridStepPoints` pontos acima da máxima armazenada, uma nova ordem de venda é emitida. Cada queda na mesma distância abaixo do mínimo armazenado aciona uma nova ordem de compra. Os extremos são atualizados para a oferta atual após cada gatilho, para que a escada “se alargue” com o preço.
5. O número total de negociações abertas simultaneamente (soma das pernas longas e curtas) é limitado por `MaxTrades`.
6. O lucro flutuante é calculado a partir do lance/pedido atual: os lucros longos usam o lance menos o preço médio do longo, os lucros curtos usam o preço médio do curto menos o pedido. O auxiliar `PriceToMoney` converte as diferenças de preço na moeda da conta usando `PriceStep`/`StepPrice` quando disponível.
7. Quando o lucro flutuante atinge `SessionTakeProfitPerLot * OrderVolume` e `UseSessionTakeProfit` está ativado, toda a exposição é nivelada. Da mesma forma, a perda flutuante abaixo de `-SessionStopLossPerLot * OrderVolume` aciona uma saída completa quando `UseSessionStopLoss` está ativado.
8. Os sinalizadores manuais reproduzem as opções originais do EA: `CloseNow` impõe um início plano, `QuiesceMode` mantém a estratégia inativa enquanto está plana e `StopNow` interrompe novas entradas sem interferir nas posições existentes.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Volume para cada ordem de mercado (MT4 `Lots`). |
| `MaxTrades` | Contagem máxima de negociações abertas simultaneamente (MT4 `maxcount`). |
| `GridStepPoints` | Distância em faixas de preço entre as camadas da grade (MT4 `pipstep`). |
| `QuiesceMode` | Se ativada, a estratégia espera uma vez, idêntico a `quiescenow`. |
| `TriggerImmediateEntries` | Abre uma compra e venda inicial assim que a estratégia estiver pronta (`donow`). |
| `StopNow` | Pausa entradas automatizadas enquanto mantém as posições atuais ativas (`stopnow`). |
| `CloseNow` | Solicita um nivelamento imediato no início (`closenow`). |
| `UseSessionTakeProfit` & `SessionTakeProfitPerLot` | Meta de lucro flutuante no nível da sessão por lote. |
| `UseSessionStopLoss` & `SessionStopLossPerLot` | Limite de perda flutuante no nível da sessão por lote. |
| `UseInitialValues`, `InitialMax`, `InitialMin` | Suporte de reinicialização opcional que reutiliza extremos anteriores (`useinvalues`, `inmax`, `inmin`). |

## Notas de implementação
- Todo o estado interno é recuado por tabulação e armazenado em campos, em vez de coleções, para seguir as diretrizes do projeto.
- As ordens de mercado são aceleradas pelo rastreamento de `_activeBuyOrder` e `_activeSellOrder` para que nenhuma solicitação duplicada seja enviada enquanto a anterior estiver pendente.
- A contabilidade de hedge é realizada em `OnOwnTradeReceived` onde os preços/volumes médios longos e curtos são atualizados de forma independente e convertidos em lucro flutuante para lógica de parada.
- `TryCloseAll()` espelha a rotina MT4 `close1by1()`, enviando ordens de mercado opostas até que ambos os livros estejam estáveis e, em seguida, redefinindo os extremos para a última venda.
- A estratégia depende exclusivamente de chamadas API de alto nível (`SubscribeLevel1()`, `BuyMarket`, `SellMarket`) e evita o acesso direto ao indicador conforme exigido pelas regras do repositório.
