# Precision Trading: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Cruzamentos de EMA confirmados pela direção Hull, impulso RSI e volatilidade ATR/intervalo high-low. A descrição histórica refere-se ao scalping de ouro; o exemplo usa o instrumento configurado e não comprova resultados em ouro.

## Sinais e execução

Somente candles concluídos, de cinco minutos por padrão. Os sete indicadores precisam estar formados. O primeiro candle totalmente preparado registra as EMA/HMA anteriores sem entrada.

- Compra: EMA rápida anterior ≤ EMA lenta anterior, EMA rápida atual > EMA lenta atual, HMA atual > HMA anterior e RSI > `RsiLongThreshold` (55).
- Venda: EMA rápida anterior ≥ EMA lenta anterior, EMA rápida atual < EMA lenta atual, HMA atual < HMA anterior e RSI < `RsiShortThreshold` (45).
- Ambos exigem close e ATR positivos, `100 * ATR / close >= MinAtrPercent` (0.05) e `Highest(high) - Lowest(low) >= ATR * RangeAtrMultiplier` (3). A janela de extremos inclui o candle atual.

Um sinal válido abre `Volume` a partir da posição zero ou envia `Volume + abs(Position)` para inverter totalmente a posição contrária. Um sinal do mesmo lado mantém a posição. Ordens pendentes impedem envios duplicados. As EMA/HMA anteriores avançam em cada candle preparado: um cruzamento rejeitado não fica agendado para depois. Não há intervalo obrigatório de seis horas nem stop-loss/take-profit adicionais; um cruzamento contrário confirmado inverte a posição.

## Indicadores e valores padrão

| Parâmetro | Padrão | Significado |
|---|---:|---|
| `FastLength` / `SlowLength` | 14 / 40 | EMA nativas do fechamento; período rápido < lento |
| `HmaLength` | 55 | `WMA(floor(sqrt(L)), 2 * WMA(floor(L/2), close) - WMA(L, close))`; L ≥ 2 |
| `RsiLength` | 14 | RSI nativo com ganhos/perdas do fechamento suavizados por Wilder |
| `RsiLongThreshold` / `RsiShortThreshold` | 55 / 45 | Limiares estritos; `0 <= short < long <= 100` |
| `AtrLength` | 14 | ATR Wilder nativo, incluindo gaps relativos ao fechamento anterior |
| `RangeLength` | 20 | Maior high e menor low, incluindo o candle atual |
| `MinAtrPercent` | 0.05 | Mínimo inclusivo de ATR em porcentagem do fechamento; não negativo |
| `RangeAtrMultiplier` | 3 | Mínimo inclusivo do intervalo em unidades ATR; positivo |
| `CandleType` | 5 minutos | Candles concluídos para todos os indicadores |

Os pesos WMA crescem de 1 para a amostra mais antiga até o tamanho da janela para a mais recente. EMA inicia na média aritmética nativa e depois usa `2 / (length + 1)`. ATR inicia na média de uma janela crescente e depois usa Wilder. Os períodos são positivos; Hull também precisa formar a WMA final de comprimento igual à raiz quadrada inteira.

Os testes C# e Python calculam independentemente EMA, HMA, RSI, ATR e extremos móveis no arquivo BTC/TON existente. Verificam cada ordem, inversões completas, confirmações rejeitadas e sinais separados por menos de seis horas. Validam comportamento, não lucratividade ou adequação à negociação real.
