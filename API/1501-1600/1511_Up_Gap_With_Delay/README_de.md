# Up-Gap-Strategie mit Verzögerung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn die Sitzung mit einer Aufwärtslücke über einem Schwellenwert öffnet und eine bestimmte Anzahl von Bars seit dem vorherigen Einstieg vergangen ist. Die Position wird für eine feste Anzahl von Bars gehalten.

## Details

- **Einstiegskriterien**: Aufwärtslücke größer als Schwellenwert und Verzögerungsperiode erfüllt
- **Long/Short**: Long
- **Ausstiegskriterien**: nach Ablauf der Halteperiode
- **Stops**: Nein
- **Standardwerte**:
  - `GapThreshold` = 1
  - `DelayPeriods` = 0
  - `HoldingPeriods` = 7
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
