# Quantum Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mit dem Stochastic-Oszillator. Wenn %K die überverkaufte Zone verlässt und über `LowLevel` kreuzt, wird eine Long-Position eröffnet. Wenn %K aus der überkauften Zone fällt und unter `HighLevel` kreuzt, wird eine Short-Position eröffnet. Positionen werden an extremen Schwellenwerten geschlossen, um Gewinne zu sichern.

## Details

- **Einstiegskriterien**:
  - **Long**: %K kreuzt über `LowLevel`.
  - **Short**: %K kreuzt unter `HighLevel`.
- **Ausstiegskriterien**:
  - **Long**: %K erreicht `HighCloseLevel`.
  - **Short**: %K erreicht `LowCloseLevel`.
- **Indikatoren**: Stochastic Oscillator.
- **Zeitrahmen**: Parameter `CandleType` (Standard 1 Minute).
- **Parameter**:
  - `KPeriod` – Periode der %K-Linie.
  - `DPeriod` – Periode der %D-Linie.
  - `Slowing` – Glättungsfaktor für Stochastic.
  - `HighLevel` – untere Grenze der überkauften Zone.
  - `LowLevel` – obere Grenze der überverkauften Zone.
  - `HighCloseLevel` – Niveau zum Schließen von Long-Positionen.
  - `LowCloseLevel` – Niveau zum Schließen von Short-Positionen.
