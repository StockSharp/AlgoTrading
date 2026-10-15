# Automatisierte Bollinger Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Kauf-Limit-Orders am unteren Bollinger Band und Verkauf-Limit-Orders am oberen Band platziert. Positionen werden geschlossen, wenn der Preis das mittlere Band berührt. Ausstehende Orders werden zu Beginn jeder Kerze aktualisiert.

## Details

- **Einstiegskriterien**:
  - Long: Kauf-Limit am unteren Bollinger Band
  - Short: Verkauf-Limit am oberen Bollinger Band
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: Preis kreuzt über das mittlere Bollinger Band
  - Short: Preis kreuzt unter das mittlere Bollinger Band
- **Stops**: Keine
- **Standardwerte**:
  - `BbPeriod` = 20
  - `BbDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
