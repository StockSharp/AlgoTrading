# Bullische Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die klassische bullische Umkehr-Kerzenmuster sucht. Wenn eines dieser Muster unterhalb eines 50-Perioden einfachen gleitenden Durchschnitts erscheint, eröffnet die Strategie eine Long-Position. Ein optionaler Trailing-Stop schützt offene Gewinne.

## Muster
- **Abandoned Baby** – zwei aufeinanderfolgende bärische Kerzen, gefolgt von einer bullischen Kerze, die über der Eröffnung der ersten Kerze schließt, während die zweite Kerze nach unten gappt.
- **Morning Doji Star** – eine bärische Kerze, ein Doji oder Kerze mit kleinem Körper, dann eine bullische Kerze, die wieder in den Körper der ersten Kerze schließt.
- **Three Inside Up** – eine bärische Kerze, eine kleinere bullische Kerze innerhalb ihres Bereichs, dann eine bullische Kerze, die über der Eröffnung der ersten Kerze schließt.
- **Three Outside Up** – eine bärische Kerze, gefolgt von einer größeren bullischen Kerze, die sie engulfiert, und eine dritte bullische Kerze, die die Umkehr bestätigt.
- **Three White Soldiers** – drei aufeinanderfolgende bullische Kerzen mit steigenden Schlusskursen.

## Details
- **Einstiegskriterien**: beliebiges gelistetes Muster und die letzte Kerze eröffnete unter dem gleitenden Durchschnitt
- **Long/Short**: Long
- **Ausstiegskriterien**: optionaler Trailing-Stop
- **Stops**: Trailing
- **Standardwerte**:
  - `MaPeriod` = 50
  - `TrailingStop` = 50
  - `UseTrailingStop` = true
- **Filter**:
  - Kategorie: Muster
  - Richtung: Nur Long
  - Indikatoren: SMA
  - Stops: Trailing
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
