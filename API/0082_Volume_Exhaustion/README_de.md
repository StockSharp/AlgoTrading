# Volumen-Erschöpfungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Starke Volumenspitzen signalisieren oft das Ende einer Bewegung, wenn Trader hastig Positionen schließen oder eröffnen. Diese Strategie misst das aktuelle Volumen im Vergleich zu einem Durchschnitt, um Erschöpfung zu erkennen. In Kombination mit der Kerzenrichtung und einem gleitenden Durchschnittsfilter können Umkehreinsteigspunkte präzise identifiziert werden.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 133%. Am besten funktioniert die Strategie auf dem Kryptomarkt.

Jede Kerze aktualisiert das durchschnittliche Volumen. Wenn das Volumen des neuen Balkens diesen Durchschnitt um einen festgelegten Multiplikator überschreitet und die Kerze in der entgegengesetzten Richtung des vorherrschenden Trends schließt, eröffnet das System einen Trade. Ein ATR-basierter Stop schützt die Position.

Der Trade wird typischerweise über den Stop-Loss beendet, da die Strategie eine schnelle Umkehr nach dem Volumenausbruch erwartet.

## Details

- **Einstiegskriterien**: Volumenspitze über dem Durchschnitt mit Kerze entgegen dem Trend.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss.
- **Stops**: Ja, ATR-basiert.
- **Standardwerte**:
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 2.0
  - `MAPeriod` = 20
  - `AtrMultiplier` = 2 ATR
  - `CandleType` = 5 minute
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Volume, MA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

