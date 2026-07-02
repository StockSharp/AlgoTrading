# Pedidos de dias e horários específicos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o MetaTrader especialista *"Dia e hora específicos do Expert Advisor"*.
Ele coloca ordens de compra e/ou venda em um carimbo de data/hora programado e, opcionalmente, remove todas as exposições em outro carimbo de data/hora.
A versão StockSharp mantém o comportamento original de gerenciamento de risco, incluindo trailing stops opcionais e movimentos de equilíbrio.

## Lógica central

1. **Agendamento**
   - `OpenTime` – momento em que os pedidos são criados.
   - `CloseTime` – momento em que as posições são achatadas e as ordens pendentes podem ser removidas.
Ambas as verificações aceitam uma janela de um minuto, correspondendo à comparação `TimeToString(..., TIME_MINUTES)` usada no código MT4.

2. **Colocação de pedido**
   - `OrderPlacement` escolhe entre ordens de mercado, stop ou limite.
   - `OpenBuyOrders` / `OpenSellOrders` habilite as rotas desejadas.
   - `OrderDistancePoints` compensa o preço das ordens pendentes em vários pontos (pips).
   - `PendingExpireMinutes` cancela pedidos pendentes automaticamente quando o período de validade termina.

3. **Gerenciamento de volumes**
   - `LotSizing = Manual` envia o `ManualVolume` fixo.
   - `LotSizing = Automatic` calcula o volume a partir do valor atual do portfólio e do tamanho do contrato do instrumento:
`volume = (portfolio / contractSize) * RiskFactor`.
O resultado é alinhado a `Security.VolumeStep` e fixado entre `MinVolume`/`MaxVolume` quando disponível.

4. **Lógica de proteção**
   - `StopLossPoints` e `TakeProfitPoints` traduzem as distâncias originais baseadas em pontos em preços absolutos usando o tamanho do pip do instrumento.
   - `TrailingStopEnabled` + `TrailingStepPoints` e `BreakEvenEnabled` movem o stop de proteção exatamente como o script MQL, usando atualizações de oferta/venda como acionadores.
   - Quando as condições de stop-loss ou take-profit são atingidas, a posição é fechada com uma ordem de mercado, refletindo o comportamento do MT4 de modificar os stops para um novo preço.

5. **Fase de encerramento**
   - Quando `CloseOwnOrders` ou `CloseAllOrders` está ativado, a estratégia sai de qualquer posição aberta na janela de fechamento.
   - `DeletePendingOrders` remove todos os pedidos pendentes restantes ao mesmo tempo.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `OpenTime`, `CloseTime` | Carimbos de data e hora UTC para entrada e saída do mercado. |
| `OrderPlacement` | Comercialize, pare ou limite a colocação de pedidos. |
| `OpenBuyOrders`, `OpenSellOrders` | Instruções para ativar. |
| `TakeProfitPoints`, `StopLossPoints` | Distâncias de proteção expressas em pontos (0 desabilita). |
| `TrailingStopEnabled`, `TrailingStepPoints` | Habilite o trailing stop e defina o avanço mínimo antes de movê-lo. |
| `BreakEvenEnabled`, `BreakEvenAfterPoints` | Mude o stop para o ponto de equilíbrio quando o lucro exceder o limite. |
| `OrderDistancePoints` | Offset usado para pedidos pendentes. |
| `PendingExpireMinutes` | Janela de expiração para pedidos pendentes. |
| `LotSizing` | Dimensionamento de volume manual ou automático. |
| `RiskFactor`, `ManualVolume` | Entradas para os modos de dimensionamento. |
| `CloseOwnOrders`, `CloseAllOrders`, `DeletePendingOrders` | Controle como as posições e ordens pendentes são fechadas no final. |

## Notas

- A classe reside no namespace `StockSharp.Samples.Strategies` com recuo de tabulação conforme exigido pelas diretrizes do projeto.
- Os dados de nível 1 são usados para reproduzir a lógica sensível à oferta/venda da versão MQL (trailing stop, colocação de ordem pendente).
- As configurações de `MagicNumber` do MT4 não são necessárias porque StockSharp já isola ordens de estratégia.

## Uso

1. Compile o projeto via `AlgoTrading.sln` e anexe a estratégia a um par segurança/portfólio.
2. Ajuste o cronograma, o tipo de pedido e os parâmetros de risco conforme necessário.
3. Inicie a estratégia antes de `OpenTime`; os pedidos serão enviados automaticamente assim que a janela começar.
4. Mantenha a estratégia em execução até depois de `CloseTime` se desejar que a etapa de nivelamento automático seja acionada.
