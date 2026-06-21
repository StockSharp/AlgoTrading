# My Line Order
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie löst Marktorders aus, wenn der Preis vordefinierte horizontale Niveaus kreuzt. Der Benutzer legt separate Niveaus für Long- und Short-Einstiege sowie Risikoparameter in Pips fest. Nach dem Öffnen einer Position verfolgt die Strategie Stop-Loss, Take-Profit und einen optionalen Trailing Stop.

Das System eignet sich für diskretionäre Setups, bei denen die Einstiegsniveaus im Voraus bekannt sind. Es funktioniert mit jedem Instrument und Zeitrahmen, da es nur auf Preisniveaus basiert.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs kreuzt über `BuyPrice`.
  - **Short**: Schlusskurs kreuzt unter `SellPrice`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Stop-Loss bei `StopLossPips`.
  - Take-Profit bei `TakeProfitPips`.
  - Trailing Stop wenn `TrailingStopPips` > 0.
- **Stops**: Ja, in Pips.
- **Standardwerte**:
  - `BuyPrice` = 0 (deaktiviert)
  - `SellPrice` = 0 (deaktiviert)
  - `TakeProfitPips` = 30
  - `StopLossPips` = 20
  - `TrailingStopPips` = 0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Manuell
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
