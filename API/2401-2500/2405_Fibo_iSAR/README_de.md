# Fibo iSAR Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert schnelle und langsame Parabolic-SAR-Indikatoren mit Fibonacci-Retracement-Niveaus. Wenn der schnelle SAR über dem langsamen SAR und unter dem Preis liegt, erwartet die Strategie einen Aufwärtstrend und platziert eine Buy-Limit-Order bei der 50%-Fibonacci-Korrektur des jüngsten Bereichs. Der Stop-Loss wird unterhalb des Swing-Tiefs platziert und der Take-Profit bei der 161%-Extension. Für einen Abwärtstrend ist die Logik gespiegelt mit Sell-Limit-Orders.

## Details

- **Einstiegskriterien**: Trendrichtung durch schnellen/langsamen SAR; Einstieg bei 50% Fibonacci-Retracement.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss- oder Take-Profit-Niveaus.
- **Stops**: Ja.
- **Standardwerte**:
  - `StepFast` = 0.02
  - `MaximumFast` = 0.2
  - `StepSlow` = 0.01
  - `MaximumSlow` = 0.1
  - `CountBarSearch` = 3
  - `IndentStopLoss` = 30
  - `FiboEntranceLevel` = 50
  - `FiboProfitLevel` = 161
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR, Fibonacci
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
