# MOC Delta MOO Entry v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erfasst das Kauf- und Verkaufsvolumen während der Nachmittagssitzung und nutzt das resultierende MOC-Delta, um den Eröffnungskurs des nächsten Tages zu handeln.

Von 14:50 bis 14:55 sammelt sie Hochs, Tiefs und getrenntes Kauf-/Verkaufsvolumen. Um 14:55 berechnet sie den Delta-Prozentsatz von Kauf minus Verkauf relativ zum gesamten Tagesvolumen. Am nächsten Tag um 8:30 wird eine Long-Position eröffnet, wenn das Delta über dem Schwellenwert liegt und die Eröffnung oberhalb der 15- und 30-Perioden-SMAs liegt. Eine Short-Position verwendet die entgegengesetzten Bedingungen. Positionen umfassen tick-basierten Take-Profit und Stop-Loss und werden um 14:50 geschlossen.

## Details

- **Einstiegskriterien**: Um 8:30, Delta-Prozentsatz über Schwellenwert und Preis über SMA15 und SMA30 für Long; Delta unter negativem Schwellenwert und Preis unter den SMAs für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss; alle Positionen werden um 14:50 geschlossen.
- **Stops**: Ja.
- **Standardwerte**:
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `DeltaThreshold` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Volumen, SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
