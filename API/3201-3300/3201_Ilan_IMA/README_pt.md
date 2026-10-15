# Estratégia de Ilan iMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Ilan iMA** é um port do StockSharp do consultor especialista de MetaTrader 5 `Ilan iMA.mq5`. O consultor combina um filtro de tendência de média móvel deslocada com uma grade de média estilo martingale. A versão StockSharp reimplementa as mesmas ideias com a API de alto nível: quando a média móvel ponderada confirma uma tendência, a estratégia abre uma ordem de mercado e continua adicionando operações sempre que o preço se move contra a posição por um passo configurável. Todo o cesto é fechado quando um alvo de lucro, trailing stop ou stop-loss explícito é atingido, reproduzindo o modelo de gestão de dinheiro do EA original.

## Lógica de trading
1. Assinar o período selecionado (`CandleType`) e alimentar uma média móvel configurável (`MaMethod`, `MaPeriod`, `PriceMode`). Um `MaShift` positivo desloca o indicador para frente, de modo que a estratégia avalia valores históricos para imitar o comportamento do MT5.
2. Aguardar o fechamento da vela. Apenas barras finalizadas geram sinais e atualizam a lógica de trailing/stop.
3. Detectar a tendência comparando quatro valores consecutivos de média móvel deslocados por `MaShift` barras:
   - valores estritamente decrescentes sinalizam uma tendência de baixa;
   - valores estritamente crescentes sinalizam uma tendência de alta.
4. Quando nenhum cesto está aberto:
   - em tendência de baixa, se o fechamento estiver acima do valor da média móvel, abrir um vendido com `StartVolume`;
   - em tendência de alta, se o fechamento estiver abaixo do valor da média móvel, abrir um comprado com `StartVolume`.
5. Quando um cesto existe:
   - se o preço se mover contra a posição pelo menos `GridStepPips`, abrir outra ordem cujo tamanho cresce por `LotExponent`, mas é limitado por `LotMaximum` e os limites de volume da bolsa;
   - o preço médio de entrada, o menor preço de compra e o maior preço de venda são rastreados internamente para manter o comportamento próximo à lógica do MT5.
6. Condições de fechamento:
   - uma vez que o lucro flutuante de um cesto com mais de uma operação atinge `ProfitMinimum` (na moeda da conta), fechar todas as ordens nessa direção;
   - se o lucro flutuante atinge `TakeProfitPips` ou a perda chega a `StopLossPips`, fechar o cesto;
   - a proteção de trailing fica ativa após `TrailingStopPips + TrailingStepPips` pontos de movimento favorável e se move em passos de `TrailingStepPips`.

## Gestão de risco e dimensionamento
- `StartVolume` replica o parâmetro `StartLots` do MT5. Cada ordem adicional multiplica o tamanho anterior por `LotExponent` respeitando `LotMaximum` e os limites do local (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`).
- `ProfitMinimum` preserva o comportamento de "liberação de bloqueio" da versão MT5: uma vez que a grade se recuperou de uma cobertura e imprime o lucro solicitado, todas as operações nessa direção são fechadas.
- As distâncias de stop-loss e take-profit são medidas em pips (`StopLossPips`, `TakeProfitPips`). O método helper converte pips em passos de preço da bolsa usando `Security.PriceStep`.
- O bloco de trailing emula a implementação do MT5: o trailing começa apenas após o preço exceder `TrailingStopPips + TrailingStepPips` e é atualizado em passos discretos para evitar ajustes prematuros do stop.

## Parâmetros
| Nome | Tipo | Padrão | Contraparte MT5 | Descrição |
| --- | --- | --- | --- | --- |
| `MaPeriod` | `int` | `15` | `Inp_MA_ma_period` | Período da média móvel do filtro de tendência. |
| `MaShift` | `int` | `5` | `Inp_MA_ma_shift` | Deslocamento para frente da linha de média móvel em barras. |
| `MaMethod` | `MovingAverageMethod` | `Weighted` | `Inp_MA_ma_method` | Algoritmo de suavização (SMA, EMA, SMMA, LWMA). |
| `PriceMode` | `CandlePrice` | `Weighted` | `Inp_MA_applied_price` | Preço de vela alimentado ao indicador. |
| `StartVolume` | `decimal` | `1` | `InpStartLots` | Volume base da ordem para a primeira operação em um cesto. |
| `GridStepPips` | `decimal` | `30` | `InpStep` | Distância (em pips) entre entradas de média. |
| `LotExponent` | `decimal` | `1.6` | `InpLotExponent` | Multiplicador aplicado ao tamanho da ordem anterior. |
| `LotMaximum` | `decimal` | `15` | `InpLotMaximum` | Limite máximo para um único volume de ordem. |
| `ProfitMinimum` | `decimal` | `15` | `InpProfitMinimum` | Lucro flutuante mínimo necessário para fechar um cesto com várias operações. |
| `StopLossPips` | `decimal` | `0` | `InpStopLoss` | Distância do stop-loss em pips (0 desabilita o stop). |
| `TakeProfitPips` | `decimal` | `100` | `InpTakeProfit` | Distância do take-profit em pips. |
| `TrailingStopPips` | `decimal` | `15` | `InpTrailingStop` | Limiar de lucro que ativa o trailing stop. |
| `TrailingStepPips` | `decimal` | `5` | `InpTrailingStep` | Lucro adicional mínimo antes do trailing stop se mover novamente. |
| `CandleType` | `DataType` | Período de 15 minutos | período do gráfico | Período usado para o cálculo de sinais. |

## Diferenças do EA original
- O StockSharp funciona em um ambiente de netting, portanto, existe apenas uma posição líquida por direção. A estratégia mantém uma lista interna de preços de entrada e volumes para emular a contabilidade de cesto do MT5.
- Os limites de volume específicos da bolsa são sempre respeitados ao arredondar volumes, enquanto o código do MT5 dependia de verificações manuais. Isso evita ordens que seriam rejeitadas pelo conector do broker.
- A lógica de stop-loss, take-profit e trailing é expressa por meio de saídas de mercado em vez de modificar posições existentes do MT5. O comportamento funcional permanece o mesmo, mas o gerenciamento de ordens é tratado pelo StockSharp.

## Notas de uso
- Certifique-se de que os metadados do instrumento (`PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep`, `MaxVolume`) estão preenchidos no conector para que as conversões de pip para preço e o arredondamento de volume funcionem corretamente.
- O bloco de trailing assume que o tamanho do pip é igual ao passo de preço da bolsa. Ajuste `GridStepPips`, `StopLossPips` e `TrailingStopPips` para instrumentos com tamanhos de tick não convencionais.
- As grades de martingale são inerentemente arriscadas. Teste a estratégia em dados históricos e use configurações realistas de comissão/slippage antes de implantar em produção.
