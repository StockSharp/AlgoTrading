# Virtual Trailing Stop Level1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Virtual Trailing Stop-Strategie** ist eine direkte Portierung des MetaTrader-Expertenberaters `Virtual Trailing Stop.mq5` (MQL ID 21362). Der ursprüngliche Experte verwaltet nur Schutz-Stops für Positionen, die an anderer Stelle geöffnet wurden. Dieser C#-Port reproduziert dasselbe Verhalten auf Basis der StockSharp High-Level-API: Er überwacht die besten Bid/Ask-Kurse und schließt die aktuelle Position, wenn Stop-Loss-, Take-Profit- oder Trailing-Stop-Bedingungen erfüllt sind.

Im Gegensatz zu einstiegsgetriebenen Strategien eröffnet diese Implementierung niemals selbstständig neue Positionen. Sie ist dafür gedacht, mit anderen automatisierten Einstiegen oder manuellen Handelssitzungen kombiniert zu werden, wenn ein MetaTrader-artiger "virtueller" Trailing Stop innerhalb von StockSharp benötigt wird.

## Handelslogik
1. **Level1-Feed** – die Strategie abonniert Level1-Daten und speichert kontinuierlich die aktuellen Bid/Ask-Werte.
2. **Pip-Konvertierung** – Benutzereingaben werden in *Pips* definiert. Die Strategie konvertiert sie in Preisabstände, indem der Wert mit dem `PriceStep` des Instruments multipliziert wird. Für 3- und 5-stellige Forex-Kurse wird ein 10-facher Multiplikator angewendet, um der Pip-Definition von MetaTrader zu entsprechen.
3. **Stop-Loss-Prüfung** – wenn das Bid einer Long-Position unter `Einstiegspreis − StopLoss` fällt oder das Ask einer Short-Position über `Einstiegspreis + StopLoss` steigt, wird die Position zum Marktpreis geschlossen.
4. **Take-Profit-Prüfung** – wenn das Bid einer Long-Position über `Einstiegspreis + TakeProfit` steigt oder das Ask einer Short-Position unter `Einstiegspreis − TakeProfit` fällt, wird die Position geschlossen.
5. **Trailing-Aktivierung** – sobald sich der Preis um `TrailingStart` Pips zugunsten der Position bewegt, wird ein Trailing-Level bei `Bid − TrailingStop` (Long) oder `Ask + TrailingStop` (Short) erstellt.
6. **Trailing-Aktualisierung** – jedes Mal, wenn der unrealisierte Gewinn um mindestens `TrailingStep` Pips zunimmt, wird das Trailing-Level entsprechend verschoben. Wenn der Step auf null gesetzt wird, folgt der Trail jedem günstigen Tick.
7. **Trailing-Exit** – die Position wird geschlossen, wenn der Preis das Trailing-Level berührt, während der Trade profitabel bleibt (entspricht der `Profit()>0`-Absicherung aus dem Quell-EA).

Es werden keine ausstehenden Orders platziert. Jeder Exit wird durch Marktorders ausgeführt, um den "virtuellen" Charakter der MQL-Implementierung nachzuahmen.

## Parameter
| Parameter | Beschreibung | Standardwert |
| --- | --- | --- |
| `StopLossPips` | Stop-Loss-Abstand in Pips. Auf `0` setzen, um die harte Stop-Loss-Verwaltung zu deaktivieren. | `0` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Auf `0` setzen, um die Take-Profit-Verwaltung zu deaktivieren. | `0` |
| `TrailingStopPips` | Abstand zwischen aktuellem Preis und Trailing-Level, gemessen in Pips. | `5` |
| `TrailingStartPips` | Gewinnschwelle (in Pips), die erreicht werden muss, bevor das Trailing aktiviert wird. | `5` |
| `TrailingStepPips` | Minimaler Pip-Anstieg, der erforderlich ist, bevor das Trailing-Level wieder verschoben wird. `0` für kontinuierliches Trailing verwenden. | `1` |

Alle Parameter unterstützen die Optimierung dank der StockSharp `StrategyParam`-Helfer.

## Implementierungshinweise
- Die Strategie verwendet nur Level1-Daten (`DataType.Level1`) und registriert keine Chartobjekte, da StockSharp die Visualisierung anders als MetaTrader handhabt.
- Preiskonvertierungen basieren auf `Security.PriceStep` und `Security.Decimals`. Wenn die Börse diese Metadaten nicht bereitstellt, beträgt die Fallback-Pip-Größe `1`.
- Der Schutz ist für Long- und Short-Positionen symmetrisch. Trailing-Werte werden für beide Richtungen separat gespeichert.
- Die automatische Positions-Initialisierung, die im Tester-Modus des ursprünglichen EA vorhanden war, wurde absichtlich weggelassen, da StockSharp-Strategien auf Netto-Positionen operieren.

## Verwendungshinweise
- Fügen Sie die Strategie einem Portfolio-/Instrumentenpaar hinzu, das bereits offene Positionen hat oder voraussichtlich welche von einer anderen Komponente erhalten wird.
- Kombinieren Sie sie mit diskretionärem Handel oder automatisierten Einstiegsstrategien, um MetaTrader-artiges Trade-Management in StockSharp Designer, Shell oder Runner nachzuahmen.
- Beim Handel mit Nicht-Forex-Instrumenten passen Sie die Pip-basierten Eingaben an die Tick-Größe des Instruments an. Das Setzen von `TrailingStopPips = 1` bewirkt effektiv ein Trailing um einen `PriceStep`.

## Dateien
- `CS/VirtualTrailingStopLevel1Strategy.cs` – Strategieimplementierung.
- `README.md`, `README_zh.md`, `README_ru.md` – mehrsprachige Dokumentation der Strategie.
