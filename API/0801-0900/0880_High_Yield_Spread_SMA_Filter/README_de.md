# High-Yield-Spread-Strategie mit SMA-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des High-Yield-Kreditspreads oder des VIX-Index. Wenn der ausgewählte Spread einen Schwellenwert überschreitet und der Preis auf der richtigen Seite eines einfachen gleitenden Durchschnitts liegt, eröffnet die Strategie eine Position in der gewählten Richtung.

Positionen werden für eine feste Anzahl von Kerzen gehalten und dann geschlossen. Die Strategie arbeitet ausschließlich mit Tageskerzen.

## Details

- **Einstiegskriterien**:
  - **Long**: Spread > Schwellenwert und (Preis > SMA wenn Filter aktiviert)
  - **Short**: Spread < Schwellenwert und (Preis < SMA wenn Filter aktiviert)
- **Long/Short**: jeweils eine Seite (Parameter)
- **Ausstiegskriterien**: Position nach Halteperiode geschlossen
- **Stops**: Nein
- **Standardwerte**:
  - `Basis` = HighYieldSpread
  - `Threshold` = 5
  - `IsLong` = true
  - `HoldingPeriod` = 5
  - `UseSmaFilter` = true
  - `SmaLength` = 50
  - `CandleType` = 1 day
- **Filter**:
  - Kategorie: Spread
  - Richtung: Konfigurierbar
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
