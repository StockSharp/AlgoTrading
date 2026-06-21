# MH Hull Moving Average Handels-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie auf Basis des Hull Moving Average.

Die Strategie vergleicht den Eröffnungspreis mit dynamischen Niveaus, die aus dem Hull Moving Average abgeleitet werden. Sie geht long, wenn der Preis über das obere Niveau bricht, und short, wenn er unter das untere Niveau fällt. Bestehende Positionen werden bei entgegengesetzten Ausbrüchen geschlossen.

## Details

- **Einstiegskriterien**: Preisbeziehung zu den Hull Moving Average Niveaus.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Ausbruch.
- **Stops**: Keine.
- **Standardwerte**:
  - `HullPeriod` = 210
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
