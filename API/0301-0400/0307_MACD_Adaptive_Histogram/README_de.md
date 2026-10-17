# MACD-adaptives-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt statistisch große MACD-Histogrammwerte. Verwendet abgeschlossene 15-Minuten-Kerzen und `H = MACD(12,26) − Signal(9)`. Berechnet `A = SMA(20)` und `D = StandardDeviation(20)` von H. Wartet auf alle gebildeten Indikatoren und `D > 0`.

Ohne Position Kauf bei `H >= A + 2 × D && H > 0`, Verkauf bei `H <= A − 2 × D && H < 0`. Long vollständig bei `H <= A`, Short bei `H >= A` schließen. Jeder Signalauftrag startet `CooldownBars = 16` abgeschlossene Kerzen Pause für weitere Signalentscheidungen.

Ausgeführte Positionen haben lokalen nativen Prozentschutz `StopLossPercent = 2` ab dem tatsächlichen Einstiegskurs. Bid/Ask-Überwachung löst unabhängig vom Signalcooldown einen Marktausstieg aus; zwischen Kerzen ist ein Kursstrom erforderlich. Kein Take-Profit.

`FastPeriod = 12; SlowPeriod = 26; SignalPeriod = 9; HistogramAvgPeriod = 20; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 16`.
