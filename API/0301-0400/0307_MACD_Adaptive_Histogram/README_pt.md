# Estratégia de Histograma Adaptativo MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Negocia valores estatisticamente grandes do histograma MACD. Usa velas concluídas de 15 minutos e `H = MACD(12,26) − Signal(9)`. Calcula `A = SMA(20)` e `D = StandardDeviation(20)` de H. Aguarda todos os indicadores formados e `D > 0`.

Sem posição, compra com `H >= A + 2 × D && H > 0`; vende com `H <= A − 2 × D && H < 0`. Fecha toda a compra com `H <= A`, e toda a venda com `H >= A`. Cada ordem de sinal impõe pausa de `CooldownBars = 16` velas concluídas para novas decisões.

Posições executadas têm proteção nativa local percentual `StopLossPercent = 2` desde o preço real da entrada. Monitora bid/ask e envia saída a mercado independentemente da pausa dos sinais; exige cotações entre velas. Sem take-profit.

`FastPeriod = 12; SlowPeriod = 26; SignalPeriod = 9; HistogramAvgPeriod = 20; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 16`.
