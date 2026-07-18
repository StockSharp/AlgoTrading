# Grid Tendence V1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grid-Handelsstrategie, die Positionen basierend auf prozentualen Gewinnschritten erneut öffnet oder umkehrt.

Sie startet Long, und wenn der Gewinn den angegebenen Prozentsatz erreicht, wird sie geschlossen und in dieselbe Richtung erneut eröffnet. Wenn der Verlust den Prozentsatz erreicht, wird sie geschlossen und in die entgegengesetzte Richtung eröffnet.

## Details

- **Einstiegskriterien**: Immer im Markt, beginnend Long. Erneut öffnen oder umkehren, wenn Gewinn oder Verlust `Percent` erreicht.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gewinn- oder Verlustschwelle.
- **Stops**: Nein.
- **Standardwerte**:
  - `Percent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Grid
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
