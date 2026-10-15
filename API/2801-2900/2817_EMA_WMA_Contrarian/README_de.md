# EMA WMA Contrarian-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kontrar-Kreuzungssystem, das einen exponentiellen gleitenden Durchschnitt (EMA) und einen gewichteten gleitenden Durchschnitt (WMA) auf Kerzen-Eröffnungspreisen vergleicht. Wenn die schnelle EMA unter die WMA fällt, kauft die Strategie und wettet auf einen Rückprall. Wenn die EMA wieder über die WMA steigt, geht sie Short. Die Handelsgröße wird aus dem konfigurierten Risikoprozentsatz und der Distanz zum Schutz-Stop abgeleitet, während optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Niveaus die Exposition unter Kontrolle halten.

## Details

- **Einstiegskriterien**:
  - Long: EMA(Open) kreuzt von oben nach unten unter die WMA(Open)
  - Short: EMA(Open) kreuzt von unten nach oben über die WMA(Open)
- **Long/Short**: Beide Richtungen
- **Ausstiegskriterien**:
  - Fester Stop-Loss in Preisschritten
  - Festes Take-Profit in Preisschritten
  - Trailing-Stop, der nach einer Preisbewegung von `TrailingStopPoints + TrailingStepPoints` vorrückt
  - Entgegengesetzter Kreuzung schließt die aktuelle Position und öffnet die neue
- **Stops**: Stop-Loss, Take-Profit und Trailing-Stop
- **Standardwerte**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossPoints` = 50m
  - `TakeProfitPoints` = 50m
  - `TrailingStopPoints` = 50m
  - `TrailingStepPoints` = 10m
  - `RiskPercent` = 10m
  - `BaseVolume` = 1m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Gleitender Durchschnitt, Contrarian
  - Richtung: Long & Short
  - Indikatoren: EMA (Open), WMA (Open)
  - Stops: Ja (harter Stop, Trailing)
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (Standard 1 Minute)
  - Saisonalität: Keine
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `EmaPeriod`, `WmaPeriod` | Rückblickperioden für EMA und WMA, berechnet auf Kerzen-Eröffnungen. |
| `StopLossPoints`, `TakeProfitPoints` | Abstand in Preisschritten zum Platzieren des Schutz-Stops und Gewinnziels. |
| `TrailingStopPoints` | Abstand zwischen Preis und Trailing-Stop nach Aktivierung. |
| `TrailingStepPoints` | Zusätzliche günstige Bewegung erforderlich, bevor der Trailing-Stop hoch/runter gezogen wird. Muss positiv sein, wenn Trailing aktiviert ist. |
| `RiskPercent` | Prozentsatz des Portfolio-Kapitals, der pro Trade riskiert wird. Positionsgröße wird berechnet als `RiskPercent / (StopLossPoints * PriceStep)`. |
| `BaseVolume` | Mindest-Handelsgröße, die verwendet wird, wenn risikobasiertes Sizing nicht bestimmt werden kann. |
| `CandleType` | Kerzendatentyp für Berechnungen (Standard 1 Minute). |

## Hinweise

- Beide gleitenden Durchschnitte verbrauchen Kerzen-Eröffnungspreise und spiegeln den ursprünglichen MetaTrader-Expertenberater wider.
- Trailing-Stops greifen erst, nachdem der Preis mindestens `TrailingStopPoints + TrailingStepPoints` zugunsten des Trades bewegt hat, was die Legacy-Logik repliziert.
- Wenn `TrailingStopPoints` gesetzt ist, während `TrailingStepPoints` null oder negativ ist, stoppt die Strategie sofort, um inkonsistentes Trailing-Verhalten zu vermeiden.
- Risikobasiertes Sizing fällt auf `BaseVolume` zurück, wenn der Portfolio-Wert, Preisschritt oder Stop-Distanz nicht verfügbar sind.
