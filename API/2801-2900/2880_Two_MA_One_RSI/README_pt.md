# Estratégia Two MA One RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o especialista MetaTrader 5 "Two MA one RSI" para o StockSharp. Combina um cruzamento de médias móveis rápida e lenta com uma confirmação RSI avaliada na vela fechada anterior. Interruptores flexíveis permitem converter cada comparação em uma regra "maior que" ou "menor que", para que a configuração possa ser invertida sem tocar no código.

## Detalhes
- **Critérios de entrada**:
  - Sinais comprados requerem que a MA rápida esteja abaixo da MA lenta há duas barras, que a MA rápida esteja acima da MA lenta na barra fechada mais recente, e que o RSI da barra anterior esteja acima do limiar superior. Cada comparação pode ser invertida por parâmetros booleanos.
  - Sinais vendidos espelham a lógica e verificam as relações de MA opostas junto com o RSI caindo abaixo do limiar inferior.
  - Ambas as MAs usam o mesmo tipo de média; o período lento é sempre `FastMaPeriod * SlowPeriodMultiplier`. Deslocamentos horizontais opcionais reproduzem o comportamento do MT5 onde os valores do indicador são lidos várias velas atrás.
- **Comprado/Vendido**: A estratégia pode abrir posições em ambas as direções. `CloseOppositePositions` controla se um novo sinal força o lado oposto a fechar antes de entrar.
- **Critérios de saída**:
  - Stop-loss e take-profit configuráveis em pips.
  - Trailing stop opcional que só se move depois que o preço avança pelo menos `TrailingStopPips + TrailingStepPips` além da entrada.
  - `ProfitClose` monitora P&L flutuante (usando o preço de passo do instrumento) e fecha todas as posições quando o valor alvo em moeda é atingido.
- **Stops**: Quando `StopLossPips` é zero, a estratégia depende puramente do módulo de trailing stop (se habilitado). `TrailingStopPips` requer um `TrailingStepPips` positivo, correspondendo à validação do especialista original.
- **Valores padrão**:
  - `FastMaPeriod = 10`, `SlowPeriodMultiplier = 2`.
  - `FastMaShift = 3`, `SlowMaShift = 0`.
  - `RsiPeriod = 10`, `RsiUpperLevel = 70`, `RsiLowerLevel = 30`.
  - `StopLossPips = 50`, `TakeProfitPips = 150`, `TrailingStopPips = 15`, `TrailingStepPips = 5`.
  - `MaxPositions = 10`, `ProfitClose = 100`, `TradeVolume = 1`.
- **Filtros**: Seis interruptores booleanos (`BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper`, `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower`) permitem ao usuário alterar instantaneamente o sentido de cada comparação.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período (ou outro tipo de vela) usado para análise. |
| `MaType` | Família de médias móveis (simples, exponencial, suavizada, ponderada, ponderada por volume). |
| `FastMaPeriod` | Período da MA rápida. |
| `SlowPeriodMultiplier` | Multiplicador de período da MA lenta (`lenta = rápida * multiplicador`). |
| `FastMaShift`, `SlowMaShift` | Deslocamentos horizontais em velas aplicados ao avaliar valores de MA. |
| `RsiPeriod` | Comprimento do RSI (usa a vela finalizada anterior). |
| `RsiUpperLevel`, `RsiLowerLevel` | Limiares RSI para confirmações compradas e vendidas. |
| `BuyPreviousFastBelowSlow`, `BuyCurrentFastAboveSlow`, `BuyRequiresRsiAboveUpper` | Ativar/desativar comparações para sinais comprados. |
| `SellPreviousFastAboveSlow`, `SellCurrentFastBelowSlow`, `SellRequiresRsiBelowLower` | Ativar/desativar comparações para sinais vendidos. |
| `StopLossPips`, `TakeProfitPips` | Stop protetor e alvo medido em pips (tamanho do pip derivado do passo de preço do instrumento). |
| `TrailingStopPips`, `TrailingStepPips` | Distância do trailing stop e melhoria mínima. |
| `MaxPositions` | Número máximo de entradas simultâneas por direção (`0` = ilimitado). |
| `ProfitClose` | Meta de lucro em moeda que fecha todas as posições ao ser atingida. |
| `CloseOppositePositions` | Se deve fechar o lado oposto antes de abrir uma nova operação. |
| `TradeVolume` | Tamanho base da ordem; também se sincroniza com a propriedade `Volume` da estratégia. |

## Notas de implementação
- Todas as decisões usam apenas velas finalizadas, igualando a lógica de "nova barra" do especialista MT5.
- O tamanho do pip é igual ao passo de preço do instrumento. Se seu mercado usa preços de pip fracionários, ajuste as configurações do instrumento adequadamente para que a tradução do pip corresponda à lógica `digits_adjust` do especialista original.
- Os trailing stops só começam depois que o preço avançou `TrailingStopPips + TrailingStepPips`; o stop é então ancorado `TrailingStopPips` distante do fechamento e só se move quando melhora pelo menos `TrailingStepPips`.
- `ProfitClose` calcula lucro flutuante usando `PriceStep` e `StepPrice` do instrumento. Certifique-se de que esses campos estejam configurados para resultados de moeda corretos.
