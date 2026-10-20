# Five Ema NoTouch Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Usar velas encerradas após formação da EMA. Vela inteiramente abaixo prepara compra acima da máxima; acima prepara venda abaixo da mínima. Verificar preparação anterior antes de substituí-la; tocar não rompe. Entrar apenas sem posição com Volume e limpar preparação durante a posição. Fixar stop no extremo oposto. A primeira execução real fixa risco até o stop e alvo a RewardRisk×risco. Proteção local nativa a mercado observa bid/ask entre velas. Execução além do stop é encerrada imediatamente. Sem espera nem aumento de posição.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `EmaPeriod` | 5 |
| `RewardRisk` | 3 |

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
