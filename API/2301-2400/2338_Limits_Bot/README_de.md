# Limits-Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Platziert symmetrische Limit-Orders um den Eröffnungskurs jeder Kerze und schützt Positionen mit Stop-Loss, Take-Profit und optionalem Trailing.

## Details

- **Einstieg**:
  - Kauf-Limit bei `Open - StopOrderDistance * PriceStep`, wenn Long-Handel aktiviert.
  - Verkauf-Limit bei `Open + StopOrderDistance * PriceStep`, wenn Short-Handel aktiviert.
- **Ausstieg**: Marktschluss beim Auslösen von Stop-Loss, Take-Profit oder Trailing-Stop.
- **Long/Short**: Beide.
- **Stops**: Fester Stop-Loss mit Trailing-Option.
- **Standardwerte**:
  - `StopOrderDistance` = 5
  - `TakeProfit` = 35
  - `StopLoss` = 8
  - `TrailingStart` = 40
  - `TrailingDistance` = 30
  - `TrailingStep` = 1
  - `CandleType` = 1 Minute
- **Session**: Handelt nur zwischen `StartTime` und `EndTime`.
- **Filter**:
  - Kategorie: Price action
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
