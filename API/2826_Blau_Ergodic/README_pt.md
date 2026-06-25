# Estratégia Blau Ergodic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o expert advisor **Exp_BlauErgodic** do MQL5 para o StockSharp. Reconstrói o oscilador Blau Ergodic através do
triplo suavizamento do momentum e seu valor absoluto com filtros EMA, gera um oscilador normalizado e uma linha de sinal, e
oferece três modos de sinal distintos que espelham o EA original.

A configuração padrão avalia velas completas de 4 horas. Você pode alterar o preço aplicado (fechamento, abertura, médias baseadas em alto/baixo),
cada profundidade de suavização, e o índice de barra (`SignalBar`) usado para ler sinais. As operações são dimensionadas pela propriedade
`Volume` da estratégia; entradas/saídas comprado/vendido podem ser desabilitadas individualmente através de parâmetros booleanos. Níveis protetores de stop loss e
take profit são definidos em pontos e convertidos em preços absolutos através de `Security.PriceStep`.

## Modos de sinal

- **Breakdown** – reage ao cruzamento da linha zero pelo oscilador. Compras abrem em transições de negativo para positivo e vendas em
  transições de positivo para negativo. As posições são fechadas quando o oscilador permanece no lado oposto de zero.
- **Twist** – busca reversões de inclinação. Uma configuração comprada aparece quando o oscilador estava caindo na barra anterior mas sobe na
  barra mais recente; uma configuração vendida requer o padrão inverso.
- **CloudTwist** – monitora o cruzamento da linha de sinal pelo oscilador. Compras são acionadas quando o oscilador sobe através da nuvem de sinal,
  e vendas quando cai novamente abaixo dela.

Todos os modos leem valores do indicador da barra especificada por `SignalBar` (padrão `1`, ou seja, a última barra completada) e dependem de
valores mais antigos para confirmação. Configure `SignalBar` para pelo menos `1` porque a conversão processa apenas velas terminadas.

## Regras de entrada e saída

- **Entradas compradas:** habilitadas quando `AllowBuyEntry` é verdadeiro, nenhuma posição comprada existente está aberta (`Position <= 0`), e o modo ativo
  gera uma condição de compra. A estratégia reverte qualquer exposição vendida comprando `Volume + |Position|`.
- **Entradas vendidas:** habilitadas quando `AllowSellEntry` é verdadeiro, nenhuma posição vendida existente está aberta (`Position >= 0`), e o modo ativo
  emite uma condição de venda. Cobre qualquer exposição comprada antes de estabelecer a venda.
- **Saídas compradas:** acionadas pela condição específica do modo, ou quando `StopLossPoints` / `TakeProfitPoints` são atingidos. Saídas
  forçadas ignoram o sinalizador `AllowBuyExit` para que stops protetores sejam sempre respeitados.
- **Saídas vendidas:** análogas à lógica de saída comprada com `AllowSellExit` e níveis de stop para operações vendidas.

## Parâmetros

- `CandleType` – período para assinaturas de velas (padrão velas de 4 horas).
- `Mode` – um de `Breakdown`, `Twist`, ou `CloudTwist`.
- `MomentumLength` – lookback para a diferença de momentum bruto.
- `First/Second/ThirdSmoothingLength` – profundidades de EMA para os filtros de momentum em cascata.
- `SignalSmoothingLength` – profundidade de EMA para a linha de sinal.
- `SignalBar` – índice da barra completada usado para ler sinais (mínimo `1`).
- `AppliedPrices` – fonte de preço que alimenta o oscilador (fechamento, abertura, mediana, típica, ponderada, etc.).
- `AllowBuyEntry`, `AllowSellEntry`, `AllowBuyExit`, `AllowSellExit` – habilitar ou desabilitar operações específicas.
- `StopLossPoints`, `TakeProfitPoints` – distâncias protetoras em pontos (convertidas via `Security.PriceStep`).

A conversão mantém o comportamento do expert MQL5, aproveitando a API de alto nível do StockSharp (`SubscribeCandles`,
`Bind`) e aderindo às convenções de estratégia do StockSharp com indentação por tabulações e comentários em inglês.
