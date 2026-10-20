# Bitcoin 1H/15M Breakout

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Negocia rompimentos em velas concluídas de 15 minutos do intervalo da hora anterior completa. As velas menores são agrupadas em horas UTC. Uma hora só é válida com os quatro quartos consecutivos desde a abertura exata; uma hora inicial parcial ou lacuna não cria intervalo.

Para uma vela menor aberta na hora H, usar apenas máxima/mínima da hora H−1 concluída, nunca a hora atual parcial. Sem posição, comprar `Volume` com fechamento estritamente acima da máxima ou vender `Volume` abaixo da mínima. Ambos os lados são permitidos; posições abertas não aumentam nem revertem por rompimentos. Uma ordem ativa bloqueia outra.

A proteção local nativa fixa `StopLossBuffer` como distância absoluta das execuções de entrada, padrão 50. O take-profit usa `StopLossBuffer * RiskRewardRatio`, padrão 100 com razão 2. Ambas as saídas são a mercado pela posição inteira e não mudam com novos intervalos horários. `HigherTimeFrame` deve ser múltiplo inteiro do intervalo menor.

A proteção local nativa recebe velas e atualizações de preço assinadas do instrumento. As distâncias partem das execuções reais de entrada. As saídas são a mercado; o preço depende do mercado disponível e do passo de preço.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | `15m` |
| `HigherTimeFrame` | `1h` |
| `StopLossBuffer` | `50` |
| `RiskRewardRatio` | `2` |
