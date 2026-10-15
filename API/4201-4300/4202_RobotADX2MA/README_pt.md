# Robô ADX + 2 Estratégia MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Robot ADX + 2 MA é uma versão StockSharp do MetaTrader especialista `Robot_ADX+2MA`. O sistema combina um rápido e um lento
média móvel exponencial com os componentes +DI/-DI do Índice Direcional Médio (ADX). Os pedidos só são abertos quando o
a vela anterior mostra uma separação EMA suficientemente ampla e a vela atual confirma o impulso através do índice direcional. O
conversão mantém o comportamento original de abrir no máximo uma posição de mercado por vez e delegar saídas para stop-loss e
proteções de lucro.

## Lógica de negociação
1. Assine a série de velas primárias configurada por meio de `CandleType` e processe apenas velas concluídas.
2. Alimente duas médias móveis exponenciais (períodos 5 e 12) com os preços de fechamento das velas. Seus valores da vela anterior
emular o lookback `shift = 1` usado em MetaTrader.
3. Alimente um indicador `AverageDirectionalIndex` (período 6) com as mesmas velas. Armazene o +DI/-DI atual e o anterior
leituras para replicar os filtros EA.
4. Calcule a distância absoluta EMA da vela anterior e compare-a com `DifferenceThreshold` convertida de pontos em
unidades de preço (`Point` em MetaTrader é igual a `Security.PriceStep` em StockSharp).
5. **Entrada de alta**: permitida somente se nenhuma posição estiver aberta e as seguintes condições forem atendidas:
   - O rápido anterior EMA está abaixo do lento anterior EMA.
   - O +DI anterior está abaixo de 5, o +DI atual está acima de 10 e o +DI é mais forte que o -DI.
   - A distância EMA está acima do limite configurado.
6. **Entrada de baixa**: simétrica às regras longas, exigindo o rápido anterior EMA acima do EMA lenta, os filtros -DI devem ser
satisfeito e -DI para dominar +DI.
7. Quando uma negociação for aberta, conte com o módulo de risco iniciado por `StartProtection` para sair por meio de take-profit ou stop loss. Sem manual
regras de saída são adicionadas, correspondendo ao especialista original.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Série de velas primárias processadas pela estratégia. |
| `TakeProfitPoints` | `int` | `4700` | Distância da meta de lucro expressa em etapas de preço. Defina como zero para desativar. |
| `StopLossPoints` | `int` | `2400` | Distância da meta de stop-loss em etapas de preço. Defina como zero para desativar. |
| `TradeVolume` | `decimal` | `0.1` | Volume líquido utilizado para cada ordem de mercado. |
| `DifferenceThreshold` | `int` | `10` | Distância mínima de EMA (em etapas de preço) necessária antes que um sinal seja aceito. |

## Gestão de risco
- A versão StockSharp chama `StartProtection` com `UnitTypes.Step`, portanto, as distâncias de stop-loss e take-profit configuradas são
convertido automaticamente para a etapa de preço da corretora.
- As ordens de proteção são geradas como saídas de mercado (`useMarketOrders = true`), replicando o comportamento de fechamento imediato do
MQL função auxiliar.

## Detalhes de implementação
- As vinculações de indicadores usam `SubscribeCandles(...).Bind(...).BindEx(...)` API de alto nível, portanto, nenhum loop manual de dados é necessário.
- Os valores EMA da vela anterior são armazenados em cache para reproduzir as chamadas `iMA(..., shift = 1)` no EA original.
- Os dados de ADX são consumidos por meio de `AverageDirectionalIndexValue`, dando acesso direto aos componentes +DI e -DI sem chamar
ajudantes `GetValue` proibidos.
- Uma proteção por vela (`_lastProcessedTime`) garante que os sinais sejam avaliados apenas uma vez, mesmo que as ligações EMA e ADX sejam acionadas
retornos de chamada para a mesma vela.

## Diferenças do especialista MetaTrader
- A chamada direta redundante `OrderSend` presente na ramificação de venda do código MQL é removida; ambas as direções usam um único
Ajudante `BuyMarket`/`SellMarket`.
- MetaTrader verifica a margem livre antes de enviar pedidos. A porta StockSharp delega controles de risco ao ambiente de hospedagem e
assume equilíbrio suficiente.
- A lógica de proteção é implementada por meio do gerenciador de risco do StockSharp em vez de loops personalizados que chamam `OrderSend` repetidamente.

## Dicas de uso
- Ajuste `TradeVolume` para respeitar a etapa do lote do título selecionado antes de iniciar a negociação ao vivo.
- Se o mercado usar uma escala de preços diferente, ajuste `DifferenceThreshold` junto com as distâncias de stop/alvo para que o EMA
a separação é comparável à configuração MetaTrader.
- O período padrão é de um minuto, mas o parâmetro `CandleType` permite alternar para qualquer outra série suportada pelos dados
fonte.

## Indicadores
- `ExponentialMovingAverage(5)` calculado sobre preços de fechamento.
- `ExponentialMovingAverage(12)` calculado sobre preços de fechamento.
- `AverageDirectionalIndex(6)` fornecendo filtros de força +DI/-DI e ADX.
