# Williams Alligator ATR

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

As três linhas Alligator suavizam preço mediano (máxima+mínima)/2; Length e Shift controlam suavização e atraso em velas completas. Teeth é calculado e precisa estar formado, sem filtro adicional. Com linhas e ATR de Wilder formados, Lips−Jaw de <=0 para >0 abre Volume comprado sem posição; de >=0 para <0 encerra toda compra real. Nunca abre venda a descoberto. Fixar ATR×AtrMultiplier como distância do stop local nativo desde execução real; bid/ask pode ativá-lo entre velas. Sem espera, alvo de lucro ou stop ATR móvel.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `JawLength` | 13 |
| `TeethLength` | 8 |
| `LipsLength` | 5 |
| `JawShift` | 8 |
| `TeethShift` | 5 |
| `LipsShift` | 3 |
| `AtrPeriod` | 14 |
| `AtrMultiplier` | 2 |

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
