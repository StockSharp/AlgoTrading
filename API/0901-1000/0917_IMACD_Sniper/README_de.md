# Strategie IMACD Sniper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

IMACD Sniper kombiniert MACD-Kreuzungen mit einem EMA-Trendfilter, Volumenbestätigung und starken Kerzenmustern. Dynamisches Take Profit und Stop Loss basieren auf dem jüngsten Durchschnittsbereich.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: MACD-Linie kreuzt die Signallinie nach oben, Preis über EMA, MACD-Delta > Mindest-Delta, beide Linien weit von null entfernt, Volumen über dem Durchschnitt, starke bullische Kerze.
  - **Short**: MACD-Linie kreuzt die Signallinie nach unten, Preis unter EMA, MACD-Delta > Mindest-Delta, beide Linien weit von null entfernt, Volumen über dem Durchschnitt, starke bärische Kerze.
- **Ausstiegskriterien**: Entgegengesetzter MACD-Kreuzung oder Erreichen von Take Profit / Stop Loss.
- **Stops**: Dynamisches Take Profit und Stop Loss basierend auf dem Durchschnittsbereich.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdDeltaMin` = 0.03
  - `MacdZeroLimit` = 0.05
  - `RangeLength` = 14
  - `RangeMultiplierTp` = 4.0
  - `RangeMultiplierSl` = 1.5
  - `EmaLength` = 20
  - `CandleType` = tf(1m)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long & Short
  - Indikatoren: MACD, EMA, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
