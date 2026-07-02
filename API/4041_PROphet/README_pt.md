# Estratégia do Profeta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista "PROphet". O EA original avalia a negociação recente
percorre quatro velas históricas e usa essas faixas ponderadas para acionar novas negociações. Mantém posições abertas apenas entre os
nas sessões europeia e norte-americana e segue o stop loss sempre que o preço se move uma distância fixa a favor da negociação. O StockSharp
a implementação mantém toda essa mecânica enquanto os adapta ao modelo de compensação usado pelos portfólios StockSharp.

## Lógica de negociação
- Assine o prazo configurado (`CandleType`, padrão M5) e processe apenas velas concluídas.
- Mantenha as três velas concluídas mais recentemente para reproduzir a indexação `High[i]` e `Low[i]` usada pela versão MQL.
- Calcule o gatilho longo `Qu(X1, X2, X3, X4)` e o gatilho curto `Qu(Y1, Y2, Y3, Y4)` em cada barra. Cada termo multiplica um
intervalo ponderado (por exemplo `|High[1] - Low[2]|`) pelo peso correspondente menos cem, exatamente como no código original.
- Permitir novas entradas somente quando a hora atual estiver entre `TradeStartHour` e `TradeEndHour` (inclusive). Isso imita o homem
janela de negociação normal do especialista MQL (das 10h00 às 18h00 por padrão).
- Utilize uma única ordem de mercado cujo volume neutralize qualquer exposição oposta antes de abrir a nova posição. Isso reflete o Mag
ic Filtros de número da implementação MetaTrader.

## Gerenciamento de risco e rastreamento
- A estratégia converte as distâncias de parada baseadas em pontos MetaTrader em unidades de preço por meio do instrumento `PriceStep`. Os padrões (`B
uyStopLossPoints = 68`, `SellStopLossPoints = 72`) corresponde às variáveis externas MQL.
- Assim que o lance (para negociações longas) ou o pedido (para negociações curtas) ultrapassar o stop existente em `spread + 2 * stopDistance`, o
O trailing stop é avançado para `currentPrice ± stopDistance`, usando dados de nível 1 ao vivo, quando disponíveis.
- As negociações abertas são fechadas à força após `ExitHour`. O valor padrão (18) reproduz o comportamento original de fechamento da posição
s depois das 18:00, horário do servidor.
- As saídas protetoras usam ordens de mercado porque o API de alto nível de StockSharp não gera ordens de stop automaticamente. Isso mantém
comportamento determinístico entre corretores.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `AllowBuy` | Permite negociações longas. |
| `AllowSell` | Permite negociações curtas. |
| `X1`, `X2`, `X3`, `X4` | Pesos aplicados aos componentes do intervalo do lado longo dentro da fórmula `Qu`. |
| `BuyStopLossPoints` | Distância de stop-loss para negociações longas expressa em MetaTrader pontos. |
| `Y1`, `Y2`, `Y3`, `Y4` | Pesos aplicados aos componentes do intervalo menor dentro da fórmula `Qu`. |
| `SellStopLossPoints` | Distância de stop-loss para negociações curtas expressa em MetaTrader pontos. |
| `TradeVolume` | Volume base (lotes) utilizado para novas entradas. O volume extra é adicionado automaticamente para fechar a exposição oposta. |
| `TradeStartHour` | Primeira hora da janela de negociação (inclusive). |
| `TradeEndHour` | Última hora da janela de negociação (inclusive). |
| `ExitHour` | Hora após a qual todas as negociações abertas são fechadas. |
| `CandleType` | Prazo das velas utilizadas para análise. |

## Notas
- StockSharp carteiras são compensadas por padrão. Quando um novo sinal aparece, a estratégia adiciona o volume necessário para nivelar o ex
posição atual antes de abrir a nova negociação, que reproduz o design de posição única por direção da experiência MetaTrader
Rt.
- O script MQL usou a propagação de símbolos relatada por `MarketInfo`. A porta recupera o spread dos dados de nível 1 quando disponível
e, caso contrário, volta a uma única etapa de preço.
- Como o trailing stop é avaliado no fechamento de cada vela finalizada, pode ocorrer derrapagem em comparação com o stop no nível do tick
atualizações realizadas pelo EA original.
