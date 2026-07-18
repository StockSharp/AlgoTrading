# Estratégia Não Direcional RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia transporta o consultor especialista MetaTrader 4 "RRS não direcional" para a estrutura StockSharp. O EA original abre cestas de compra e venda protegidas dependendo do modo de negociação selecionado e as gerencia com regras virtuais de stop-loss, take-profit e trailing. A implementação StockSharp reproduz os modos configuráveis, desligamento de risco monetário e lógica de proteção virtual enquanto adapta o comportamento às carteiras de compensação usadas por StockSharp. Os modos baseados em hedge, portanto, alternam entre exposições longas e curtas, em vez de manter posições opostas simultâneas.

## Lógica de negociação
- Assine os dados do Nível 1 para ler os melhores preços de compra/venda. O spread informado por essas cotações é comparado com `MaxSpreadPoints` antes de cada decisão de entrada.
- As entradas no mercado respeitam o parâmetro `TradingMode`:
  - `HedgeStyle` e `AutoSwap` espelham o modo bilateral, alternando entre negociações longas e curtas (StockSharp não pode manter bilhetes independentes de compra e venda simultaneamente).
  - `BuySellRandom` joga uma moeda em cada nova oportunidade.
  - `BuySell` sempre abre o lado oposto da posição fechada mais recentemente.
  - `BuyOrder` e `SellOrder` restringem a negociação a uma única direção.
- O externo `New_Trade` é mapeado para `AllowNewTrades`, fornecendo uma maneira rápida de pausar todas as novas ordens de mercado.
- Cada pedido usa o `TradeVolume` configurado e anexa o `TradeComment` para facilitar o rastreamento do lado do corretor.

## Gestão de riscos e saídas
- As distâncias de stop-loss e take-profit são expressas em MetaTrader pontos. Eles são convertidos em unidades de preço usando o instrumento `PriceStep` para que a lógica permaneça independente do corretor.
- `StopMode`, `TakeMode` e `TrailingMode` selecionam entre gerenciamento desativado, virtual e clássico. Na porta StockSharp, ambos os modos não desabilitados são implementados como verificações virtuais que fecham a posição por meio de ordens de mercado quando o limite é atingido. Isso mantém o comportamento determinístico entre conectores.
- O gerenciamento de rastreamento é ativado após o preço avançar em `TrailingStartPoints` e, em seguida, mantém um stop dinâmico que acompanha o melhor preço em `TrailingGapPoints`.
- Lucros e perdas não realizados são recalculados em cada atualização do Nível 1. Quando cai abaixo do limite derivado de `RiskMode` e `MoneyInRisk`, a estratégia liquida a posição imediatamente.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradingMode` | Seleção de entrada copiada do EA original. Os modos de hedge alternam entre negociações longas e curtas no modelo de compensação de StockSharp. |
| `AllowNewTrades` | Ativa ou desativa novas ordens de mercado. |
| `TradeVolume` | Tamanho base para pedidos. |
| `StopMode` | Tratamento de stop-loss (`Disabled`, `Virtual`, `Classic`). |
| `StopLossPoints` | Distância de stop-loss em MetaTrader pontos. |
| `TakeMode` | Tratamento de lucros (`Disabled`, `Virtual`, `Classic`). |
| `TakeProfitPoints` | Distância de lucro em MetaTrader pontos. |
| `TrailingMode` | Gerenciamento de trailing stop (`Disabled`, `Virtual`, `Classic`). |
| `TrailingStartPoints` | Lucro (pontos) necessário antes dos braços do trailing stop. |
| `TrailingGapPoints` | Distância (pontos) mantida atrás do melhor preço quando o trailing estiver ativo. |
| `RiskMode` | Interpreta `MoneyInRisk` como uma porcentagem do saldo ou como um valor monetário absoluto. |
| `MoneyInRisk` | Montante ou percentagem de risco que desencadeia uma liquidação total quando o lucro e prejuízo flutuante cai abaixo do limite. |
| `MaxSpreadPoints` | Spread máximo (pontos) permitido para novas negociações. |
| `SlippagePoints` | Configuração de deslizamento informativo mantida para paridade com as entradas originais. |
| `TradeComment` | Comentário anexado a cada pedido. |

## Notas e limitações
- AutoSwap depende de informações de taxa de swap em MetaTrader. Os conectores StockSharp geralmente não expõem esses números por meio de feeds de nível 1, portanto, o modo volta para `HedgeStyle` e registra o downgrade.
- As opções clássicas de stop-loss, take-profit e trailing são executadas virtualmente. Os corretores que exigem ordens de proteção nativas devem ser tratados com substituições de estratégia de nível inferior.
- Como StockSharp agrega posições por título, a estratégia alterna a exposição nos modos de hedge em vez de manter dois tickets simultâneos. Esse comportamento é documentado para que os testes futuros correspondam às expectativas.
