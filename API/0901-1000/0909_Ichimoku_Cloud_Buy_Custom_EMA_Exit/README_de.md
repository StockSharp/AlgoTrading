# Ichimoku Cloud Kauf mit Benutzerdefiniertem EMA-Ausstieg Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementierung der Strategie: Ichimoku Cloud Kauf mit benutzerdefiniertem EMA-Ausstieg und Volumenfilter. Die Strategie kauft, wenn der Preis über der Wolke liegt und das Volumen seinen Durchschnitt übersteigt. Optional kann gefordert werden, dass der Preis über dem EMA bleibt. Die Position wird geschlossen, sobald der Preis unter den EMA fällt oder der Stop-Loss ausgelöst wird.

## Details

- **Einstiegskriterien**:
  - Long: `Price > Cloud && Volume > AvgVolume && (Price > EMA if enabled)`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - `Price < EMA`
- **Stops**: Prozentbasiert über `StopLossPercent`
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `EmaLength` = 44
  - `VolumeAvgPeriod` = 10
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Ichimoku Cloud, EMA, Volumen
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
