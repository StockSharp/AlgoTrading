# MOC Delta MOO Entry v2 Reverse-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kehrt die klassische MOC Delta MOO Entry-Logik um. Sie misst das Kauf-Verkauf-Volumen-Delta in der Nachmittagssitzung (14:50–14:55) und speichert das Delta als Prozentsatz des Tagesvolumens. Am nächsten Morgen um 08:30 wird eine Position in der entgegengesetzten Richtung des Deltas eröffnet, wenn es einen Schwellenwert überschreitet, gefiltert durch zwei gleitende Durchschnitte. Positionen werden mit tick-basiertem Take-Profit und Stop-Loss oder um 14:50 geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Um 08:30, wenn der gespeicherte Delta-Prozentsatz unter `-DeltaThreshold` liegt und der Eröffnungspreis oberhalb von SMA15 und SMA30 liegt, wobei SMA15 über SMA30 liegt.
  - **Short**: Um 08:30, wenn der gespeicherte Delta-Prozentsatz über `DeltaThreshold` liegt und der Eröffnungspreis unterhalb von SMA15 und SMA30 liegt, wobei SMA15 unter SMA30 liegt.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Take-Profit und Stop-Loss in Ticks.
  - Schließung aller offenen Positionen um 14:50.
- **Stops**:
  - `TpTicks` = 20 Ticks Take-Profit.
  - `SlTicks` = 10 Ticks Stop-Loss.
- **Standardwerte**:
  - `DeltaThreshold` = 2
  - `TpTicks` = 20
  - `SlTicks` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
