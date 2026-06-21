# Relative Währungsstärke
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Relative Währungsstärke vergleicht ein Währungspaar mit einem Korb der wichtigsten Währungen.
Sie kauft, wenn das gehandelte Paar den Durchschnitt der anderen Majors übertrifft, und verkauft, wenn es darunter liegt.
Der Vergleich basiert auf der prozentualen Veränderung seit Beginn der Sitzung.

## Details

- **Einstiegskriterien**: Stärke des Hauptpaares übersteigt den Durchschnitt um den Schwellenwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stärke fällt unter den Durchschnitt um den Schwellenwert.
- **Stops**: Nein.
- **Standardwerte**:
  - `Threshold` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Preisveränderung
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
