# RSI-Strategie mit dynamischen Überkauft-/Überverkauft-Niveaus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt RSI-Extreme an adaptiven Grenzen mit einem Kurstrendfilter auf abgeschlossenen 5-Minuten-Kerzen. Berechnet `R = RSI(14)`, `P = SMA(50)` des Schlusskurses sowie `A = SMA(50)` und `D = StandardDeviation(50)` von R. Grenzen: `upper = min(A + 2 × D, 85)`, `lower = max(A − 2 × D, 15)`. Alle Indikatoren müssen gebildet sein.

Ohne Position Kauf bei `R <= lower && Close >= P × 0.995`, Verkauf bei `R >= upper && Close <= P × 1.005`. Long vollständig bei `R >= A || Close < P × 0.995` schließen, Short bei `R <= A || Close > P × 1.005`. Jeder Signalauftrag startet `CooldownBars = 48` abgeschlossene Kerzen Pause für weitere Signalentscheidungen.

Ausgeführte Positionen haben lokalen nativen Prozentschutz `StopLossPercent = 2` ab dem tatsächlichen Einstiegskurs. Bid/Ask-Überwachung löst unabhängig vom Signalcooldown einen Marktausstieg aus; zwischen Kerzen ist ein Kursstrom erforderlich. Kein Take-Profit.

`RsiPeriod = 14; MovingAvgPeriod = 50; StdDevMultiplier = 2; StopLossPercent = 2; CooldownBars = 48`.
