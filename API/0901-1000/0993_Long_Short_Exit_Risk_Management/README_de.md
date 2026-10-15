# Long/Short-Ausstieg Risikomanagement-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vorlagen-Strategie, die zeigt, wie Long- und Short-Positionen mit prozentualen Risikokontrollen verwaltet werden. Es werden einfache Preis-Gleichheits-Trigger und optionale Zeitausstiege verwendet.

## Details

- **Einstiegskriterien**: Schlusskurs entspricht dem konfigurierten Long- oder Short-Wert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder zeitbasierter Ausstieg nach N Kerzen.
- **Stops**: Prozentualer Stop-Loss und Take-Profit mit optionalem Trailing.
- **Standardwerte**:
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 3
  - `ExitBars` = 10
  - `BarsToWait` = 10
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
