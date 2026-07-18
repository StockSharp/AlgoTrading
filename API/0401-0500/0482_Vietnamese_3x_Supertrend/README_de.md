# Vietnamese 3x Supertrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie stapelt drei SuperTrend-Indikatoren mit unterschiedlichen ATR-Längen und Multiplikatoren. Sie skaliert in Long-Positionen, wenn der langsame Trend bärisch ist und schnellere Trends Pullback-Gelegenheiten zeigen. Ein optionaler Break-Even-Stop schützt Gewinne, sobald sich der Preis günstig entwickelt.

## Details

- **Einstiegskriterien**:
  - Langsamer SuperTrend im Abwärtstrend.
  - **Long 1**: Mittlerer Aufwärtstrend und schneller Abwärtstrend.
  - **Long 2**: Mittlerer Abwärtstrend und Preis über der schnellen SuperTrend-Linie.
  - **Long 3**: Schneller Abwärtstrend und Ausbruch über das höchste Hoch während des schnellen Abwärtstrends.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Alle SuperTrends drehen aufwärts und die Kerze schließt bärisch.
  - Durchschnittlicher Einstiegspreis über dem aktuellen Schlusskurs.
  - Optionaler Break-Even-Stop wenn aktiviert.
- **Stops**: Optionaler Break-Even-Stop.
- **Standardwerte**:
  - `FastAtrLength` = 10
  - `FastMultiplier` = 1
  - `MediumAtrLength` = 11
  - `MediumMultiplier` = 2
  - `SlowAtrLength` = 12
  - `SlowMultiplier` = 3
  - `UseHighestOfTwoRedCandles` = False
  - `UseEntryStopLoss` = True
  - `UseAllDowntrendExit` = True
  - `UseAvgPriceInLoss` = True
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: SuperTrend
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
