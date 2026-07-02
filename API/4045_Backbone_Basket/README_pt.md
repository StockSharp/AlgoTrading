# Estratégia de cesta de backbone (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Backbone Basket Strategy** transporta o consultor especialista original MetaTrader 4 "Backbone.mq4" para o StockSharp API de alto nível. O sistema coleta extremos de compra/venda para determinar a direção inicial da negociação e depois alterna entre cestas longas e curtas. Cada cesta é construída gradualmente, adicionando uma ordem de mercado por vela concluída até que a contagem configurada de `MaxTrades` seja atingida ou as ordens de proteção fechem a posição. O controle de risco é mantido por meio de um modelo de risco fracionário que dimensiona o volume de negociação pelo valor da conta e pela distância do stop-loss.

## Fluxo de dados de mercado
- **Velas (`CandleType`)** – velas concluídas acompanham a tomada de decisões; apenas uma ordem pode ser emitida por barra finalizada, exatamente como no script MT4.
- **Instantâneos da carteira de pedidos** – os melhores valores de lance e venda são rastreados para reproduzir cálculos de trailing-stop e a lógica inicial de descoberta "extrema".
- **Estado da estratégia** – a base StockSharp `Strategy` mantém a posição atual, o preço médio de entrada e o PnL usado para gerenciar ordens de proteção.

## Lógica de negociação
1. **Calibração inicial** – embora nenhuma direção seja definida, a estratégia registra o lance mais alto e o pedido mais baixo vistos. Quando o preço retrocede `TrailingStopPoints * PriceStep` a partir desses extremos, a primeira direção da cesta é escolhida.
2. **Sequenciamento de pedidos** –
   - Se a última negociação concluída foi curta (`_lastPositionDirection == -1`) e não há negociações abertas, uma nova ordem de **mercado de compra** será enviada.
   - Se a negociação anterior foi longa (`_lastPositionDirection == 1`) e a cesta ainda tiver capacidade, serão enviadas ordens de compra adicionais nas velas subsequentes.
   - Regras simétricas se aplicam a ordens de venda quando a última negociação foi longa.
3. **Dimensionamento de volume** – cada novo pedido chama o análogo `Vol()` inspirado no MT4. O valor da conta disponível (valor atual → saldo → saldo inicial) é multiplicado por `MaxRisk` e dividido pela distância stop-loss convertida em dinheiro usando `PriceStepCost`. O resultado é alinhado com `VolumeStep`, limitado por `MinVolume`/`MaxVolume` e rejeitado se cair abaixo do tamanho mínimo de negociação.
4. **Ordens de proteção** – assim que uma negociação é executada, a estratégia coloca uma única ordem de stop-loss e take-profit que cobre toda a cesta. As distâncias são expressas em "pontos" (etapas de preço), assim como na versão MQL.
5. **Trailing stop** – quando `StopLossPoints` e `TrailingStopPoints` são positivos, a ordem de stop é reemitida para garantir lucros sempre que o preço se move mais do que a distância final além do preço de entrada registrado. As cestas longas utilizam como referência o melhor lance; cestas curtas usam o melhor pedido.
6. **Conclusão da cesta** – se a ordem de stop-loss ou de take-profit for executada, todos os contadores internos serão reiniciados, deixando `LastPosition` inalterado, de modo que a próxima vela inicia uma cesta na direção oposta, refletindo o comportamento original de EA.

## Gestão de capital
- Usa a mesma fórmula fracionária `1 / (MaxTrades / MaxRisk - openTrades)` do especialista MQL.
- O capital de risco é estimado a partir de `Portfolio.CurrentValue`, recorrendo a `CurrentBalance` ou `BeginBalance`.
- O volume é descartado se o tamanho calculado estiver abaixo do `MinVolume` do instrumento após o alinhamento com `VolumeStep`.
- As ordens stop-loss e take-profit são recriadas sempre que o volume muda, para que a proteção sempre cubra toda a cesta.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Período de 15 minutos | Intervalo de vela usado para desencadear novas decisões. |
| `MaxRisk` | 0,5 | Fração da carteira considerada no dimensionamento do próximo pedido. Deve ser positivo. |
| `MaxTrades` | 10 | Número máximo de negociações que podem ser acumuladas na cesta atual. |
| `TakeProfitPoints` | 170 | Distância de lucro medida em etapas de preço. Defina como `0` para desativar. |
| `StopLossPoints` | 40 | Distância de stop-loss medida em etapas de preço. Necessário para dimensionamento de posição e rastreamento. |
| `TrailingStopPoints` | 300 | Distância do trailing-stop em etapas de preço. Defina como `0` para manter uma parada estática. |

## Notas de conversão
- O EA original modifica cada pedido individualmente; a versão StockSharp gerencia um stop-loss e um take-profit agregados por cesta porque as posições StockSharp são compensadas por padrão.
- O dimensionamento do volume depende de `Security.PriceStepCost`. Se o conector não fornecer esse valor, a estratégia retornará à propriedade `Volume` configurada.
- As atualizações finais são aplicadas quando uma nova vela chega, correspondendo ao comportamento "uma vez por barra" do script MT4 (que só agiu quando `Bars > PrevBars`).
- A lógica alternada mantém a última direção executada em `_lastPositionDirection`, portanto, assim que uma cesta é fechada, a próxima vela abre automaticamente uma cesta na direção oposta, assim como o código-fonte.
- Somente a implementação C# é fornecida; não há porta Python neste diretório.

## Dicas de uso
- Atribua instrumentos com `PriceStep`, `PriceStepCost` precisos e metadados de volume para obter tamanhos de posição realistas.
- Ao fazer backtesting, certifique-se de que o feed do livro de pedidos esteja disponível para que a lógica do trailing stop possa acessar os melhores valores de compra/venda.
- Para desativar o escalonamento agressivo, aumente `MaxTrades` ou reduza `MaxRisk` para que a substituição de `Vol()` retorne volumes menores.
