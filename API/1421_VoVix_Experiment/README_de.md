# VoVix-Experiment-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie analysiert das Verhältnis von schnellem ATR zu langsamem ATR. Wenn der Z-Score dieses Verhältnisses ansteigt und ein lokales Maximum erreicht, wird in Richtung der Kerze eingestiegen. Positionen werden geschlossen, wenn der Z-Score unter den Ausstiegsschwellenwert fällt.

## Details

- **Einstiegskriterien**: VoVix Z-Score über `EntryZ` und am lokalen Maximum
- **Long/Short**: Beide
- **Ausstiegskriterien**: VoVix Z-Score unter `ExitZ`
- **Stops**: Nein
- **Standardwerte**:
  - `FastAtrLength` = 13
  - `SlowAtrLength` = 26
  - `ZScoreWindow` = 81
  - `EntryZ` = 1.0
  - `ExitZ` = 1.4
  - `LocalMaxWindow` = 6
  - `SuperZ` = 2.0
  - `MinVolume` = 1
  - `MaxVolume` = 2
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: ATR, Highest, SMA, StdDev
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
