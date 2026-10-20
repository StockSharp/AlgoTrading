# Sma Pullback Atr Exits

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Com velas encerradas, SMA de fechamentos e ATR de Wilder formados, comprar se fechamento < SMA rápida e rápida > lenta; vender se fechamento > rápida e rápida < lenta. Igualdades não geram entrada. Volume apenas sem posição, aguardando ordens pendentes. Fixar SL = ATR×AtrMultiplierSl e TP = ATR×AtrMultiplierTp no sinal. Proteção local nativa ancora distâncias na execução real e observa bid/ask. ATR posterior não move níveis. Encerrar todo saldo pela proteção, sem reversão por sinal contrário.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(30).TimeFrame() |
| `FastSmaLength` | 8 |
| `SlowSmaLength` | 30 |
| `AtrLength` | 14 |
| `AtrMultiplierSl` | 1.2 |
| `AtrMultiplierTp` | 2 |

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
