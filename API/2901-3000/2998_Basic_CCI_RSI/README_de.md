# Grundlegende CCI-RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Grundlegende CCI-RSI-Strategie reproduziert den ursprünglichen MetaTrader Expert Advisor, der wartet, bis sowohl der Commodity Channel Index (CCI) als auch der Relative Strength Index (RSI) das Momentum für zwei aufeinanderfolgende geschlossene Kerzen bestätigen, bevor ein Trade eingegangen wird. Die StockSharp-Version behält die pip-basierten Geldmanagement-Regeln bei, konvertiert sie automatisch in Preisschritte und fügt dasselbe Trailing-Stop-Verhalten hinzu, das in MQL5 mit Positionsänderungen implementiert wurde.

## Wie die Strategie handelt

1. Beim Schluss jeder Kerze (standardmäßig stündlich) erhält die Strategie neue CCI- und RSI-Werte.
2. Long-Einstiege erfordern, dass **beide** Indikatoren ihre jeweiligen oberen Schwellenwerte für die aktuelle und die vorherige geschlossene Kerze überschreiten. Short-Einstiege erfordern, dass beide unter ihren unteren Schwellenwerten für die letzten zwei Kerzen bleiben.
3. Wenn ein Signal auftritt, öffnet die Strategie eine Position mit dem konfigurierten Volumen (schließt dabei jedes entgegengesetzte Exposure) und berechnet sofort feste Stop-Loss- und Take-Profit-Preise mithilfe der Pip-Abstände aus dem ursprünglichen Skript.
4. Während die Position offen ist, überprüft die Strategie ständig, ob der Kerzenbereich das Stop- oder Take-Level berührt hat, und schließt zum Marktpreis, wenn eines erreicht wird.
5. Ein Trailing-Stop repliziert die MetaTrader-Implementierung: Sobald der Gewinn `TrailingStopPips + TrailingStepPips` übersteigt, wird der Schutz-Stop auf `TrailingStopPips` hinter dem aktuellen Schluss (für Longs) oder darüber (für Shorts) verschoben. Weitere Anpassungen erfordern ein zusätzliches `TrailingStepPips` an Gewinn, bevor erneut enger gezogen wird.

Dieser Ablauf hält die Logik nah am MQL5-Quell-Experten, während High-Level-Kerzenabonnements und Indikatoren von StockSharp verwendet werden.

## Risikomanagement

- **Stop-Loss**: fester Pip-Abstand, der in den Instrument-Preisschritt konvertiert wird. Deaktiviert wenn auf null gesetzt.
- **Take-Profit**: fester Pip-Abstand, der in den Preisschritt konvertiert wird. Deaktiviert wenn null.
- **Trailing-Stop**: optionaler Pip-Abstand mit einem Schritt-Buffer, der die `Trailing()`-Funktion des Expert Advisors imitiert. Deaktiviert wenn `TrailingStopPips` null ist.
- **Positionsgrößenbestimmung**: über die `Volume`-Eigenschaft der Strategie gesteuert; das Standard-Lot beträgt einen Kontrakt.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `StopLossPips` | Abstand in Pips zwischen dem Einstiegspreis und der Stop-Loss-Order. |
| `TakeProfitPips` | Abstand in Pips zwischen dem Einstiegspreis und dem Take-Profit-Ziel. |
| `TrailingStopPips` | Gewinn (in Pips), der erforderlich ist, um den Stop zu trailen. |
| `TrailingStepPips` | Zusätzlicher Gewinn (in Pips), der vor jeder neuen Trailing-Anpassung erforderlich ist. |
| `CciPeriod` | Mittelungsperiode für den CCI-Indikator. |
| `RsiPeriod` | Mittelungsperiode für den RSI-Indikator. |
| `RsiLevelUp` | Überkauft-Level, das überschritten werden muss, um Long-Trades zu validieren. |
| `RsiLevelDown` | Überverkauft-Level, das unterschritten werden muss, um Short-Trades zu validieren. |
| `CciLevelUp` | Oberer CCI-Schwellenwert, der bullischen Impuls bestätigt. |
| `CciLevelDown` | Unterer CCI-Schwellenwert, der bärischen Impuls bestätigt. |
| `CandleType` | Zeitrahmen für die Kerzenaggregation und Indikatorberechnungen. |

## Standardwerte

- `StopLossPips` = 125
- `TakeProfitPips` = 60
- `TrailingStopPips` = 5
- `TrailingStepPips` = 5
- `CciPeriod` = 12
- `RsiPeriod` = 15
- `RsiLevelUp` = 75
- `RsiLevelDown` = 30
- `CciLevelUp` = 80
- `CciLevelDown` = -95
- `CandleType` = 1-Stunden-Kerzen

## Zusätzliche Hinweise

- Pip-Abstände werden automatisch skaliert: Wenn das Instrument 3 oder 5 Dezimalstellen verwendet, multipliziert die Strategie den Preisschritt mit zehn, was der MetaTrader-Logik des „angepassten Punkts" entspricht.
- Einstiege werden nur auf geschlossenen Kerzen ausgewertet, um Neuzeichnen zu vermeiden und die ursprüngliche „neuer Balken"-Bedingung im Expert Advisor zu spiegeln.
- Ausstiege verwenden immer Marktorders, was deterministisches Verhalten innerhalb der StockSharp-Backtesting-Umgebung gewährleistet.

## Klassifizierungshinweise

- Kategorie: Oszillator-Bestätigung
- Richtung: Bidirektional
- Indikatoren: CCI, RSI
- Stops: Fest und Trailing (pip-basiert)
- Komplexität: Grundlegend
- Zeitrahmen: Intraday bis Swing (Standard 1 Stunde)
- Saisonalität: Nein
- Neuronale Netze: Nein
- Divergenz: Nein
- Risikolevel: Moderat
