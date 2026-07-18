# MACD + Stochastic Estratégia de filtro de tendências
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o comportamento do consultor especialista MetaTrader da pasta `MQL/7604`. O script original dependia de um oscilador personalizado que produzia buffers verdes e vermelhos. Na prática, os números `(15, 3, 3)` correspondem a um oscilador estocástico clássico, portanto, a porta StockSharp usa o indicador `Stochastic` integrado para a confirmação do sinal enquanto MACD e um filtro de tendência EMA gerenciam a direção.

A estratégia é negociada tanto longa quanto curta. Ele espera por um cruzamento estocástico na direção da negociação, exige que o histograma MACD cruze sua linha de sinal com distância suficiente de zero e exige que a inclinação EMA esteja de acordo com a entrada. O gerenciamento de risco reflete a versão MQL: um stop-loss fixo, um take-profit e um trailing stop baseado em pontos que estreita o nível de proteção assim que a negociação passa para o lucro.

## Indicadores

- **MovingAverageConvergenceDivergenceSignal** com parâmetros `fast = 12`, `slow = 26`, `signal = 9`. O histograma MACD deve cruzar sua linha de sinal enquanto permanece abaixo de zero para configurações longas e acima de zero para configurações curtas. Limites adicionais (`MacdOpenLevel`, `MacdCloseLevel`) impõem uma distância absoluta mínima da linha zero.
- **Stochastic** oscilador com `(Length = 15, KPeriod = 3, DPeriod = 3)`. A linha %K desempenha o papel de buffer "verde" e deve estar acima de %D para negociações longas (abaixo para negociações curtas). O mesmo cruzamento é usado para sair de posições.
- **ExponentialMovingAverage** com período `26`. O EMA fornece um filtro direcional: para uma negociação longa, o valor atual de EMA deve estar acima do EMA da barra anterior e, inversamente, para uma negociação curta.

## Lógica de entrada

1. **Configuração longa**
   - Stochastic %K > %D na vela fechada atual.
   - MACD histograma < 0 e > linha de sinal na barra atual.
   - MACD histograma <linha de sinal na barra anterior (ou seja, cruzamento de alta agora).
   - `|MACD| > MacdOpenLevel * price_step`.
   - EMA aumentando (atual EMA > anterior EMA).
2. **Configuração curta**
   - Stochastic %K <%D na vela atual.
   - MACD histograma > 0 e < linha de sinal na barra atual.
   - MACD histograma > linha de sinal na barra anterior (cruzamento de baixa agora).
   - `MACD > MacdOpenLevel * price_step`.
   - EMA caindo (atual EMA < anterior EMA).

Se a conta já tiver uma posição, nenhuma nova ordem será gerada até que a negociação aberta seja fechada.

## Sair da lógica

Enquanto uma posição está aberta, a estratégia impõe continuamente:

- **Saída do indicador**
  - As posições longas fecham quando `%K < %D`, MACD > 0, MACD < sinal, o anterior MACD estava acima de seu sinal e o histograma absoluto excede `MacdCloseLevel * price_step`.
  - As posições curtas fecham quando `%K > %D`, MACD < 0, MACD > sinal, o anterior MACD estava abaixo de seu sinal e `|MACD| > MacdCloseLevel * price_step`.
- **Stop-loss**: configurado por `StopLossPoints`, convertido em unidades de preço através do `PriceStep` do instrumento.
- **Realização de lucro**: `TakeProfitPoints` multiplicado por `PriceStep`.
- **Trailing Stop**: quando o lucro excede `TrailingStopPoints * PriceStep`, o nível de stop é aumentado (para posições compradas) ou diminuído (para posições vendidas) para que a negociação sempre bloqueie pelo menos esse valor de lucro.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho do pedido em lotes | `0.1` |
| `TakeProfitPoints` | Distância de lucro em pontos | `10` |
| `StopLossPoints` | Distância de stop-loss em pontos | `50` |
| `TrailingStopPoints` | Distância da parada final em pontos | `5` |
| `MacdOpenLevel` | Valor absoluto mínimo MACD para entradas | `3` |
| `MacdCloseLevel` | Valor absoluto mínimo MACD para saídas | `2` |
| `MacdFastPeriod` | Comprimento EMA rápido dentro de MACD | `12` |
| `MacdSlowPeriod` | Comprimento EMA lento dentro de MACD | `26` |
| `MacdSignalPeriod` | MACD comprimento do sinal EMA | `9` |
| `EmaPeriod` | EMA período para o filtro de tendência | `26` |
| `StochasticLength` | Stochastic janela de retrospectiva | `15` |
| `StochasticKPeriod` | Suavização %K | `3` |
| `StochasticDPeriod` | Suavização %D | `3` |
| `CandleType` | Prazo usado para cálculos | `15m` |

## Notas

- Todos os cálculos usam apenas velas finalizadas, correspondendo ao loop `start()` no EA original.
- O `PriceStep` fornecido pelo instrumento define um ponto. Quando a segurança não expõe uma etapa, a estratégia volta para `1`.
- O código depende puramente do API de alto nível de StockSharp: os indicadores são vinculados por meio de `SubscribeCandles().BindEx(...)`, nenhum buffer de histórico manual é criado e os pedidos usam `BuyMarket`/`SellMarket` como na versão MQL.
