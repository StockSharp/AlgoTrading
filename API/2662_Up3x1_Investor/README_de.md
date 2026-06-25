# Up3x1 Investor Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Up3x1 Investor Strategie portiert den klassischen MetaTrader Expert Advisor, der auf starke Expansionskerzen reagiert. Sie beobachtet den letzten abgeschlossenen Balken im konfigurierten Zeitrahmen und eröffnet eine neue Position auf dem folgenden Balken, wenn der vorherige Kursbereich und der Kerzenkörper in Richtung des Schlusskurses weit genug waren.

Die Strategie ist für diskretionäre Märkte wie Forex-Majors auf dem H1-Chart ausgelegt, aber die Schwellenwerte können für andere Instrumente angepasst werden. Es wird immer nur eine Position gehalten und jede Order verwendet die `Volume`-Eigenschaft der Strategie als Handelsgröße.

## Handelslogik

- **Signalquelle** – abgeschlossene Zeitrahmenkerzen aus `CandleType` (Standard: 1 Stunde).
- **Einstiegskriterien**
  - Berechnung des Hoch–Tief-Bereichs und des absoluten Kerzenkörpers des vorherigen Balkens.
  - Long-Einstieg, wenn die Kerze über der Eröffnung schloss und sowohl der Bereich als auch der Körper ihre jeweiligen Pip-Schwellenwerte überschreiten.
  - Short-Einstieg, wenn die Kerze unter der Eröffnung schloss und sowohl der Bereich als auch der Körper die Schwellenwerte überschreiten.
  - Neue Einstiege werden ignoriert, solange eine Position offen ist.
- **Positionsmanagement**
  - Optionale Stop-Loss- und Take-Profit-Levels werden mit `Security.PriceStep` von Pips in Preiseinheiten umgerechnet.
  - Ein Trailing Stop aktiviert sich, sobald sich der Preis um `TrailingStopPips + TrailingStepPips` vom Einstieg entfernt hat.
  - Der Trailing Stop bewegt sich nur, wenn das neue Niveau mindestens `TrailingStepPips` näher am Preis liegt als das vorherige Trailing-Niveau.
  - Die Strategie schließt eine Position, wenn der Preis die Stop-Loss-, Take-Profit- oder Trailing-Stop-Niveaus berührt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Datentyp der für Signale verwendeten Kerzen (Standard: 1-Stunden-Zeitrahmen). |
| `RangeThresholdPips` | Mindest-Hoch-Tief-Abstand der vorherigen Kerze, in Pips ausgedrückt. |
| `BodyThresholdPips` | Mindest-Eröffnungs-Schluss-Abstand der vorherigen Kerze, in Pips ausgedrückt. |
| `StopLossPips` | Stop-Loss-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPips` | Hinter dem Preis gehaltener Abstand beim Trailing. Auf 0 setzen zum Deaktivieren des Trailing. |
| `TrailingStepPips` | Zusätzliche Bewegung in Pips, die erforderlich ist, bevor der Trailing Stop enger gestellt wird. |

> **Hinweis:** Pip-Schwellenwerte werden mit `Security.PriceStep` multipliziert. Stellen Sie sicher, dass das Symbol einen gültigen `PriceStep` hat, damit Pip-Umrechnungen Ihr Instrument korrekt widerspiegeln.

## Verwendungshinweise

1. Weisen Sie die Ziel-`Security` und den Handels-Connector zu, bevor Sie die Strategie starten.
2. Passen Sie die Pip-Schwellenwerte an die Volatilität Ihres Marktes an. Forex-Paare mit 5-stelligen Kursen verwenden typischerweise 10 Pips = 0.0010.
3. Setzen Sie das `Volume` der Strategie auf die gewünschte Ordergröße. Die Positionsgrößenlogik des Original-EA ist absichtlich vereinfacht, um die StockSharp-Version transparent zu halten.
4. Da Signale auf geschlossenen Kerzen ausgewertet werden, werden Einstiege unmittelbar nach Bestätigung der Expansionskerze gesendet.
