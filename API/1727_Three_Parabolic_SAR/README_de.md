# Drei Parabolic SAR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Drei Parabolic SAR-Strategie verwendet drei Parabolic SAR-Indikatoren, die auf 6-Stunden-, 3-Stunden- und 1-Stunden-Kerzen berechnet werden. Ein Trade wird auf dem 1-Stunden-Zeitrahmen eröffnet, wenn die beiden höheren Zeitrahmen die Richtung bestätigen und der 1-Stunden-SAR kippt.

## Details

- **Einstiegskriterien**:
  - SAR auf 6h-Kerzen ist unter dem Schlusskurs und SAR auf 3h-Kerzen unter dem Schlusskurs für Long; darüber für Short.
  - Auf 1h-Kerzen kreuzt der SAR den Preis: von oben nach unten für Long, von unten nach oben für Short.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Position wird geschlossen, wenn der 1h-SAR gegen die Position dreht oder wenn einer der höheren Zeitrahmen-SARs umkehrt.
- **Stops**: Nein.
- **Standardwerte**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `HigherTimeframe` = TimeSpan.FromHours(6)
  - `MiddleTimeframe` = TimeSpan.FromHours(3)
  - `TradingTimeframe` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
