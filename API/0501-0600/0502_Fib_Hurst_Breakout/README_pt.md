# Fib Hurst Breakout

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

CandleType intradiário. Faixa Fibonacci: máxima e mínima das LookbackPeriod velas diárias UTC completas anteriores, sem dia do sinal. Níveis L+0.618×(H−L) e L+0.382×(H−L). Hurst diário de HurstPeriod fechamentos anteriores: H = log(R/S)/log(N), R amplitude dos desvios acumulados da média, S desvio padrão populacional; variância zero bloqueia entrada. Comparar fechamento intradiário anterior e atual com o mesmo nível diário: cruzar acima de 61.8% com H>0.5 compra, abaixo de 38.2% com H<0.5 vende. Volume apenas sem posição. MaxTradesPerDay conta tentativas de ordens por data UTC histórica e MaxTotalTrades por execução. Proteção nativa por cotações fixa SL = RiskPercent% e TP = RiskPercent×RiskReward% desde execução real. Sem reversão ou espera.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(15).TimeFrame() |
| `HurstPeriod` | 50 |
| `LookbackPeriod` | 20 |
| `MaxTradesPerDay` | 5 |
| `MaxTotalTrades` | 510 |
| `RiskPercent` | 2 |
| `RiskReward` | 2 |
| `DailyHistoryFile` | não definido |

## Dados de entrada

CSV opcional `DailyHistoryFile`: `Date,High,Low,Close`, data `yyyy-MM-dd`. Datas únicas e 0 < Low ≤ Close ≤ High. Apenas dias anteriores ao dia do sinal intradiário. Velas diárias encerradas substituem a própria data; linhas futuras não afetam decisões anteriores.

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
