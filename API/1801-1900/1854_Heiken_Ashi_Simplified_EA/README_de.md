# Heiken Ashi Simplified EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein musterbasiertetes System, das auf Heikin Ashi-Kerzen aufbaut. Die Strategie beobachtet eine Sequenz vorheriger Heikin Ashi-Eröffnungen und -Schlusskurse. Wenn drei aufeinanderfolgende Schlusskurse über ihre jeweiligen Eröffnungen steigen (oder fallen), während die Eröffnungen eine decelerating Pullback bilden, kann die nächste Kerze einen Ausbruchstrade auslösen, sobald sich der Kurs um eine Mindestdistanz von der letzten Heikin Ashi-Eröffnung entfernt. Der Algorithmus skaliert Positionen bis zu einem definierten Limit.

## Details

- **Einstiegskriterien**:
  - **Long**: Drei vorherige HA-Schlusskurse liegen über früheren Eröffnungen und die Eröffnungen bilden eine absteigende Reihe mit schrumpfenden Differenzen.
  - **Short**: Drei vorherige HA-Schlusskurse liegen unter früheren Eröffnungen und die Eröffnungen bilden eine aufsteigende Reihe mit expandierenden Differenzen.
- **Long/Short**: Beide Seiten
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `CandleType` = 1 Stunde
  - `MaxPositions` = 3
  - `DistancePoints` = 300
  - `Volume` = 1
- **Filter**:
  - Kategorie: Muster-Ausbruch
  - Richtung: Beide
  - Indikatoren: Heikin Ashi
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Stündlich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
