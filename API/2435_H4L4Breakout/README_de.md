# H4L4 Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tägliche Ausbruchsstrategie, die H4- und L4-Levels aus dem Hoch, Tief und Schlusskurs des Vortages berechnet.
Zu Beginn jedes Tages wird ein Sell-Limit bei H4 und ein Buy-Limit bei L4 platziert.
Alle offenen Positionen und ausstehenden Orders werden vor der Eingabe neuer Orders geschlossen.
Schützender Stop-Loss und Take-Profit werden anhand von Tick-basierten Distanzen gesetzt.

## Details

- **Einstiegskriterien**: Sell-Limit bei H4 und Buy-Limit bei L4, abgeleitet aus der Kerze des Vortages.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `TakeProfit` = 57
  - `StopLoss` = 7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
