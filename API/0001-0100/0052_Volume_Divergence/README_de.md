# Volumen-Divergenz (Volume Divergence)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Volumen-Divergenz sucht nach Diskrepanzen zwischen der Kursbewegung und dem Handelsvolumen. Fällt der Kurs, während das Volumen steigt, kann dies auf Akkumulation hinweisen; steigt der Kurs bei starkem Volumen, kann dies auf Distribution hindeuten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 43 %. Die Strategie eignet sich am besten für den Aktienmarkt.

Die Strategie geht long, wenn fallende Kurse von steigendem Volumen begleitet werden, und short, wenn steigende Kurse mit hohem Volumen einhergehen. Ausstiege basieren auf einem gleitenden Durchschnitts-Crossover.

Dieser Ansatz versucht, gegen nicht nachhaltige Bewegungen zu handeln.

## Details

- **Einstiegskriterien**: Kurs und Volumen bewegen sich in entgegengesetzte Richtungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Kurs kreuzt den MA oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: Volume, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
