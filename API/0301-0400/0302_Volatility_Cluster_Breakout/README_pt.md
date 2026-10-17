# Volatility Cluster Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa velas de 5 minutos concluídas e aguarda indicadores formados e permissão para negociar. Todas as estatísticas móveis incluem a vela de sinal. `C` é seu fechamento, `M = SMA(C, 20)`, `D = desvio padrão populacional(C, 20)`, `U = M + 2 × D`, `L = M − 2 × D` e `A = ATR(14)` com suavização de Wilder do intervalo verdadeiro.

A condição de alta volatilidade é `A >= 1.15 × SMA(A, 28)`.

Sem posição, compra quando há alta volatilidade e `C >= U`; vende quando ela é confirmada e `C <= L`. O volume de entrada é `Volume`. Uma nova entrada exige o fim da pausa e nenhuma ordem pendente. A entrada exige `A > 0`.

`k = StopMultiplier`. Fixa o fechamento e o ATR da vela de entrada: stop de compra `C_entry − k × A_entry`, stop de venda `C_entry + k × A_entry`. Em cada vela concluída, encerra toda a posição real com uma ordem a mercado se o fechamento atingir o stop fixo, uma posição comprada tiver `C <= M`, uma vendida tiver `C >= M` ou a alta volatilidade deixar de ser confirmada.

A pausa bloqueia novas entradas nas próximas 60 velas concluídas após uma ordem; as saídas continuam ativas. O stop é verificado no fechamento da vela e a ordem a mercado usa o preço disponível. A distância do stop permanece fixa durante a posição. Não há realização de lucro automática nem stop móvel.

## Parâmetros padrão

```text
PriceAvgPeriod = 20
StdDevMultiplier = 2
AtrPeriod = 14
StopMultiplier = 2
CooldownBars = 60
CandleType = TimeSpan.FromMinutes(5).TimeFrame()
```
