# Estratégia SuperForexV2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
SuperForexV2 é uma porta StockSharp do MetaTrader 4 consultor especialista `SuperForexV2.mq4`. O roteiro original combina uma visão de curto prazo
Oscilador de Índice de Força Relativa (RSI) com distâncias fixas de take-profit, stop-loss e trailing stop. A implementação C#
reconstrói o mesmo processo de decisão com o StockSharp API de alto nível: a estratégia observa velas finalizadas, reage a RSI
cruzamentos de limites e gerencia uma única posição líquida usando limites de risco baseados em pip.

## Lógica de negociação
1. **Pipeline de indicadores**
   - Assina a série de velas configuráveis (barras de 15 minutos por padrão) e alimenta cada barra finalizada em um indicador RSI.
   - O comprimento RSI é configurável e o padrão é o valor MT4 original de 4.
2. **Dimensionamento de posição dinâmico**
   - Antes de cada entrada, a estratégia deriva um tamanho de lote de trabalho do valor atual do portfólio dividido por `BalanceToVolumeDivider`.
   - O volume resultante é limitado por `InitialVolume` (substituição quando o saldo é desconhecido) e `MaxVolume`, depois arredondado para o
passo de volume do instrumento.
3. **Regras de entrada**
   - Quando não há posição aberta e RSI cai abaixo de `RsiLowerLevel`, uma ordem de compra de mercado é colocada.
   - Quando RSI ultrapassa `RsiUpperLevel`, uma ordem de venda a mercado é enviada.
4. **Saída e gerenciamento de riscos**
   - Cada posição armazena níveis absolutos de stop-loss e take-profit calculados a partir das distâncias baseadas em pip.
   - A cada vela finalizada, a estratégia verifica se a barra tocou esses níveis; em caso afirmativo, fecha a posição no mercado.
   - Um trailing stop imita a lógica MT4: uma vez que o preço avançou pelo menos `TrailingStopPips`, o stop é aproximado para que o
o lucro atual está bloqueado.
   - As posições também são fechadas sempre que RSI cruza para o extremo oposto (por exemplo, as posições compradas saem quando RSI excede o nível superior).
5. **Escopo de posição**
   - O bot reflete o comportamento de “uma negociação por símbolo” do EA, aplicando um livro plano antes de avaliar novas entradas.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `CandleType` | Série de velas que conduz os cálculos do indicador. | `15m` período de tempo | Aceita qualquer `DataType` compatível com o conector. |
| `RsiPeriod` | RSI comprimento de lookback. | `4` | Deve ser maior que zero. |
| `RsiUpperLevel` | Limite de sobrecompra usado para saídas curtas e longas. | `62` | Corresponde à entrada MT4 `Pos`. |
| `RsiLowerLevel` | Limite de sobrevenda usado para saídas longas e curtas. | `42` | Corresponde à entrada MT4 `Neg`. |
| `TakeProfitPips` | Distância de lucro expressa em pips. | `109` | Defina como `0` para desativar o take-profit. |
| `StopLossPips` | Distância de stop-loss expressa em pips. | `9` | Defina como `0` para desativar o stop loss. |
| `TrailingStopPips` | Distância do trailing stop expressa em pips. | `6` | Defina como `0` para desativar o comportamento de rastreamento. |
| `InitialVolume` | Tamanho do lote substituto quando o saldo do portfólio não está disponível. | `0.1` | Também usado se o dimensionamento dinâmico produzir um valor não positivo. |
| `MaxVolume` | Volume máximo permitido por entrada. | `100` | Impede que o dimensionamento baseado em equilíbrio seja superdimensionado. |
| `BalanceToVolumeDivider` | Divisor aplicado ao saldo da conta para calcular o volume. | `10000` | Replica a fórmula MT4 `Lots = AccountBalance()/10000`. |

## Notas de implementação
- O processamento de velas acontece somente após `CandleStates.Finished` para espelhar o comportamento de final de tick do MT4 `start()`, evitando
dados incompletos.
- As distâncias pip são convertidas em preços absolutos usando o `PriceStep` do instrumento. Para símbolos Forex de 3 e 5 dígitos, o código
multiplica o passo por dez para que o StockSharp “pip” corresponda à definição do ponto MetaTrader.
- Os níveis de stop-loss, take-profit e trailing são armazenados internamente e verificados em relação aos máximos e mínimos das velas, porque StockSharp
não gerencia automaticamente paradas no nível do pedido no estilo MT4.
- A estratégia arredonda o volume calculado para o lote válido mais próximo, respeitando `MinVolume`, `MaxVolume` e `VolumeStep`
limites expostos pelo título.
- Apenas é permitida uma posição líquida por vez; a lógica de entrada sai mais cedo se a estratégia já estiver comprada ou vendida.

## Diferenças em comparação com a versão MT4
- A porta StockSharp funciona em velas finalizadas em vez de ticks individuais, então a parada intrabarra ou acertos no alvo são detectados no
próxima barra fecha.
- A proteção `AccountFreeMargin()` de MetaTrader foi substituída por um volume derivado de saldo mais seguro; se o conector não puder fornecer o
valor do portfólio, o substituto `InitialVolume` é usado em vez de anular.
- Os valores de stop-loss e take-profit da ordem não são enviados à corretora. Em vez disso, a estratégia fecha posições no mercado assim que um nível
é violado porque pedidos StockSharp de alto nível dependem de saídas gerenciadas por estratégia.
- A entrada `NumeroMagico` usada para filtrar pedidos MT4 é desnecessária em StockSharp e foi omitida.
- As mensagens de registro do EA original não são reproduzidas; Os próprios recursos de registro de StockSharp devem ser usados se for necessário
é necessária instrumentação.
