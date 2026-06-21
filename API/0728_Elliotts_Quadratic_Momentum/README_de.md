# Elliott's Quadratic Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Elliott's Quadratic Momentum** kombiniert mehrere SuperTrend-Indikatoren, um vom Elliott-Wellen inspirierten Momentum zu erfassen.

Die Strategie geht long, wenn alle vier SuperTrend-Linien einen Aufwärtstrend signalisieren, und short, wenn alle einen Abwärtstrend signalisieren. Positionen werden geschlossen, wenn ein beliebiger SuperTrend die Richtung umkehrt.

## Details
- **Einstiegskriterien**: Alle SuperTrend-Indikatoren in dieselbe Richtung ausgerichtet.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Beliebiger SuperTrend dreht gegen die Position.
- **Stops**: Keine expliziten Stops.
- **Standardwerte**:
  - `AtrLength1 = 7`
  - `Multiplier1 = 4.0m`
  - `AtrLength2 = 14`
  - `Multiplier2 = 3.618m`
  - `AtrLength3 = 21`
  - `Multiplier3 = 3.5m`
  - `AtrLength4 = 28`
  - `Multiplier4 = 3.382m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
