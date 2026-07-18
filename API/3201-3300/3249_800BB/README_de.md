# 800BB-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader 4-Expertenberater "800BB" unter Verwendung der High-Level-API von StockSharp. Sie geht Mean-Reversion-Trades ein, wenn der Preis ein sehr langes Bollinger Band durchbricht und auf der nächsten Kerze sofort wieder in den Kanal eintritt. Das Risiko wird über ATR-basierte Stop- und Take-Profit-Abstände in Kombination mit dynamischer Positionsgrößenberechnung basierend auf dem konfigurierten Risikoprozentsatz kontrolliert.

## Überblick

- Funktioniert auf jedem Instrument und Zeitrahmen, der über den `CandleType`-Parameter bereitgestellt wird.
- Verwendet ein 800-Perioden-Bollinger-Band mit einem Zwei-Standardabweichungs-Umschlag zur Erkennung extremer Ausschläge.
- Bestätigt Einstiege auf der Kerze, die direkt nach einem Außenschluss wieder innerhalb des Bandes öffnet.
- Bemisst Aufträge durch Schätzung der ATR-abgeleiteten Stop-Distanz in Pips und Anwendung des ausgewählten `RiskPercent` auf den aktuellen Portfoliowert.
- Repliziert MetaTraders Pip-Berechnung durch Multiplikation des Preisschritts mit 10, wenn das Symbol 3 oder 5 Dezimalstellen hat.

## Handelslogik

### Long-Setup

1. Die vorherige abgeschlossene Kerze öffnete oder schloss unterhalb des unteren Bollinger-Bandes, was einen überverkauften Ausschlag signalisiert.
2. Die aktuelle Kerze öffnet auf oder über dem vorherigen unteren Bandniveau (Preis ist wieder in den Kanal eingetreten).
3. Keine Long-Position ist derzeit aktiv. Offene Short-Positionen werden geschlossen, bevor die neue Long-Position eröffnet wird.
4. Die Positionsgröße wird anhand der ATR-basierten Stop-Distanz und dem konfigurierten Risikoprozentsatz berechnet.
5. Eine Market-Kauforder wird bei der Kerzenöffnung eingereicht. Der Stop-Loss wird `StopLossAtrMultiplier × ATR` unterhalb des Einstiegs platziert, während der Take-Profit `TakeProfitAtrMultiplier × ATR` oberhalb des Einstiegs liegt.

### Short-Setup

1. Die vorherige abgeschlossene Kerze öffnete oder schloss oberhalb des oberen Bollinger-Bandes, was einen überkauften Ausschlag signalisiert.
2. Die aktuelle Kerze öffnet auf oder unter dem vorherigen oberen Bandniveau (Preis ist wieder in den Kanal eingetreten).
3. Keine Short-Position ist derzeit aktiv. Offene Long-Positionen werden geschlossen, bevor die neue Short-Position eröffnet wird.
4. Die Positionsgröße wird durch dieselbe ATR-und-Risikoprozent-Berechnung bestimmt.
5. Eine Market-Verkaufsorder wird bei der Kerzenöffnung eingereicht. Der Stop-Loss wird `StopLossAtrMultiplier × ATR` oberhalb des Einstiegs platziert, während der Take-Profit `TakeProfitAtrMultiplier × ATR` unterhalb des Einstiegs liegt.

### Exit-Management

- **Schutzorders:** Stop-Loss- und Take-Profit-Niveaus werden intern verfolgt und bei jeder abgeschlossenen Kerze bewertet. Wenn einer der Schwellenwerte überschritten wird, wird die Position zum Marktpreis geschlossen.
- **Entgegengesetzte Signale:** Wenn ein entgegengesetztes Setup ausgelöst wird, wird die aktuelle Position geflacht, bevor die neue Order platziert wird.
- **Visualisierung:** Der ursprüngliche EA konnte vertikale Linien für potenzielle Trades zeichnen. Diagrammanmerkungen werden hier nicht neu erstellt; stattdessen zeichnet die Strategie Kerzen, das Bollinger-Band und eigene Trades, wenn ein Diagrammbereich verfügbar ist.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `RiskPercent` | `2` | Prozentsatz des Portfoliowerts, der pro Trade riskiert wird. |
| `TakeProfitAtrMultiplier` | `1.5` | ATR-Vielfaches zur Berechnung der Take-Profit-Distanz. |
| `StopLossAtrMultiplier` | `1` | ATR-Vielfaches zur Berechnung der Stop-Loss-Distanz. |
| `AtrPeriod` | `14` | Rückblickperiode für den ATR-Indikator. |
| `BollingerPeriod` | `800` | Periode des gleitenden Durchschnitts des Bollinger-Bandes. |
| `BollingerDeviation` | `2` | Standardabweichungsmultiplikator für das Bollinger-Band. |
| `CandleType` | `1 hour` | Zeitrahmen (oder ein anderer Kerzentyp) für die Signalerzeugung. |

## Hinweise

- Stellen Sie sicher, dass der Portfolio-Adapter `Portfolio.CurrentValue` liefert; andernfalls gibt die risikobasierte Positionsgrößenberechnung null zurück und die Strategie wird nicht handeln.
- Wenn das Symbol keinen gültigen Preisschritt oder Tick-Wert bereitstellt, greifen die Pip- und Geld-pro-Pip-Berechnungen auf konservative Standardwerte zurück.
- Der lange Bollinger-Rückblick (800 Kerzen) bedeutet, dass der erste Trade erst erfolgen kann, wenn genügend historische Daten empfangen wurden, um sowohl den Bollinger- als auch den ATR-Indikator aufzuwärmen.
