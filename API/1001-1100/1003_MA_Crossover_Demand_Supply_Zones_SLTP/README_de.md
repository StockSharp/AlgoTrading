# MA-Crossover-Strategie mit Angebots-/Nachfragezonen und SLTP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Crossover kurzer/langer einfacher gleitender Durchschnitte mit der Erkennung von Angebots- und Nachfragezonen. Das System sucht nach Crossovern, die in der Nähe kürzlich bestätigter Nachfrage- oder Angebotszonen auftreten, geht dann in die Richtung des Crossovers ein und verwaltet die Position mit festem prozentualen Stop-Loss und Take-Profit.

## Details

- **Einstiegskriterien**:
  - Long: Kurze SMA kreuzt die lange SMA von unten in der Nähe einer Nachfragezone.
  - Short: Kurze SMA kreuzt die lange SMA von oben in der Nähe einer Angebotszone.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Kurs erreicht Take-Profit- oder Stop-Loss-Niveaus.
- **Stops**: Prozentualer Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `ShortMaLength` = 9
  - `LongMaLength` = 21
  - `ZoneLookback` = 50
  - `ZoneStrength` = 2
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, Highest, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
