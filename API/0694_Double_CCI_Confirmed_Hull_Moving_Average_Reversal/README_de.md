# Doppel-CCI-bestätigte Hull-MA-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, wenn der Preis den Hull Moving Average nach oben kreuzt, bestätigt durch schnelle und langsame CCI-Indikatoren. Ein Trailing-EMA verwaltet den Gewinn nach einer ATR-basierten Aktivierung.

Tests zeigen moderate jährliche Renditen. Am besten in gemischten Märkten.

## Details
- **Einstiegskriterien**:
  - **Long**: Preis kreuzt HMA nach oben, Schlusskurs über HMA, schneller CCI > 0, langsamer CCI > 0
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - **Long**: Schlusskurs unter Trailing-EMA nach Aktivierung oder Tief trifft ATR-Stop
- **Stops**: Ja.
- **Standardwerte**:
  - `StopLossAtrMultiplier` = 1.75
  - `TrailingActivationMultiplier` = 2.25
  - `FastCciPeriod` = 25
  - `SlowCciPeriod` = 50
  - `HullMaLength` = 34
  - `TrailingEmaLength` = 20
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Nur Long
  - Indikatoren: CCI, HMA, EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
