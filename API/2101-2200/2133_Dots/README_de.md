# Punkte-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus MQL5 "Exp_Dots". Die Strategie handelt Umkehrungen, wenn der Dots-Indikator die Farbe wechselt.
Sie geht Long, wenn der Indikator von Blau auf Rot wechselt, und Short, wenn er von Rot auf Blau wechselt.

## Details

- **Einstiegskriterien**:
  - Long: Indikatorfarbe wechselt von Blau zu Rot.
  - Short: Indikatorfarbe wechselt von Rot zu Blau.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal
- **Stops**: Nein
- **Standardwerte**:
  - `Length` = 10
  - `Filter` = 0m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trendumkehr
  - Richtung: Beide
  - Indikatoren: Dots (NonLag Moving Average)
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: 4H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
