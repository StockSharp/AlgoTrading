# TCPivot-Sitzungsstoppstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die TCPivot Session Stop-Strategie ist eine direkte Portierung des MetaTrader 4 Expert Advisors `gpfTCPivotStop`. Der Handel erfolgt um das klassische tägliche Pivot-Level, das vom vorherigen Handelstag berechnet wird. Die Strategie:

- Berechnet den Drehpunkt, drei Widerstands- und drei Unterstützungsniveaus aus dem Hoch, Tief und Schluss des Vortages.
- Wartet darauf, dass der aktuelle Schlusskurs das Pivot-Level von unten (Long-Setup) oder von oben (Short-Setup) kreuzt.
- Eröffnet eine Marktposition in Richtung des Ausbruchs und weist einen Stop-Loss und einen Take-Profit auf der ausgewählten Pivot-Ebene zu.
- Erzwingt optional das Schließen der Position zu Beginn einer bestimmten Sitzungsstunde, um den ursprünglichen Intraday-Ausstieg zu emulieren.

Die Implementierung basiert auf dem High-Level StockSharp API. Die Größe der Positionen wird mit der Eigenschaft `Volume` der Basisklasse `Strategy` bestimmt.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `TargetLevel` | Pivot-Stufe für Stop-Loss und Take-Profit (1, 2 oder 3). | `1` |
| `CloseAtSessionStart` | Wenn aktiviert, werden offene Positionen geschlossen, wenn die konfigurierte Stunde beginnt. | `false` |
| `SessionCloseHour` | Sitzungsstunde (0–23), ausgewertet, wenn `CloseAtSessionStart` aktiviert ist. | `0` |
| `CandleType` | Zeitrahmen, der die Handelssignale speist. | `H1` |

## Handelslogik

1. Abonnieren Sie stündliche (oder konfigurierte) Kerzen für Signale und tägliche Kerzen für die Pivot-Berechnung.
2. Berechnen Sie nach Abschluss jeder täglichen Kerze die klassischen Pivot-Levels:
   - `Pivot = (High + Low + Close) / 3`
   - `R1 = 2 * Pivot - Low`, `S1 = 2 * Pivot - High`
   - `R2 = Pivot + (R1 - S1)`, `S2 = Pivot - (R1 - S1)`
   - `R3 = High + 2 * (Pivot - Low)`, `S3 = Low - 2 * (High - Pivot)`
3. Wenn eine Signalkerze endet:
   - Wenn `CloseAtSessionStart` aktiviert ist und die Kerze bei `SessionCloseHour` öffnet, wird die Position sofort abgeflacht.
   - Wenn flach und der vorherige Schlusskurs unter dem Pivot lag, während der aktuelle Schlusskurs darüber liegt, geben Sie eine Long-Position ein, wobei das Ziel/der Stopp durch `TargetLevel` ausgewählt wird.
   - Wenn flach und der vorherige Schlusskurs über dem Pivot lag, während der aktuelle Schlusskurs darunter liegt, gehen Sie mit dem gespiegelten Ziel/Stopp in den Short-Kurs.
   - Wenn Sie sich bereits in einer Position befinden, beenden Sie die Position, wenn der Schlusskurs das konfigurierte Stop-Loss- oder Take-Profit-Niveau erreicht.

## Notizen

- Die Strategie nutzt `StartProtection()` zur Integration in die integrierten Risikokontrollen der Plattform. Stop-Loss- und Take-Profit-Exits werden innerhalb der Strategielogik explizit behandelt.
- Die MetaTrader-Version enthielt optionale E-Mail-Benachrichtigungen und dynamische Positionsgrößenbestimmung basierend auf dem Kontorisiko. Diese Funktionen sind nicht Teil des StockSharp-Ports; Nutzen Sie bei Bedarf die Benachrichtigungs- und Geldverwaltungsmodule der Plattform.
- Der ursprüngliche Fachberater schloss Geschäfte um Mitternacht ab, als `isTradeDay` aktiviert wurde. Dieses Verhalten wird durch `CloseAtSessionStart` + `SessionCloseHour` reproduziert (auf `0` gesetzt, um Mitternacht nachzuahmen).
