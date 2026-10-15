# OzFx Accelerator Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader-Expertenberaters *OzFx (barabashkakvns Edition)* zur High-Level-Strategie-API von StockSharp.
- Kombiniert den Acceleration/Deceleration-Oszillator (AC) mit einem Stochastik-Schwellenwert, um in Trends schichtweise einzusteigen.
- Entwickelt für diskretionären Forex-Handel, bei dem Orders in Lots dimensioniert werden und der Schutz in Pips ausgedrückt wird.

## Handelslogik
1. Berechne den Acceleration/Deceleration-Oszillator als Differenz zwischen dem Awesome Oscillator und seinem 5-Perioden-SMA.
2. Abonniere einen stochastischen Oszillator mit konfigurierbaren `%K`-, `%D`- und Verlangsamungsperioden.
3. Wenn eine neue Kerze schließt, bewerte die zwei aktuellsten AC-Werte zusammen mit dem stochastischen Niveau:
   - **Long-Setup**: `%K` kreuzt über dem konfigurierten Niveau, der aktuelle AC ist positiv und steigt, während der vorherige Wert negativ war.
   - **Short-Setup**: `%K` kreuzt unter dem Niveau, der aktuelle AC ist negativ und fällt, während der vorherige Wert positiv war.
4. Bei einem gültigen Signal werden bis zu fünf gleich große Marktorders geöffnet. Die erste Schicht spiegelt den ursprünglichen EA wider, indem sie ohne Stop/Ziel gestartet wird, während die verbleibenden Schichten den konfigurierten Stop Loss und gestaffelte Take Profits erben.
5. Das Exit-Management emuliert das ursprüngliche `modok`-Flag-Verhalten:
   - Wenn Trailing Stops deaktiviert sind, zieht die Strategie Stops nur nach einem profitablen Exit auf Breakeven nach, und schließt alle Schichten, wenn die Stochastik/AC-Kombination sich gegen die Position dreht.
   - Mit aktivierten Trailing Stops folgt der Stop dem Preis, sobald die Bewegung *TrailingStop + TrailingStep* übersteigt, und dieselbe Momentum-Umkehr schließt den Stapel.

## Positionsskalierung und Ziele
- Long-Positionen platzieren vier zusätzliche Schichten mit Take Profits bei `entry + TakeProfit * i` für `i = 1..4`. Shorts spiegeln dies unterhalb des Preises.
- Stop Losses (wenn konfiguriert) werden jeder Schicht außer der allerersten angehängt, genau wie das MT5-Skript.
- Teilweise Take Profits aktualisieren das interne Flag, sodass die nächste Kampagne sofort im Zustand "modok = true" startet und den Breakeven-Schutz für die initiale Schicht freischaltet.

## Risikomanagement
- `StopLossPips` und `TakeProfitPips` werden in Pips definiert. Die Strategie konvertiert sie mithilfe der Instrument-Tick-Größe und Stellenpräzision (`5` oder `3` Dezimalpaare zählen als Bruchteile von Pips).
- `TrailingStopPips = 0` deaktiviert die Trailing-Logik und ermöglicht nur Breakeven-Anpassungen nach einem Take Profit. Jeder positive Wert aktiviert den oben beschriebenen Trailing-Block.
- Alle Exits werden mit Marktorders ausgeführt, wenn der Kerzenbereich die gespeicherten Stop- oder Zielniveaus durchbricht, was dem Verhalten des ursprünglichen Experten entspricht, der auf brokerseitige Schutzorders angewiesen war.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Lotgröße pro Schicht. | `0.1` |
| `StopLossPips` | Abstand für Schutz-Stop-Orders (Pips). | `100` |
| `TakeProfitPips` | Basisabstand zwischen gestaffelten Take Profits (Pips). | `50` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing). | `50` |
| `TrailingStepPips` | Zusätzlicher Abstand vor dem Vorrücken des Trailing Stops. | `5` |
| `KPeriod` | Stochastik-`%K`-Lookback. | `5` |
| `DPeriod` | Stochastik-`%D`-Glättung. | `3` |
| `SmoothingPeriod` | Endglättung für `%K`. | `3` |
| `StochasticLevel` | Schwellenwert zur Trennung bullischer/bärischer Regime. | `50` |
| `CandleType` | Quell-Kerzenserie für Berechnungen. | `4h-Zeitrahmen` |

## Implementierungshinweise
- Signale, Trailing-Updates und Schutz-Exits werden auf abgeschlossenen Kerzen verarbeitet, um konsistent mit dem EA zu bleiben, der bei neuen Bars auslöst.
- Der AC-Indikator wird reproduziert, indem der Awesome Oscillator gebunden und sein 5-Perioden-SMA subtrahiert wird; auf Low-Level-Indikatoren-Puffer wird nicht zugegriffen.
- Die Pip-Konvertierung passt sich automatisch an 4/5-stellige Forex-Symbole an und fällt auf einen angemessenen Standard zurück, wenn Tick-Größen-Metadaten fehlen.
- Die Strategie führt ein internes Hauptbuch mit Schichteinstiegen, damit Teil-Take-Profits und Stop-Anpassungen mit der Pro-Position-Logik der MetaTrader-Version übereinstimmen.
- Da StockSharp Exits über Marktorders ausführt, werden Trades geglättet, wenn das Hoch/Tief der Kerze die gespeicherten Stop- oder Zielniveaus durchsticht, anstatt auf brokerseitige Auslöser zu warten.
