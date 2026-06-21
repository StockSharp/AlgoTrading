# Nadaraya-Watson-Envelope-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erstellt Nadaraya-Watson-Kernregressionsumschläge in logarithmischer Skala. Geht Long, wenn der Kurs den unteren Umschlag nach oben kreuzt, und optional Short, wenn der Kurs den oberen Umschlag nach unten kreuzt.

## Details

- **Einstiegskriterien**:
  - Long, wenn der Schlusskurs den unteren Umschlag nach oben kreuzt.
  - Short, wenn der Schlusskurs den oberen Umschlag nach unten kreuzt (im Long/Short-Modus).
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Gegenläufiger Umschlag-Kreuzung.
- **Stops**: Nein.
- **Standardwerte**:
  - `LookbackWindow` = 8
  - `RelativeWeighting` = 8
  - `StartRegressionBar` = 25
  - `StrategyType` = Long Only
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Envelope
  - Richtung: Konfigurierbar
  - Indikatoren: Nadaraya-Watson
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
