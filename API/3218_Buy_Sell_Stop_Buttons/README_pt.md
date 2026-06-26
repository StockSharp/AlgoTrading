# Estratégia de Buy Sell Stop Buttons
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Recria o consultor especialista "Buy Sell Stop Buttons" do MetaTrader 4 dentro do StockSharp.
- Fornece três parâmetros manuais (`BuyRequest`, `SellRequest`, `CloseRequest`) que emulam os botões do gráfico.
- Implementa os mesmos auxiliares de gestão monetária: take-profit de dinheiro fixo, take-profit percentual, bloqueio de trailing de capital, break-even e trailing stops em pips.
- Usa uma subscrição de vela de um minuto puramente como sinal de funcionamento para avaliar as regras de gestão em barras finalizadas.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `OrderLots` | Tamanho de lote base usado quando uma entrada manual é solicitada. Espelha a entrada extern `Lots` (`0.01` por padrão). |
| `NumberOfTrades` | Número de tickets despachados por solicitação. O porto C# consolida o volume em uma única ordem a mercado. |
| `UseTakeProfitInMoney` / `TakeProfitInMoney` | Habilitar e configurar o alvo monetário direto que fecha todas as operações ao ser atingido. |
| `UseTakeProfitPercent` / `TakeProfitPercent` | Habilitar e configurar o alvo de porcentagem de capital. A estratégia usa `Portfolio.CurrentValue` para aproximar o saldo da conta. |
| `EnableTrailing`, `TrailingProfitMoney`, `TrailingLossMoney` | Configurar o bloco de trailing de capital: assim que o lucro excede `TrailingProfitMoney`, o pico é rastreado e todas as operações fecham se o lucro retroceder `TrailingLossMoney`. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Mover o stop para break-even mais offset após a posição ganhar a distância de pips configurada. |
| `StopLossPips`, `TakeProfitPips`, `TrailingStopPips` | Configurações de gestão de tickets convertidas para distâncias em pips no StockSharp. |
| `CandleType` | Série de velas que impulsiona o sinal de funcionamento (padrão de velas de um minuto). |
| `BuyRequest`, `SellRequest`, `CloseRequest` | Comandos manuais que substituem os botões originais do gráfico. Os sinalizadores são redefinidos automaticamente após a ação ser bem-sucedida. |

## Lógica de trading
1. `OnStarted` subscreve a série de velas configurada, define o `Volume` base e habilita a proteção de posição integrada.
2. Cada vela finalizada aciona o seguinte fluxo de trabalho:
   - Comandos manuais são avaliados: compra e venda enviam uma ordem a mercado com volume `OrderLots * NumberOfTrades`, opcionalmente compensando uma posição oposta; solicitações de fechamento achatam a estratégia.
   - Alvos monetários são verificados em ordem: valor fixo, percentual de capital, depois o bloqueio de trailing de capital.
   - Stops de break-even e pip trailing ajustam os níveis de stop internos com base no preço de entrada médio.
   - Distâncias estáticas de stop-loss/take-profit são aplicadas.
   - A saída opcional de Bandas de Bollinger fecha posições compradas que tocam a banda superior ou posições vendidas que tocam a banda inferior (20 períodos, largura 2).
3. O lucro aberto é calculado com `Security.PriceStep`/`Security.StepPrice` quando disponível; caso contrário, um fallback de diferença de preço é usado.

## Diferenças da versão MQL
- MetaTrader permitia posições com hedge; StockSharp consolida a exposição, então solicitações de compra/venda primeiro neutralizam posições opostas.
- Saídas baseadas em MACD mensal (`Close_BUY`/`Close_SELL`) não estão presentes porque nunca foram chamadas no script original.
- O dimensionamento automático de volume via `MaximumRisk`/`DecreaseFactor` é substituído pelo parâmetro explícito `OrderLots`. O auxiliar MQL dependia do histórico de conta que não está disponível neste porto.
- Ajustes de stop são conduzidos por velas finalizadas em vez de ticks brutos, seguindo as diretrizes do repositório.
- Valores de indicadores são processados através de `Bind`, evitando coleções diretas ou buffers de histórico manuais.

## Notas de uso
- Manter `BuyRequest`, `SellRequest` e `CloseRequest` sob o grupo "Controles manuais" desabilitados ao executar otimizações.
- A lógica de trailing de capital e take-profit monetário requerem `Security.StepPrice` para traduzir o lucro em moeda. Quando indisponível, o fallback usa diferenças de preço puras.
- Break-even e trailing stops usam o tamanho de pip do instrumento inferido de `MinPriceStep`/`PriceStep` e dígitos decimais.
- Não há tradução para Python, conforme solicitado.

## Testes
- Nenhum teste automatizado foi modificado; a estratégia se integra com a estrutura de solução existente e depende de alternância manual de parâmetros para verificação.
