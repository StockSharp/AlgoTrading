# I4 DRF-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem benutzerdefinierten I4 DRF-Indikator. Er vergleicht die Richtung der letzten Kerzen-Hochs und -Tiefs und erzeugt einen Wert zwischen -100 und +100. Handelsaktionen hängen von Farbübergängen dieses Indikators und dem ausgewählten Modus ab.

## Details

- **Einstiegskriterien**:
  - Modus `Direct`: Long öffnen, wenn der Indikator von positiv auf negativ wechselt; Short öffnen, wenn er von negativ auf positiv wechselt.
  - Modus `NotDirect`: Long bei einem Wechsel von negativ auf positiv öffnen; Short bei einem Wechsel von positiv auf negativ öffnen.
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Positionen werden geschlossen, wenn das entgegengesetzte Signal erscheint.
- **Stops**: Keine
- **Standardwerte**:
  - `Period` = 11
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: I4 DRF
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
