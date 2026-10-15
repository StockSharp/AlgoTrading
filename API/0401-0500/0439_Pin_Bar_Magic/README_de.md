# Pin Bar Magic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erkennt bullische und bärische Pin Bars innerhalb eines Trends, der durch ein Trio von gleitenden Durchschnitten definiert wird. Aufträge werden an den Kerzenextremen platziert und nach einigen Kerzen storniert, wenn sie nicht ausgeführt werden. Die Positionsgröße wird aus einem prozentualen Eigenkapitalrisiko und dem ATR-basierten Stop-Abstand berechnet.

Die Methode zielt darauf ab, scharfe Umkehrungen an bedeutenden Unterstützungs- oder Widerstandsniveaus zu erfassen. Positionen werden beendet, wenn die schnelle und mittlere EMA in die entgegengesetzte Richtung kreuzen, was eine Trendschwäche signalisiert.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle EMA > Mittlere EMA > Langsame SMA, bullischer Pin Bar, der eine der Durchschnitte durchsticht.
  - **Short**: Schnelle EMA < Mittlere EMA < Langsame SMA, bärischer Pin Bar, der eine der Durchschnitte durchsticht.
- **Ausstiegskriterien**:
  - Schnelle EMA kreuzt die mittlere EMA in die entgegengesetzte Richtung.
- **Indikatoren**:
  - Langsame SMA (Periode 50)
  - Mittlere EMA (18) und schnelle EMA (6)
  - ATR (Länge 14)
- **Stops**: Positionsrisiko = EquityRisk% des Kontos mit Stop bei ATR * Multiplikator.
- **Standardwerte**:
  - `EquityRisk` = 3
  - `AtrMultiplier` = 0.5
  - `SlowSmaLength` = 50
  - `MediumEmaLength` = 18
  - `FastEmaLength` = 6
  - `AtrLength` = 14
  - `CancelEntryBars` = 3
- **Filter**:
  - Kursaktions-Umkehr
  - Funktioniert standardmäßig auf 1-Stunden-Kerzen
  - Indikatoren: EMA, SMA, ATR
  - Stops: Ja
  - Komplexität: Hoch
