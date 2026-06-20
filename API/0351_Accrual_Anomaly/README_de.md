# Strategie der Abgrenzungsanomalie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Accrual Anomaly**-Strategie implementiert den Abgrenzungsanomalie-Faktor. Sie wird jährlich am ersten Handelstag im Mai neu gewichtet, wobei Aktien mit niedrigen Abgrenzungen long und Aktien mit hohen Abgrenzungen short gegangen wird.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 12%. Sie funktioniert am besten auf dem US-Aktienmarkt.

Positionen werden einmal pro Jahr angepasst; es werden keine Intraday-Signale verwendet.

## Details
- **Einstiegskriterien**: siehe Implementierung für Abgrenzungsberechnungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Neugewichtung am nächsten geplanten Datum.
- **Stops**: Keine explizite Stop-Logik.
- **Standardwerte**:
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Fundamentals
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
