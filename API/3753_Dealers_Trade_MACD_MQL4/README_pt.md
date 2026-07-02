# Estratégia de comércio de revendedores MACD MQL4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Dealers Trade MACD MQL4 é uma conversão direta do consultor especialista "Dealers Trade v7.74" para MetaTrader 4. Ela mantém o gerenciamento de dinheiro em pirâmide e a lógica de inclinação MACD do sistema original enquanto adapta o tratamento de posição às contas líquidas de StockSharp. A estratégia foi projetada para negociação de swing em gráficos H4/D1 e aumenta continuamente a tendência, desde que o impulso permaneça alinhado com a linha principal MACD.

## Como funciona a estratégia

- **Detecção de sinal** – a estratégia assina velas do período de tempo configurado e calcula um indicador MACD clássico (EMA rápida, EMA lenta e sinal EMA). Um valor principal ascendente MACD em comparação com a barra anterior sinaliza um impulso de alta, enquanto um valor em queda sinaliza um impulso de baixa. O parâmetro `ReverseCondition` pode ser usado para mudar a direção quando uma abordagem contrária for preferida.
- **Espaçamento e escala de pedidos** – apenas uma cesta direcional está ativa por vez. Quando o MACD indica uma tendência longa, a estratégia abre uma ordem inicial de compra de mercado. Compras adicionais são enviadas somente quando o preço caiu pelo menos `SpacingPips * PriceStep` em relação ao último preço de entrada, refletindo o comportamento de "média" do script MQL. As cestas curtas se comportam simetricamente quando a inclinação MACD se torna negativa.
- **Dimensionamento do lote** – o tamanho do lote base é o `FixedVolume` fixo ou, se `UseRiskSizing` estiver ativado, um valor derivado do patrimônio do portfólio e `RiskPercent`. Mini contas são suportadas por meio do sinalizador `IsStandardAccount` que emula a opção original "Conta normal". Cada pedido extra dentro da mesma cesta é multiplicado por `LotMultiplier` e limitado por `MaxVolume`.
- **Controles de risco** – os níveis de hard stop loss e takeprofit são anexados a cada posição usando as distâncias `StopLossPips` e `TakeProfitPips`. Depois que uma negociação avança `TrailingStopPips + SpacingPips` de lucro, o nível de stop é reduzido para manter pelo menos `TrailingStopPips` de lucro, reproduzindo a regra final da implementação MetaTrader.
- **Proteção da conta** – quando o número de negociações abertas atinge `MaxTrades - OrdersToProtect` e o lucro não realizado agregado excede `SecureProfit`, a negociação mais recente é fechada para garantir ganhos antes que novos pedidos sejam considerados. Isso corresponde ao bloco "AccountProtection" na fonte EA.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | H4 | Período usado para cálculos MACD e avaliação de sinal. |
| `FixedVolume` | 0,1 | Tamanho base do lote quando `UseRiskSizing` está desativado. |
| `UseRiskSizing` | verdade | Ativa o dimensionamento da posição com base no equilíbrio. |
| `RiskPercent` | 2 | Porcentagem de patrimônio usado para dimensionar posições quando `UseRiskSizing` for verdadeiro. |
| `IsStandardAccount` | verdade | Defina como falso para contas mini (lotes divididos por 10). |
| `MaxVolume` | 5 | Volume máximo permitido para um único pedido. |
| `LotMultiplier` | 1,5 | Multiplicador aplicado ao lote base para cada entrada adicional na cesta. |
| `MaxTrades` | 5 | Número máximo de negociações abertas simultaneamente. |
| `SpacingPips` | 4 | Distância mínima de pip entre entradas consecutivas. |
| `OrdersToProtect` | 3 | Número de ordens mantidas antes que o bloco de proteção possa abrir novas negociações. |
| `AccountProtection` | verdade | Ativa a lógica segura de proteção de lucros. |
| `SecureProfit` | 50 | Lucro não realizado (na moeda da conta) necessário para acionar a proteção. |
| `TakeProfitPips` | 30 | Distância de lucro por negociação, expressa em pips. |
| `StopLossPips` | 90 | Distância de stop loss por negociação, expressa em pips. |
| `TrailingStopPips` | 15 | Distância de parada móvel aplicada após a ativação. |
| `ReverseCondition` | falso | Inverte a interpretação da inclinação MACD. |
| `MacdFast` | 14 | Comprimento EMA rápido para o indicador MACD. |
| `MacdSlow` | 26 | Comprimento EMA lento para o indicador MACD. |
| `MacdSignal` | 1 | Comprimento do sinal EMA para o indicador MACD. |

## Notas e limitações

- As estratégias StockSharp gerenciam uma posição líquida por título, portanto, cestas longas e curtas cobertas não podem coexistir. O EA original permitia o hedge, mas a conversão fecha o lado oposto antes de mudar de direção.
- A lógica de lucro seguro calcula o lucro não realizado usando os metadados do instrumento `PriceStep` e `StepPrice`. Os instrumentos sem esta informação recorrem a um valor nominal de pip de 0,0001 com um passo de moeda unitária, portanto ajuste os limites adequadamente.
- O dimensionamento baseado em risco requer um valor `StopLossPips` positivo. Quando a distância de parada é zero, o valor do risco calculado torna-se indefinido e a estratégia irá pular a negociação.
- A estratégia funciona apenas em velas fechadas. Os sinais que dependiam de movimentos intrabarra MACD em MetaTrader podem aparecer em uma barra posteriormente nesta implementação, mas o comportamento é significativamente mais estável para backtesting.
