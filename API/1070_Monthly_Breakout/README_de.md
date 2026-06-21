# Monatlicher Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt Ausbrüche des monatlichen Hochs oder Tiefs nur während ausgewählter Kalendermonate. Die Richtung wird über `EntryOption` gewählt, und Positionen werden nach einer festen Anzahl von Kerzen geschlossen.

## Details

- **Einstiegskriterien**:
  - Abhängig von `EntryOption` und den ausgewählten Monaten (z. B. Long, wenn der Schlusskurs das Monatshoch übersteigt).
- **Long/Short**: Konfigurierbar.
- **Ausstiegskriterien**: Schließen nach `HoldingPeriod` Kerzen.
- **Stops**: Nein.
- **Standardwerte**:
  - `EntryOption` = LongAtHigh
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Konfigurierbar
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
