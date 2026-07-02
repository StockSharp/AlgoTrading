# Estratégia Test MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Test MACD** é uma conversão fiel do expert advisor `TestMACD` do MetaTrader para a API de alto nível do StockSharp. Ela usa o indicador Moving Average Convergence Divergence (MACD) para detectar mudanças de momentum e executa operações sempre que a linha MACD cruza a linha de sinal em candles fechados. A estratégia opera em um único instrumento e timeframe fornecido pelo parâmetro `CandleType`.

## Lógica de negociação
1. Assinar dados de candles definidos por `CandleType` e calcular um indicador MACD com períodos rápido, lento e de sinal configuráveis.
2. Monitorar a diferença de valor MACD (`MACD - Signal`) em cada candle concluído.
3. Disparar uma **entrada altista** quando a diferença muda de sinal de não positiva para positiva, indicando que a linha MACD cruzou acima da linha de sinal. Qualquer exposição vendida é fechada antes de abrir a posição comprada.
4. Disparar uma **entrada baixista** quando a diferença muda de sinal de não negativa para negativa, indicando que a linha MACD cruzou abaixo da linha de sinal. Qualquer exposição comprada é fechada antes de abrir a posição vendida.
5. Todas as ordens são emitidas a mercado com volume fixo configurado por `TradeVolume`.
6. Cada entrada é automaticamente protegida com níveis de stop-loss e take-profit expressos em passos de preço para replicar a gestão de risco baseada em pontos do expert original.

## Gestão de risco
- Distâncias de stop-loss e take-profit espelham as entradas do MetaTrader e são fornecidas em passos de preço. Se o ativo não tiver informação `PriceStep`, a estratégia usa distâncias absolutas de preço com `MinPriceStep` ou `1` como multiplicador.
- Ordens protetoras são criadas uma vez, quando a estratégia inicia, via `StartProtection`, garantindo que se apliquem a cada operação posterior sem reconfiguração.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `FastPeriod` | Comprimento da EMA rápida usada nos cálculos MACD. | `12` |
| `SlowPeriod` | Comprimento da EMA lenta usada nos cálculos MACD. | `24` |
| `SignalPeriod` | Comprimento da EMA de sinal para suavização MACD. | `9` |
| `StopLossPoints` | Distância do stop-loss expressa em passos de preço. | `90` |
| `TakeProfitPoints` | Distância do take-profit expressa em passos de preço. | `110` |
| `TradeVolume` | Volume fixo para todas as ordens a mercado. | `1` |
| `CandleType` | Tipo de dados de candles e timeframe assinados pela estratégia. | `Timeframe de 30 minutos` |

## Notas de uso
- Anexe a estratégia a um ativo antes de iniciá-la para que `PriceStep` e `MinPriceStep` estejam disponíveis.
- Garanta que dados de mercado sejam fornecidos para o `CandleType` selecionado; caso contrário o indicador MACD não se formará e não haverá negociação.
- A estratégia registra cada evento de cruzamento, facilitando rastrear decisões de negociação durante backtests.

## Detalhes de conversão
- As classes originais do MetaTrader `CSignalMACD`, `CTrailingNone` e `CMoneyFixedLot` são substituídas por binding de indicadores StockSharp e mecanismos `StartProtection`.
- A lógica de `ExtStateMACD`, que verificava cruzamentos MACD, é representada por um detector de mudança de sinal na diferença MACD entre candles concluídos consecutivos.
- A gestão monetária é simplificada para um parâmetro de volume fixo, muito semelhante ao comportamento de lote fixo de `CMoneyFixedLot` quando o sizing percentual está desativado.
