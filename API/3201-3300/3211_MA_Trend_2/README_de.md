# MA Trend 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Konvertiert vom MetaTrader 5-Expertenberater `MA Trend 2.mq5`.
- Verwendet einen konfigurierbaren gleitenden Durchschnitt, um zu erkennen, ob der Preis über oder unter dem verschobenen Durchschnitt handelt.
- Positionen werden mit optionalem Stop-Loss, Take-Profit, Trailing Stop und Geldmanagement-Funktionen verwaltet.

## Strategie-Logik
1. Die vom Benutzer gewählte Kerzenserie abonnieren und den gleitenden Durchschnitt mit der gewählten Methode, Periode, Shift und Preisquelle berechnen.
2. Bei jeder abgeschlossenen Kerze den neuesten gleitenden Durchschnittswert speichern, damit eine verschobene Stichprobe (vorherige Bar plus `MaShift`) mit dem aktuellen Schlusskurs verglichen werden kann.
3. Kaufsignale generieren, wenn der Preis über den Referenzdurchschnitt steigt und der Richtungsfilter Long-Trades erlaubt. Verkaufssignale für die umgekehrte Bedingung generieren. Wenn `ReverseSignals` aktiviert ist, werden diese Regeln umgekehrt.
4. Vor dem Einstieg in einen Trade die Flags `OnlyOnePosition` und `CloseOppositePositions` prüfen. Die Strategie kann entweder Einstiege überspringen, wenn das entgegengesetzte Exposure existiert, oder es in derselben Order schließen, um die Position zu drehen.
5. Das Positionsgrößen-Management verwendet entweder ein festes Volumen oder ein Risikoprozent-Modell aus dem ursprünglichen EA. Der Prozentmodus schätzt das erforderliche Volumen so, dass der Verlust bei der konfigurierten Stop-Distanz dem Risikobudget entspricht.
6. Ein Trailing Stop repliziert die ursprüngliche Schrittlogik: Sobald der Gewinn `TrailingStopPips + TrailingStepPips` überschreitet, bewegt er den Stop in Schritten, ohne ihn jemals zu lockern. Wenn der Preis den Trailing Stop kreuzt, wird die Position zum Markt geschlossen.
7. Optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen werden durch den High-Level-`StartProtection`-Helfer angehängt, damit das Brokermodell Positionen zwischen Kerzenaktualisierungen schließen kann.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `StopLossPips` | Stop-Loss-Distanz in Pips. Auf `0` setzen zum Deaktivieren. | `50` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. Auf `0` setzen zum Deaktivieren. | `140` |
| `TrailingStopPips` | Basisdistanz für den Trailing Stop in Pips. | `15` |
| `TrailingStepPips` | Minimaler zusätzlicher Gewinn, bevor der Trailing Stop enger gezogen wird. | `5` |
| `LotMode` | `FixedVolume` verwendet `LotOrRiskValue` direkt. `RiskPercent` interpretiert es als Konto-Risikoprozent. | `RiskPercent` |
| `LotOrRiskValue` | Feste Ordergröße oder Risikoprozent abhängig von `LotMode`. | `3` |
| `MaPeriod` | Periode des gleitenden Durchschnitts. | `12` |
| `MaShift` | Anzahl abgeschlossener Kerzen zwischen der aktuellen Bar und der für Signale verwendeten gleitenden Durchschnittsstichprobe. | `3` |
| `MaMethod` | Methode des gleitenden Durchschnitts (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `MaPrice` | Vom gleitenden Durchschnitt verwendeter Kerzenpreis (Schlusskurs, Eröffnung, gewichtet usw.). | `Weighted` |
| `CandleType` | Von der Strategie abonnierter Kerzendatentyp. | `1 minute time frame` |
| `Direction` | Erlaubte Richtung (`BuyOnly`, `SellOnly`, `Both`). | `Both` |
| `OnlyOnePosition` | Nur eine einzige offene Position erlauben. | `false` |
| `ReverseSignals` | Kauf/Verkauf-Logik umkehren. | `false` |
| `CloseOppositePositions` | Entgegengesetztes Exposure vor dem Öffnen eines neuen Trades schließen. | `false` |

## Geldmanagement
- Wenn `LotMode = RiskPercent`, konvertiert die Strategie die Stop-Loss-Distanz (in Pips) in Preiseinheiten unter Verwendung von Wertpapier-Metadaten (`PriceStep`, `StepPrice`).
- Das Risiko wird aus dem Portfoliowert berechnet (`CurrentValue` mit Fallback auf `BeginValue`).
- Das angeforderte Volumen wird auf den nächsten `VolumeStep` aufgerundet, um Ablehnungen durch die Börse zu vermeiden.

## Trailing Stop
- Trailing-Distanz und -Schritt werden in Pips ausgedrückt; der Code leitet die tatsächliche Preisentfernung unter Verwendung der Instrument-Pip-Größe ab.
- Long-Positionen bewegen den Stop nach oben, sobald der Schlusskurs den Einstieg um mindestens `TrailingStopPips + TrailingStepPips` überschreitet. Der Stop bleibt fest, wenn der Gewinn zurückgeht.
- Short-Positionen spiegeln dieselbe Logik mit symmetrischen Preisüberprüfungen wider.

## Konvertierungshinweise
- Alle Trading-Aktionen verwenden die High-Level-`Strategy`-API (`BuyMarket`, `SellMarket`, `StartProtection`).
- Die Strategie hält nur einen kurzen gleitenden Durchschnitts-Verlauf (Shift + Puffer), um die Vorgänger-Bar-Referenz zu replizieren, ohne große Datensätze zu speichern.
- Kommentare sind auf Englisch bereitgestellt, um jeden wichtigen Logikblock zu dokumentieren.
