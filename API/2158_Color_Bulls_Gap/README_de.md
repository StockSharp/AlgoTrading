# Color Bulls Gap-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den ColorBullsGap-Indikator nachbildet, indem sie geglättete Lücken zwischen dem Hochpreis und Durchschnittswerten von Eröffnungs- und Schlusskurs vergleicht.
Geht long, wenn die Farbe vor zwei Balken bullisch war und auf dem letzten Balken neutral oder bärisch wird, wobei Short-Positionen geschlossen werden.
Geht short, wenn die Farbe vor zwei Balken bärisch war und auf dem letzten Balken neutral oder bullisch wird, wobei Long-Positionen geschlossen werden.

## Details

- **Einstiegskriterien**:
  - Long: `PrevColor == 0 && LastColor > 0`
  - Short: `PrevColor == 2 && LastColor < 2`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `Length1` = 12
  - `Length2` = 5
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filter**:
  - Kategorie: Indikator
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
