# Grade Adaptável MT4 (StockSharp Porta)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia recria o consultor especialista "Adaptive Grid Mt4" para StockSharp de alto nível API. Ele descarta uma grade simétrica de
ordens de compra e venda em torno do fechamento da vela atual. As distâncias da grade são derivadas do Average True Range (ATR) e
são, portanto, adaptáveis à volatilidade do mercado. Cada ordem pendente expira após um número configurável de velas, mantendo o
carteira de pedidos organizada em mercados laterais.

Quando uma ordem de entrada é preenchida, a estratégia registra imediatamente as ordens correspondentes de take-profit e stop-loss a preços calculados
do instantâneo ATR que produziu a grade. As ordens de proteção são individuais com a entrada preenchida e persistem até serem executadas
ou cancelado manualmente.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `GridLevels` | Número de ordens stop acima e abaixo do mercado. Equivalente à entrada `nGrid` do EA. |
| `TimerBars` | Número de velas concluídas após as quais qualquer entrada pendente é cancelada (MT4 `nBars`). |
| `PriceOffsetMultiplier` | Multiplicador ATR aplicado ao deslocamento inicial do preço atual (`Poffset`). |
| `GridStepMultiplier` | Multiplicador ATR usado para o espaçamento entre níveis de grade consecutivos (`Pstep`). |
| `StopLossMultiplier` | Multiplicador ATR que define a distância do stop-loss associado a cada ordem (`StopLoss`). |
| `TakeProfitMultiplier` | Multiplicador ATR que define a distância do take-profit (`TakeProfit`). |
| `AtrPeriod` | período ATR médio. Espelha o valor codificado de 14 do script. |
| `OrderVolume` | Volume utilizado para todas as ordens pendentes (MT4 `Lot`). |
| `CandleType` | Período que impulsiona o recálculo da grade (`Wtf`). |

## Lógica de negociação

1. Assine velas do `CandleType` configurado e alimente um ATR(14).
2. Em cada vela acabada:
   - Avançar o contador de barras interno e cancelar pedidos de grade pendentes que excederam `TimerBars`.
   - Ignore o processamento adicional se ATR não estiver formado, se alguma ordem de grade ainda estiver ativa ou se a estratégia já mantiver uma posição.
   - Calcule o deslocamento do rompimento, o espaçamento da grade, as distâncias de stop-loss e de take-profit como valores `ATR * multiplier`.
   - Coloque `GridLevels` pares de ordens de compra e venda em torno do fechamento da vela, normalizando os preços com
`Security.ShrinkPrice` para respeitar o tamanho do tick do instrumento.
3. Quando uma entrada for preenchida, remova-a da lista da grade rastreada e gere as ordens de proteção correspondentes:
   - As entradas longas recebem um stop-loss de `SellStop` e um take-profit de `SellLimit`.
   - As entradas curtas recebem um stop-loss de `BuyStop` e um take-profit de `BuyLimit`.
4. As ordens de proteção são monitoradas via `OnOrderChanged` para que as entradas concluídas ou canceladas sejam removidas do rastreamento
listas.

## Notas

- A grade só é reconstruída quando não há posições abertas e todas as ordens de grade existentes expiraram, correspondendo à lógica `What()` de
o original EA.
- Os preços são calculados a partir do fechamento da vela, em vez do tick bruto `Bid/Ask`. Isso mantém a implementação orientada por velas
enquanto produz o mesmo layout simétrico em todo o mercado.
- O instantâneo ATR usado para a grade também é usado para ordens de proteção para imitar a parada e o lucro por bilhete de MetaTrader
valores.
- Ainda não há tradução do Python, correspondendo à solicitação.
