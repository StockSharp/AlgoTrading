# Estratégia Cruzamento de Duas iMA v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria o expert advisor do MetaTrader "Crossing of two iMA v2" usando a API de alto nível do StockSharp. Duas médias móveis deslocadas geram sinais de cruzamento, opcionalmente filtrados por uma terceira média móvel. Stops protetores, dimensionamento de posição fixo ou baseado em porcentagem, e um trailing stop barra a barra emulam o comportamento do robô original enquanto mantêm a implementação compatível com as melhores práticas do StockSharp.

## Indicadores e entradas
- **Primeira Média Móvel** – período, deslocamento, método de suavização e preço aplicado configuráveis.
- **Segunda Média Móvel** – configuração independente com o mesmo conjunto de opções.
- **Filtro de Terceira Média Móvel** – filtro de tendência opcional que mantém operações compradas apenas quando a primeira MA está abaixo do filtro e operações vendidas quando a primeira MA está acima do filtro.
- **Tipo de Candle** – controla o período/série entregue pela subscrição de dados.

## Lógica de trading
### Passo 1 – Cruzamento imediato
1. Em cada candle terminado, a estratégia atualiza todas as médias móveis usando os preços aplicados selecionados.
2. Uma entrada **comprada** é acionada quando a primeira MA cruza **acima** da segunda MA entre a barra anterior e a atual.
3. Uma entrada **vendida** é acionada quando a primeira MA cruza **abaixo** da segunda MA entre a barra anterior e a atual.
4. Quando o filtro está habilitado, os sinais comprados requerem que a primeira MA se mantenha **abaixo** da MA do filtro, enquanto os sinais vendidos requerem que se mantenha **acima** da MA do filtro.

### Passo 2 – Confirmação diferida
Se nenhum sinal for disparado no Passo 1, a estratégia verifica um cruzamento que começou duas barras atrás, mas ainda é válido. Isso reflete o comportamento original do EA que busca no histórico recente por cruzamentos perdidos. Para evitar preenchimentos repetidos, o sinal só se ativa quando pelo menos três barras passaram desde o último trade.

### Execução de ordens
- As entradas são executadas ao preço de mercado. Posições opostas são fechadas antes de abrir na nova direção.
- As saídas ocorrem quando os níveis de stop loss, take profit ou trailing stop são tocados no candle atual. A operação é fechada com uma ordem de mercado assim que um nível protetor é violado.

## Gestão de risco
- As distâncias de **Stop Loss** e **Take Profit** são configuradas em pips. Elas são convertidas em offsets de preço usando o `PriceStep` do instrumento (padrão `1` quando indisponível).
- O **Trailing Stop** começa a partir do preço de entrada e segue o movimento de preço favorável. O stop é atualizado sempre que o melhor preço avança pelo menos `TrailingStepPips` pips além do nível de trailing anterior.
- Se tanto um stop fixo quanto um trailing stop estiverem ativos, a estratégia usa o nível mais conservador (mais alto para posições compradas, mais baixo para posições vendidas).

## Dimensionamento de posição
- Quando `UseRiskPercent` é **true**, o volume equivale a `Equity * RiskPercent / (StopLossPips * PipValue)`. Se nenhum stop for definido, a estratégia recorre ao volume fixo.
- Quando `UseRiskPercent` é **false**, o tamanho da operação é sempre `FixedVolume`.
- `PipValue` deve refletir o valor monetário de um único pip por um lote/contrato do instrumento negociado.

## Notas de implementação
- A implementação do StockSharp funciona inteiramente em candles fechados e não registra ordens pendentes. Usuários que precisam de entradas de stop ou limite podem estender a estratégia adequadamente.
- O filtro de terceira média móvel pode ser desabilitado para negociar cada cruzamento, correspondendo à opção `InpFilterMA = false` do EA.
- Certifique-se de que o tipo de candle, passo de preço e parâmetros de valor de pip correspondam ao instrumento negociado para controle de risco correto.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `FirstPeriod` | Período da primeira média móvel. | 5 |
| `FirstShift` | Deslocamento (barras) aplicado à saída da primeira média móvel. | 3 |
| `FirstMethod` | Método de suavização da primeira média móvel (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `FirstAppliedPrice` | Preço aplicado para a primeira média móvel (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPeriod` | Período da segunda média móvel. | 8 |
| `SecondShift` | Deslocamento (barras) aplicado à saída da segunda média móvel. | 5 |
| `SecondMethod` | Método de suavização para a segunda média móvel. | `Smoothed` |
| `SecondAppliedPrice` | Preço aplicado para a segunda média móvel. | `Close` |
| `UseFilter` | Habilita o filtro de direção da terceira média móvel. | `true` |
| `ThirdPeriod` | Período do filtro da terceira média móvel. | 13 |
| `ThirdShift` | Deslocamento (barras) aplicado à saída da terceira média móvel. | 8 |
| `ThirdMethod` | Método de suavização para o filtro da terceira média móvel. | `Smoothed` |
| `ThirdAppliedPrice` | Preço aplicado para o filtro da terceira média móvel. | `Close` |
| `UseRiskPercent` | Alterna entre volume fixo e dimensionamento de posição baseado em porcentagem. | `true` |
| `FixedVolume` | Tamanho da operação quando o dimensionamento fixo está ativo. | 0.1 |
| `RiskPercent` | Fração do capital da conta arriscado por operação. | 5 |
| `PipValue` | Valor monetário de um pip por lote/contrato. | 1 |
| `StopLossPips` | Distância do stop-loss em pips. | 50 |
| `TakeProfitPips` | Distância do take-profit em pips. | 50 |
| `TrailingStopPips` | Distância do trailing stop em pips. | 10 |
| `TrailingStepPips` | Incremento mínimo de pips necessário para avançar o trailing stop. | 4 |
| `CandleType` | Tipo de dado de candle / período usado pela estratégia. | Candles de 1 minuto |
