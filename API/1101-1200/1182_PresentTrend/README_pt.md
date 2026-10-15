# PresentTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Linha ATR causal controlada por RSI do fechamento ou MFI do preço típico e volume. Cruzamentos confirmados com a linha de dois candles atrás abrem, invertem completamente ou fecham posições.

## Linha e sinais

Somente candles concluídos, cinco minutos por padrão. ATR Wilder nativo e oscilador selecionado compartilham `Length=14`, positivo. O oscilador não selecionado não é registrado. Ambos precisam estar formados:

- Oscilador ≥50: candidato `low - Multiplier * ATR`; linha = máximo entre candidato e linha anterior.
- Oscilador <50: candidato `high + Multiplier * ATR`; linha = mínimo entre candidato e linha anterior.
- Primeira linha formada = candidato selecionado, sem inventar histórico. `Multiplier=1`, positivo.

ATR inclui gaps em relação ao fechamento anterior, inicia com média crescente e depois Wilder. RSI suaviza ganhos/perdas do fechamento por Wilder e conserva o resultado anterior quando o fechamento não muda. MFI usa `typical=(high+low+close)/3` vezes volume: típico crescente é fluxo positivo, decrescente negativo, igual nenhum. Nas últimas `Length` velas, `100 * positiveSum / (positiveSum + negativeSum)`, ou 100 sem fluxo negativo.

Cruzamento bruto comprado em t exige `line[t-1] <= line[t-3]` e `line[t] > line[t-2]`; vendido usa ≥ e < respectivamente. Exige quatro valores formados.

Compra é confirmada somente se existe sinal bruto vendido anterior mais recente que a compra bruta anterior, ou ainda não existe compra anterior. Venda é simétrica. Cada sinal bruto é registrado após a comparação, inclusive quando a negociação é rejeitada. O primeiro não opera sem sinal contrário observado. Repetições do mesmo lado exigem sinal contrário intermediário.

## Execução e padrões

Com ambos os lados habilitados, abre `Volume` de zero ou envia `Volume + abs(Position)` para inversão completa da posição oposta. Mesmo lado mantém. Apenas comprado: venda confirmada fecha toda a compra sem abrir venda; apenas vendido é simétrico. Direção desabilitada de zero não age. Ordens pendentes bloqueiam duplicados enquanto linha e histórico avançam. Sinais rejeitados não são adiados; não há espera de seis horas.

Não há stop-loss/take-profit de preço separados. A saída unilateral usa sinal contrário confirmado, não ordem stop de preço ATR.

| Parâmetro | Padrão |
|---|---:|
| `Length` | 14, período compartilhado positivo |
| `Multiplier` | 1, distância ATR positiva |
| `IsUseMfi` | false seleciona RSI; true MFI |
| `IsLongEnabled` / `IsShortEnabled` | true / true; pelo menos um lado habilitado |
| `CandleType` | 5 minutos |

Testes C# e Python calculam independentemente ATR, RSI/MFI, linha e cruzamentos brutos/confirmados no arquivo BTC/TON. Cada ordem e posição real é verificada por momento, direção, inversão completa ou saída unilateral. Ambos os osciladores e sinais separados por menos de seis horas são exercitados. Não comprovam lucratividade.
