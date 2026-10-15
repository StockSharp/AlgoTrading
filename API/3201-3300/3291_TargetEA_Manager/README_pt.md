# Estratégia Target EA Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **estratégia Target EA Manager** é uma versão fiel para StockSharp do expert do MetaTrader *TargetEA_v1.5*. A estratégia não abre novas operações por conta própria. Em vez disso, monitora constantemente o lucro e prejuízo flutuantes das ordens que já pertencem à estratégia e, se necessário, liquida posições e cancela ordens pendentes quando limites definidos pelo usuário são atingidos. O comportamento reproduz a lógica de gestão de "cesta" do expert original: ordens de compra e venda podem ser avaliadas independentemente ou como uma única cesta combinada.

A estratégia assina dados Level1 (melhor bid e ask) e depende da API de alto nível para fechamento de posições e cancelamento de ordens. Cotações bid e ask em tempo real são traduzidas em métricas de lucro não realizado para a exposição aberta.

## Recursos principais
- **Cestas independentes ou combinadas** - escolha se ordens compradas e vendidas são tratadas separadamente ou juntas via `ManageBuySellOrders`.
- **Múltiplos tipos de alvo** - limites podem ser expressos em pips, em moeda da conta por lote ou como percentual do saldo do portfólio, correspondendo ao flag `TypeTargetUse` da versão MQL.
- **Gatilhos de dois lados** - seletores separados para reagir a lucros flutuantes (`CloseInProfit`) e perdas flutuantes (`CloseInLoss`).
- **Limpeza de ordens pendentes** - cancelamento opcional de ordens pendentes de compra e/ou venda sempre que uma cesta é fechada.
- **Operações de alto nível** - saídas a mercado são executadas com `BuyMarket` / `SellMarket`, e ordens pendentes são canceladas pela coleção de ordens da estratégia.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `ManageBuySellOrders` | `Separate` emula duas cestas (comprada e vendida), `Combined` funde ambos os lados. |
| `CloseBuyOrders` / `CloseSellOrders` | Habilita liquidação para o respectivo lado. |
| `DeleteBuyPendingPositions` / `DeleteSellPendingPositions` | Cancela ordens pendentes ativas depois que uma cesta fecha. |
| `TypeTargetUse` | `Pips`, `CurrencyPerLot` ou `PercentageOfBalance` selecionam a medição aplicada ao PnL aberto. |
| `CloseInProfit` / `CloseInLoss` | Ativam gatilhos de lucro ou perda. |
| `TargetProfitInPips`, `TargetLossInPips` | Limites em pips. Quando o instrumento fornece `PriceStep`, o valor de pip é calculado como `priceDifference / PriceStep * (volume / VolumeStep)`. |
| `TargetProfitInCurrency`, `TargetLossInCurrency` | Lucro ou perda flutuante por lote, multiplicado pelo volume atual antes da comparação. |
| `TargetProfitInPercentage`, `TargetLossInPercentage` | Percentual do saldo do portfólio que deve ser atingido antes do fechamento. O expert original compara o lucro flutuante bruto com `Balance ± Balance * Percentage / 100`, e esta versão mantém essa convenção intacta. |

## Comportamento
1. **Rastreamento de estado** - operações executadas atualizam totais internos de volume comprado e vendido e seus preços médios ponderados. Posições hedgeadas (compradas e vendidas) são, portanto, tratadas corretamente.
2. **Cálculo de PnL** - cada atualização Level1 renova valores bid/ask, a partir dos quais são calculados lucros em pips e em moeda para ambos os lados.
3. **Avaliação de alvo** - dependendo do modo de alvo e do modo de cesta, os limites correspondentes são verificados. Verificações de lucro exigem valores *maiores ou iguais* aos alvos configurados, enquanto verificações de perda usam comparações *menores ou iguais*, correspondendo à lógica MQL.
4. **Liquidação da cesta** - quando uma condição é satisfeita, a estratégia opcionalmente cancela ordens pendentes desse lado e envia a ordem a mercado necessária para zerar a exposição aberta.

A implementação evita intencionalmente coleções adicionais ou armazenamento de indicadores e depende da API de alto nível do StockSharp, assim como o EA original.
