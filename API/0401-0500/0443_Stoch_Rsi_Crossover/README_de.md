# Stochastic RSI Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Methode wandelt den klassischen Relative Strength Index in einen Stochastic RSI um und glättet das Ergebnis dann in %K- und %D-Linien. Wenn %K %D innerhalb sorgfältig gewählter Zonen kreuzt, impliziert die Bewegung eine kurzfristige Verschiebung des Momentums. Der Algorithmus handelt nur, wenn eine dreischichtige EMA-Struktur die Richtung des übergeordneten Trends bestätigt, was Fehlsignale herausfiltert.

Sobald eine Kreuzung erscheint, muss der Schlusskurs je nach Signal auch über oder unter der schnellen EMA liegen. Dies schützt vor dem Handeln bei Oszillationen, die gegen den vorherrschenden Trend auftreten, und hält die Aufmerksamkeit auf Momenten, wenn Momentum mit der Richtung übereinstimmt. Trader können Glättungsperioden und RSI-Längen anpassen, um einzustellen, wie empfindlich das System auf Volatilitätsspitzen reagiert.

Das Risiko wird durch eine Average True Range-Lesart referenziert. Multiplikatoren des aktuellen ATR schlagen Stop‑Loss und Gewinnziele vor, die ein dynamisches Niveau liefern, das sich in volatilen Märkten ausdehnt und bei ruhiger Aktivität zusammenzieht. Obwohl das Skript keine Schutzaufträge automatisch sendet, helfen diese berechneten Niveaus beim manuellen Management oder können an zusätzliche Risikomodule angebunden werden.

## Details

- **Einstiegskriterien**:
  - **Long**: `%K` kreuzt über `%D`, `%K` in `[10,60]`, EMAs bullisch ausgerichtet, Preis über EMA1.
  - **Short**: `%K` kreuzt unter `%D`, `%K` in `[40,95]`, EMAs bärisch ausgerichtet, Preis unter EMA1.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Keine eingebaut.
- **Stops**: ATR-Multiples vorgeschlagen, aber nicht automatisch platziert.
- **Standardwerte**:
  - `SmoothK` = 3, `SmoothD` = 3.
  - `RsiLength` = 14, `StochLength` = 14.
  - `Ema1Length` = 20, `Ema2Length` = 50, `Ema3Length` = 100.
  - `AtrLength` = 14, `AtrLossMultiplier` = 1.5, `AtrProfitMultiplier` = 2.0.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
