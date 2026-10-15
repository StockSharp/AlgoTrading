# Grundlegend Martingale EA 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Basic Martingale EA 3**-Strategie repliziert den MetaTrader 5 Expert Advisor, der einen Trendfilter basierend auf dem Triple Exponential Moving Average (TEMA) mit ATR-gesteuerter Martingal-Mittelwertbildung kombiniert. Die konvertierte StockSharp-Version behält die gleichen Risikoparameter, das gleiche Handelsfenster und die gleiche Geldverwaltungslogik bei, während alles durch Strategieparameter zur Optimierung offengelegt wird.

## Handelslogik
1. **Signalgenerierung** – bei jeder abgeschlossenen Kerze des ausgewählten Zeitrahmens wird der Schlusskurs mit dem TEMA-Wert verglichen. Ein Schlusskurs über dem Indikator öffnet einen Long-Korb, während ein Schlusskurs darunter einen Short-Korb öffnet. Es kann immer nur eine Richtung gleichzeitig aktiv sein.
2. **Handelsfenster** – neue Körbe sind nur zwischen `StartHour` und `EndHour` (Umtauschzeit) zulässig. Wenn beide Stunden gleich sind, gilt das Fenster als immer geöffnet. Setzen Sie `TradeAtNewBar` auf `true`, um neue Körbe auf einen pro Kerze zu beschränken, ähnlich dem ursprünglichen `TradeAtNewBar`-Schalter in MT5.
3. **Durchschnittsraster** – Sobald eine Position existiert, misst die Strategie den Abstand vom schlechtesten/besten Einstiegspreis. Immer wenn sich der Markt um mindestens `GridMultiplier × ATR` bewegt, wird eine zusätzliche Order in der durch `Averaging` definierten Richtung (Durchschnitt nach unten oder Durchschnitt nach oben) hinzugefügt, bis `MaxAverageOrders` erreicht ist. Die neue Ordergröße folgt dem gewählten Martingalmodus (`Multiply` oder `Increment`).
4. **Schutzausstiege** – optionale Stop-Loss- und Take-Profit-Level werden von der ersten Order im Warenkorb übernommen. Darüber hinaus ahmt der Trailing Block die MT5-Implementierung nach: Nach `TrailingStart` Gewinnpunkten wird der Stop auf `price - TrailingStop` (oder `price + TrailingStop` für Shorts) verschoben und um `TrailingStep` verschärft.
5. **Abflachung** – wenn ein Stop-, Take-Profit- oder Trailing-Level erreicht wird, wird der gesamte Korb zum Marktwert geschlossen und alle Durchschnittszähler werden zurückgesetzt.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | H1-Zeitrahmen | Kerzenserie, die die Strategie vorantreibt. |
| `StartVolume` | `decimal` | `0.01` | Anfangsvolumen für die erste Bestellung in einem Warenkorb. |
| `StopLossPoints` | `decimal` | `20` | Stop-Loss-Distanz in Preisschritten. Zum Deaktivieren auf `0` setzen. |
| `TakeProfitPoints` | `decimal` | `20` | Take-Profit-Distanz in Preisschritten. Zum Deaktivieren auf `0` setzen. |
| `StartHour` | `int` | `3` | Stunde (einschließlich), in der neue Körbe beginnen können. |
| `EndHour` | `int` | `18` | Stunde (exklusiv), in der die Warenkorberstellung stoppt. |
| `TemaPeriod` | `int` | `50` | Länge des TEMA-Indikators. |
| `BarsCalculated` | `int` | `3` | Anzahl der benötigten fertigen Kerzen, bevor der Handel beginnt. |
| `AtrPeriod` | `int` | `14` | Zeitraum des Average True Range-Indikators. |
| `GridMultiplier` | `decimal` | `0.75` | ATR-Multiplikator, der den Rasterabstand definiert. |
| `MaxAverageOrders` | `int` | `3` | Maximale Anzahl von Durchschnittsaufträgen pro Richtung (einschließlich der anfänglichen). |
| `Averaging` | Aufzählung | `AverageDown` | Wählen Sie zwischen der Mittelwertbildung beim Drawdown, der Mittelwertbildung beim Gewinn oder der Deaktivierung zusätzlicher Einträge. |
| `Martin` | Aufzählung | `Multiply` | Wählen Sie zwischen multiplikativer oder inkrementeller Martingal-Größenbestimmung. |
| `LotMultiplier` | `decimal` | `1.5` | Vom Martingalmodus `Multiply` verwendeter Faktor. |
| `LotIncrement` | `decimal` | `0.1` | Zusätzliches Volumen, das vom Martingalmodus `Increment` verwendet wird. |
| `TradeAtNewBar` | `bool` | `false` | Beschränken Sie die Anzahl neuer Körbe auf einen pro fertiger Kerze. |
| `TrailingStart` | `int` | `100` | Profitieren Sie von den zur Aktivierung des Trailings erforderlichen Punkten. |
| `TrailingStop` | `int` | `50` | Trailing-Stop-Distanz in Punkten. |
| `TrailingStep` | `int` | `30` | Minimale Verbesserung (Punkte), bevor der Trailing Stop erneut verschoben wird. |

## Konvertierungshinweise
- Die StockSharp-Version behält die MT5-Indikatorkonfiguration (TEMA(50) + ATR(14)) bei und stellt den Parameter `bar` als `BarsCalculated` bereit, wodurch sichergestellt wird, dass vor dem Handel mindestens die angegebene Anzahl von Kerzen vorhanden ist.
- Bei der Volumenverarbeitung werden die Werte `MinVolume`, `MaxVolume` und `VolumeStep` des Instruments berücksichtigt, sodass beim Live-Handel die Börsenlimits auch bei gebrochenen Martingalschritten eingehalten werden.
- Die Trailing-Logik folgt dem ursprünglichen Break-Even-Plus-Trailing-Step-Verhalten, wird jedoch mit aggregierten Positionsdaten implementiert, da StockSharp Positionen nach Instrument saldiert werden.
- Diagrammanmerkungen des MT5-Experten wurden nicht portiert, da StockSharp bereits eine Auftrags- und Positionsvisualisierung in den Diagrammfeldern bereitstellt.
