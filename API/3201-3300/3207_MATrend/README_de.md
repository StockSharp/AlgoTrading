# 3207 – MA Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **MA Trend-Strategie** repliziert den MetaTrader-Experten *MA Trend.mq5* mit der High-Level-API von StockSharp. Der Bot folgt einem einzigen linear gewichteten gleitenden Durchschnitt mit einem konfigurierbaren Vorwärts-Shift. Wenn der Schlusskurs über den verschobenen Durchschnitt steigt, geht die Strategie long, während ein Fall unter den Durchschnitt Short-Positionen eröffnet. Optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Regeln spiegeln die Risikokontrollen der ursprünglichen MQL-Implementierung wider.

## Trading-Logik
1. Den konfigurierten Kerzentyp abonnieren (Standard: 1-Minuten-Zeitrahmen) und einen gleitenden Durchschnitt mit der gewählten Methode und Preisquelle berechnen.
2. Den gleitenden Durchschnittswert um die angeforderte Anzahl abgeschlossener Kerzen vorwärts verschieben, bevor er mit dem neuesten Schlusskurs verglichen wird.
3. Signale generieren:
   - **Long** – Schlusskurs über dem verschobenen MA (umgekehrt, wenn `ReverseSignals` aktiviert ist).
   - **Short** – Schlusskurs unter dem verschobenen MA (umgekehrt, wenn `ReverseSignals` aktiviert ist).
4. Positionsmanagement-Optionen anwenden:
   - Das entgegengesetzte Exposure schließen, bevor ein Trade eröffnet wird, wenn `CloseOpposite` `true` ist.
   - Neue Einstiege blockieren, wenn `OnlyOnePosition` aktiviert ist und bereits eine Position existiert.
5. Ausstiege mit Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen in Pips verwalten. Die Trailing-Logik erfordert, dass der Preis sich um `TrailingStopPips + TrailingStepPips` bewegt, bevor der Stop enger gezogen wird, genau wie beim MQL-Experten.

## Parameter
| Name | Typ | Standard | Beschreibung |
|------|------|---------|-------------|
| `OrderVolume` | `decimal` | `0.1` | Ordergröße in Lots/Kontrakten. |
| `StopLossPips` | `int` | `50` | Stop-Loss-Distanz in Pips. Null deaktiviert den festen Stop. |
| `TakeProfitPips` | `int` | `140` | Take-Profit-Distanz in Pips. Null deaktiviert das Ziel. |
| `TrailingStopPips` | `int` | `15` | Trailing-Stop-Distanz. Auf null setzen, um Trailing zu deaktivieren. |
| `TrailingStepPips` | `int` | `5` | Zusätzliche Pips erforderlich, bevor der Trailing Stop bewegt wird. Muss positiv bleiben, wenn `TrailingStopPips` größer als null ist. |
| `MaPeriod` | `int` | `12` | Länge des gleitenden Durchschnitts. |
| `MaShift` | `int` | `3` | Anzahl abgeschlossener Bars zum Vorwärtsverschieben des gleitenden Durchschnitts. |
| `MaMethod` | `MovingAverageKind` | `Weighted` | Berechnungsmodus des gleitenden Durchschnitts (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `AppliedPriceMode` | `Weighted` | Kerzenpreis als Indikatoreingang (Close, Open, High, Low, Median, Typical, Weighted). |
| `OnlyOnePosition` | `bool` | `false` | Strategie auf eine einzelne offene Position beschränken. |
| `ReverseSignals` | `bool` | `false` | Long/Short-Signalrichtungen umkehren. |
| `CloseOpposite` | `bool` | `false` | Entgegengesetztes Exposure vor dem Einstieg in eine neue Position schließen. |
| `CandleType` | `DataType` | `1 minute` | Kerzentyp/Zeitrahmen für den Indikator. |

## Hinweise
- Die Pip-Größe passt sich automatisch an Instrumente mit 3/5-Dezimal-Preisen an, um das ursprüngliche MetaTrader-Verhalten zu replizieren.
- Die Trailing-Stop-Validierung reproduziert die MQL-Prüfung: wenn `TrailingStopPips > 0` und `TrailingStepPips <= 0`, wirft die Strategie beim Start eine Ausnahme.
- Alle Indikatorupdates und Orderentscheidungen verwenden nur abgeschlossene Kerzen, was deterministische Backtests gewährleistet.
