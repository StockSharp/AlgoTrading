# Aroon-Oszillator-Vorzeichenalarm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Aroon-Oszillator, um Handelssignale zu erzeugen, wenn der Oszillator vordefinierte Niveaus kreuzt. Eine Long-Position wird eröffnet, wenn der Oszillator das untere Niveau (Standard -50) nach oben kreuzt. Eine Short-Position wird eröffnet, wenn er das obere Niveau (Standard +50) nach unten kreuzt. Umgekehrte Signale schließen oder kehren die Position um.

## Details

- **Einstiegskriterien:**
  - **Long**: Der Aroon-Oszillator kreuzt das untere Niveau nach oben.
  - **Short**: Der Aroon-Oszillator kreuzt das obere Niveau nach unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Das umgekehrte Signal schließt oder kehrt automatisch die aktuelle Position um.
- **Stops**: Keine.
- **Filter**: Keine.
- **Zeitrahmen**: Standard 4-Stunden-Kerzen (konfigurierbar).

## Parameter

- `AroonPeriod` – Lookback-Periode für den Aroon-Oszillator (Standard 9).
- `UpLevel` – oberer Schwellenwert für Verkaufssignale (Standard +50).
- `DownLevel` – unterer Schwellenwert für Kaufsignale (Standard -50).
- `CandleType` – Kerzen-Zeitrahmen für Berechnungen (Standard 4 Stunden).
