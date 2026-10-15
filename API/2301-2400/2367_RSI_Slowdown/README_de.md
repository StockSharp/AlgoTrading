# RSI-Verlangsamungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die RSI-Verlangsamungs-Strategie reagiert auf extreme Messwerte des Relative Strength Index, die Anzeichen nachlassenden Momentums zeigen. Wenn sich RSI den überkauften oder überverkauften Zonen nähert und seine Veränderung zwischen den Bars unter einen Punkt fällt, geht die Strategie davon aus, dass der Markt für eine Umkehr bereit ist.

Eine Long-Position wird eröffnet, wenn RSI das obere Niveau erreicht oder überschreitet und das Wachstum des Indikators nachlässt. Eine Short-Position wird eröffnet, wenn RSI auf das untere Niveau fällt, mit einer ähnlichen Verlangsamung. Jede bestehende entgegengesetzte Position wird geschlossen, bevor ein neuer Trade eingegangen wird.

Die Standardkonfiguration verwendet 6-Stunden-Kerzen und einen 2-Perioden-RSI mit Schwellenwerten von 90 und 10. Diese Werte imitieren die ursprüngliche MetaTrader-Implementierung.

## Details
- **Einstiegskriterien**:
  - **Long**: RSI >= `LevelMax` und `|RSI - prev RSI| < 1` (wenn Verlangsamung aktiviert ist)
  - **Short**: RSI <= `LevelMin` und `|RSI - prev RSI| < 1` (wenn Verlangsamung aktiviert ist)
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - **Long**: Gegenteiliges Signal oder Short-Einstieg.
  - **Short**: Gegenteiliges Signal oder Long-Einstieg.
- **Stops**: Keine automatischen Stops.
- **Standardwerte**:
  - `RsiPeriod` = 2
  - `LevelMax` = 90
  - `LevelMin` = 10
  - `SeekSlowdown` = true
  - `CandleType` = `TimeSpan.FromHours(6)`
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday bis Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja (Verlangsamung)
  - Risikolevel: Mittel
