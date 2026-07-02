# Estratégia CorrTime
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia CorrTime é um sistema de símbolo único que replica o expert advisor MetaTrader de mesmo nome. Ela analisa a correlação entre preços de fechamento e sua ordem cronológica para detectar aceleração ou reversão de momentum. O algoritmo opera em candles concluídos e combina três camadas de confirmação:

1. **Filtro de volatilidade:** a largura das bandas de Bollinger deve ficar dentro de uma faixa configurável de atividade aceitável, para que o sistema evite fases laterais e excessivamente voláteis.
2. **Filtro de força de tendência:** o Average Directional Index (ADX) deve permanecer acima de um limiar antes que sinais de correlação sejam avaliados.
3. **Gatilhos de correlação:** estimadores de correlação Pearson, Spearman, Kendall ou Fechner medem quão de perto o preço evolui com o tempo. Uma mudança súbita do coeficiente gera a decisão de negociação.

Embora o robô original tenha sido projetado para EURUSD no timeframe H1, a versão StockSharp mantém todos os parâmetros configuráveis. As configurações padrão permanecem fiéis à fonte (candles de 1 hora, correlação Fechner, modo de negociação reverso).

## Fluxo de negociação

1. Assinar o `CandleType` selecionado e aguardar uma barra concluída.
2. Atualizar bandas de Bollinger e valores ADX no novo candle.
3. Rejeitar a barra quando:
   - O spread de Bollinger convertido para pips está fora de `[BollingerSpreadMin, BollingerSpreadMax]`.
   - ADX está abaixo de `AdxLevel`.
   - O candle começa fora da janela `[EntryHour, EntryHour + OpenHours]` (com suporte a virada da meia-noite).
4. Construir um histórico rolante de preços de fechamento e calcular o coeficiente de correlação nos lookbacks `CorrelationRangeTrend` e `CorrelationRangeReverse`. O código recalcula os três últimos valores de correlação para detectar um cruzamento real dos limites, exatamente como o arquivo include original fazia com buffers.
5. Gatilho seguidor de tendência (quando `TradeMode` é *TrendFollow* ou *Both*):
   - **Compra:** a correlação estava abaixo de `CorrLimitTrendBuy`, ainda estava abaixo na barra anterior e cruza acima do limiar na última barra.
   - **Venda:** a correlação estava acima de `-CorrLimitTrendSell`, ainda estava acima na barra anterior e cruza abaixo de `-CorrLimitTrendSell` na última barra.
6. Gatilho de reversão (quando `TradeMode` é *Reverse* ou *Both*):
   - **Compra:** a correlação estava abaixo de `-CorrLimitReverseBuy`, ainda estava abaixo na barra anterior e sobe acima de `-CorrLimitReverseBuy` na última barra.
   - **Venda:** a correlação estava acima de `CorrLimitReverseSell`, ainda estava acima na barra anterior e cai abaixo de `CorrLimitReverseSell` na última barra.
7. Se as duas direções disparam simultaneamente, os sinais se cancelam, espelhando o comportamento do MetaTrader.
8. Se `CloseTradeOnOppositeSignal` estiver habilitado, a estratégia fecha imediatamente qualquer posição oposta antes de abrir uma nova.
9. Entradas são dimensionadas com a propriedade `Volume` e respeitam `MaxOpenOrders`, então a exposição líquida nunca excede `Volume * MaxOpenOrders` em nenhuma direção.
10. O risco é controlado por `StartProtection`: stop-loss e take-profit usam distâncias baseadas em pips, e a flag de trailing reutiliza a mesma distância de stop quando habilitada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Timeframe usado para gerar candles e alimentar todos os indicadores. |
| `CloseTradeOnOppositeSignal` | Fecha posições abertas quando o próximo sinal aponta na direção oposta. |
| `EntryHour`, `OpenHours` | Define a janela diária de negociação. `OpenHours = 0` mantém a janela aberta por uma única hora. |
| `BollingerPeriod`, `BollingerDeviation` | Configurações padrão das bandas de Bollinger aplicadas a fechamentos. |
| `BollingerSpreadMin`, `BollingerSpreadMax` | Largura mínima e máxima (em pips) exigida para o canal de Bollinger. |
| `AdxPeriod`, `AdxLevel` | Configuração do Average Directional Index e força mínima de tendência exigida. |
| `TradeMode` | Escolhe entre seguidor de tendência, reversão ou avaliação combinada. |
| `CorrelationRangeTrend`, `CorrelationRangeReverse` | Comprimentos de lookback para cálculos de correlação. |
| `CorrelationType` | Seleciona fórmulas de correlação Pearson, Spearman, Kendall ou Fechner. |
| `CorrLimitTrendBuy`, `CorrLimitTrendSell` | Limiares que definem um rompimento seguidor de tendência válido. |
| `CorrLimitReverseBuy`, `CorrLimitReverseSell` | Limiares que definem um rompimento de reversão válido. |
| `TakeProfitPips`, `StopLossPips`, `TrailingStopPips` | Parâmetros de risco expressos em pips e traduzidos para unidades de preço com o tamanho de pip do instrumento. |
| `MaxOpenOrders` | Limite superior no número agregado de entradas (teto por lado igual a `Volume * MaxOpenOrders`). |

## Notas práticas

- O tamanho de pip é deduzido dos decimais do ativo (5 ou 3 casas decimais correspondem a um multiplicador 10x) para imitar o tratamento de pontos do MetaTrader. Ajuste os limiares ao trabalhar com ativos não forex.
- Buffers de correlação precisam de pelo menos `lookback + 2` candles concluídos para avaliar um cruzamento. Durante o aquecimento, a estratégia permanece inativa.
- Como toda a lógica é executada em candles concluídos, a estratégia é resiliente ao ruído intrabar e espelha o comportamento original baseado em snapshots `iTime` e `iClose`.
- Combine esta estratégia com controles de risco em nível de carteira ao implantar múltiplas instâncias, pois o robô original também limitava o número total de ordens entre símbolos.
