# Estratégia MTrendLine Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia MTrendLine** transporta o script MetaTrader `MTrendLine.mq4` para a estratégia de alto nível de StockSharp API. O original
consultor especialista ajusta repetidamente o preço das ordens pendentes existentes para que permaneçam alinhadas com uma linha de tendência desenhada no
gráfico. A versão StockSharp automatiza o mesmo comportamento reconstruindo a linha de tendência móvel com um valor configurável
Indicador `LinearRegression`. Até três slots de ordens pendentes independentes podem seguir a linha de regressão calculada com seus
próprio tipo de pedido, distância e volume. Cada vez que uma nova vela fecha, a estratégia recalcula o valor da linha, avalia o
compensações necessárias e atualiza os pedidos pendentes de acordo.

A porta adiciona melhorias modernas de risco e usabilidade, como parâmetros estruturados, conversão automática de MetaTrader pontos
em etapas de preços reais e distâncias opcionais de stop-loss/take-profit que se movem junto com as ordens pendentes. Oferta/pedida
as atualizações são monitoradas via `SubscribeLevel1()` para que a estratégia respeite a distância mínima que os corretores exigem entre o
preço de mercado atual e ordens restantes.

## Lógica de negociação
1. Assine a série de velas configurada por meio de `SubscribeCandles()` e alimente um indicador `LinearRegression` com cada
barra acabada. O indicador representa a linha de tendência manual da versão MetaTrader.
2. Mantenha assinaturas de Nível 1 para armazenar em cache os melhores valores de lance e de venda mais recentes. Eles são usados para impor o mínimo
parâmetro de distância antes de realocar uma ordem pendente.
3. Para cada slot habilitado calcule o preço desejado como **valor de regressão + distância × tamanho do ponto**. The point size defaults to
a etapa do preço do título, mas pode ser substituída para corresponder à constante `Point` de MetaTrader.
4. Converta a configuração do slot em StockSharp auxiliares de pedido (`BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`). Opcional
os preços de stop-loss e take-profit são derivados da distância solicitada em pontos para que rastreiem a ordem após cada movimento.
5. Se já existir uma ordem pendente ativa para o slot e o novo preço-alvo for diferente, cancele primeiro a ordem atual e
espere a próxima vela para colocar a atualizada. Isso reflete o comportamento de `OrderModify` do código MQL sem
arriscando solicitações duplicadas.
6. Quando um slot for desativado ou o preço calculado se tornar inválido (por exemplo, negativo), cancele a ordem pendente associada
e limpe seu estado de cache.

## Espaços de pedidos pendentes
Cada slot emula uma chamada para `modify()` no EA original. Slots can be configured independently:
- **Tipo** — escolha entre Buy Limit, Buy Stop, Sell Limit ou Sell Stop.
- **Distância** — distância em MetaTrader pontos adicionados ao valor da regressão para obter o novo preço. Use valores negativos para
posicionar ordens abaixo da linha de regressão.
- **Volume** — tamanho da ordem pendente. Se definido como zero ou negativo, a estratégia volta ao `TradeVolume` global.
- **Habilitar sinalizador** — permite desabilitar um slot sem remover sua configuração. Slots desativados cancelam automaticamente qualquer ativo
ordens que lhes pertencem.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Velas de 1 hora | Período primário usado para construir a linha de tendência de regressão. |
| `RegressionLength` | `int` | `24` | Número de velas concluídas alimentadas no indicador `LinearRegression`. |
| `PointValue` | `decimal` | `0` | Valor monetário de um MetaTrader ponto. Quando zero, a estratégia usa a etapa do preço do título. |
| `TradeVolume` | `decimal` | `1` | Volume padrão usado por todos os slots quando seu próprio volume é zero. |
| `StopLossPoints` | `decimal` | `0` | Distância de stop-loss em pontos. Defina como zero para desativar a colocação automática de stop loss. |
| `TakeProfitPoints` | `decimal` | `0` | Distância de lucro em pontos. Defina como zero para desativar a colocação automática de lucro. |
| `MinDistancePoints` | `decimal` | `0` | Gap mínimo (em pontos) que deve existir entre o melhor bid/ask e a ordem pendente. |
| `PendingOrder{1,2,3}Enabled` | `bool` | Slot specific | Habilita ou desabilita o slot determinado. |
| `PendingOrder{1,2,3}Mode` | `enum` | Slot specific | Tipo de pedido pendente: BuyLimit, BuyStop, SellLimit ou SellStop. |
| `PendingOrder{1,2,3}DistancePoints` | `decimal` | Slot specific | Distância (em pontos) adicionada ao valor da regressão para calcular o preço do pedido. |
| `PendingOrder{1,2,3}Volume` | `decimal` | Slot specific | Volume para o slot. Zero reutilizações `TradeVolume`. |

## Diferenças em relação ao script MetaTrader original
- MetaTrader modifica pedidos existentes. StockSharp usa semântica de cancelar e substituir enquanto aguarda confirmação
antes de registrar o pedido de substituição na próxima vela.
- O código original lê o valor de uma linha de tendência desenhada manualmente. A porta substitui isso por um automático
Indicador `LinearRegression` para que o comportamento seja determinístico e possa ser executado sem supervisão.
- `MODE_STOPLEVEL` não está disponível em StockSharp. Em vez disso, a estratégia fornece o configurável `MinDistancePoints`
parâmetro e o aplica usando atualizações de compra/venda em tempo real.
- As distâncias de stop-loss e take-profit são parâmetros opcionais em vez de ler as configurações de pedidos existentes. Isso mantém os valores
consistente em todos os novos registros de pedidos.

## Dicas de uso
- Defina `PointValue` para corresponder à definição de ponto do corretor se for diferente do título `PriceStep`; isso garante o
parâmetros de distância espelham seus equivalentes MetaTrader.
- Ative apenas os slots necessários. Cada slot mantém sua própria ordem pendente e comentário (`"MTrendLine slot N"`), identificando assim
em relatórios ou no Registro de pedidos é simples.
- Considere combinar a estratégia com os auxiliares de proteção de risco integrados do StockSharp se você precisar de trailing stops ou conta
controles de nível. A implementação se concentra em espelhar a lógica original de modificação de pedido.

## Indicadores
- `LinearRegression` aplicado a velas acabadas.
