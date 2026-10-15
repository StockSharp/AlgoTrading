# Liquidex V1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Liquidex V1 ist eine Breakout-Scalping-Strategie, die aus dem ursprünglichen MQL-Experten-Advisor konvertiert wurde. Sie kombiniert einen **Bereichsfilter** und einen **gewichteten gleitenden Durchschnitt (WMA)**, um kurzfristige Chancen zu identifizieren.

## Handelslogik
1. Für jede abgeschlossene Kerze misst die Strategie deren Bereich (`high - low`).
2. Ist der Kerzenbereich kleiner als `RangeFilter`, wird die Kerze ignoriert.
3. Ein WMA mit der Periode `MaPeriod` wird anhand der Schlusskurse berechnet.
4. Wenn die Kerze unterhalb des WMA öffnet und darüber schließt, wird eine **Kauf**-Marktorder gesendet.
5. Wenn die Kerze oberhalb des WMA öffnet und darunter schließt, wird eine **Verkauf**-Marktorder gesendet.
6. Jede Position ist durch einen in `StopLoss` definierten Stop-Loss geschützt.

## Parameter
- `RangeFilter` – Mindestkerzenbereich in Preiseinheiten, der für den Handel erforderlich ist.
- `MaPeriod` – Anzahl der Perioden für den gewichteten gleitenden Durchschnitt.
- `StopLoss` – Schutz-Stop-Loss in Punkten.
- `CandleType` – Für die Analyse verwendete Kerzenserie.

Die Strategie verwendet `Strategy.Volume` als Ordergröße und kehrt die Position um, wenn ein entgegengesetztes Signal erscheint.
