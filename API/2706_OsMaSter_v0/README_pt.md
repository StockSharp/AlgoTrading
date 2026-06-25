# Estratégia OsMaSter V0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertida do especialista de MetaTrader 5 "OsMaSter v0" (arquivo MQL `OsMaSter v0.mq5`).
- Usa um padrão de histograma MACD (OsMA) para identificar reversões de momentum após uma breve consolidação.
- Projetada para operar em um único instrumento e período selecionado pelo usuário através do parâmetro `CandleType`.
- Converte automaticamente as configurações de risco baseadas em pips (stop-loss e take-profit) para offsets de preço absolutos usando o passo de preço do instrumento e a precisão decimal.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `FastPeriod` | 9 | Comprimento da EMA rápida para o histograma MACD. |
| `SlowPeriod` | 26 | Comprimento da EMA lenta para o histograma MACD. |
| `SignalPeriod` | 5 | Comprimento da EMA de sinal usada para suavizar o histograma. |
| `StopLossPips` | 30 | Distância ao stop de proteção em pips. Definir como `0` para desabilitar. |
| `TakeProfitPips` | 50 | Distância ao alvo de lucro em pips. Definir como `0` para desabilitar. |
| `TradeVolume` | 1 | Volume de ordem (lotes) usado para entradas de mercado. |
| `CandleType` | Candles de 15 minutos | Período usado para os cálculos do indicador. |

## Lógica de sinais
1. A estratégia mantém os últimos quatro valores do histograma MACD (`hist0` = atual, `hist1` = anterior, ..., `hist3` = três candles atrás).
2. **Entrada comprada** quando `hist3 > hist2`, `hist2 < hist1`, e `hist1 < hist0` &mdash; uma sequência ascendente após um mínimo local.
3. **Entrada vendida** quando `hist3 < hist2`, `hist2 > hist1`, e `hist1 > hist0` &mdash; uma sequência descendente após um máximo local.
4. Apenas uma posição pode estar aberta por vez. A estratégia ignora novos sinais enquanto uma negociação está ativa.

## Gerenciamento de posição
- As ordens são enviadas com `BuyMarket()` ou `SellMarket()` usando o `TradeVolume` configurado.
- `StartProtection` anexa offsets de stop-loss e take-profit baseados nas entradas de pips. O tamanho do pip segue a convenção forex (passo de preço × 10 para instrumentos de 3/5 decimais, caso contrário o próprio passo de preço).
- Não há regras de saída adicionais; as posições são gerenciadas exclusivamente pelas ordens de proteção ou por intervenção manual.

## Notas
- Certifique-se de que o `Security` tem valores corretos de `PriceStep` e `Decimals` para que a conversão de pips coincida com a especificação do broker.
- Ajuste o período do candle e o volume para corresponder ao horizonte de negociação do mercado alvo.
- Como a estratégia aguarda a execução do stop ou do alvo, sinais consecutivos na mesma direção são ignorados enquanto uma posição permanece aberta.
