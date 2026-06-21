# Prozentsatz-Stop-und-Gewinnmitnahme-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei einfache gleitende Durchschnitte (SMA), um die Trendrichtung zu erkennen. Wenn der schnelle SMA den langsamen SMA nach oben kreuzt, wird eine Long-Position eröffnet. Wenn der schnelle SMA den langsamen SMA nach unten kreuzt, wird eine Short-Position eröffnet. Nach dem Einstieg setzt die Strategie Stop-Loss- und Gewinnmitnahme-Niveaus als Prozentsätze des Einstiegspreises.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller SMA kreuzt den langsamen SMA nach oben.
  - **Short**: Schneller SMA kreuzt den langsamen SMA nach unten.
- **Ausstiegskriterien**:
  - Stop-Loss und Gewinnmitnahme basierend auf Prozentsätzen des Einstiegspreises.
- **Stops**: Ja, sowohl Stop-Loss als auch Gewinnmitnahme.
- **Indikatoren**: SMA.
- **Kategorie**: Trendfolge.
- **Zeitrahmen**: Beliebig.
