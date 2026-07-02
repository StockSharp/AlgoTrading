# ZigAndZag-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ZigAndZag Trader Strategy** ist der StockSharp-Port des MetaTrader-Experten *ZigAndZag_trader.mq4*. Das System schichtet zwei ZigZag-inspirierte Schwungdetektoren:

1. Ein **langfristiger ZigZag** (konfiguriert durch `TrendDepth`) verfolgt den primären Trend, indem er große Swing-Hochs und -Tiefs markiert.
2. Ein **kurzfristiger ZigZag** (konfiguriert durch `ExitDepth`) identifiziert den letzten Swing-Pivot innerhalb dieses Trends und überwacht den gewichteten Preis (`(5×Close + 2×Open + High + Low) / 9`).

Der Roboter eröffnet Geschäfte nur, wenn sich der Preis vom letzten Swing-Pivot in Richtung des vorherrschenden Trends entfernt, und schließt Positionen, wenn der gewichtete Preis diesen Pivot entgegen dem Trend durchbricht. Dies reproduziert das Verhalten des ursprünglichen MetaTrader-Experten, der die Puffer 4–6 des benutzerdefinierten `ZigAndZag`-Indikators gelesen hat.

## Handelslogik
- **Trenderkennung** – wenn der langfristige ZigZag ein neues Tief bestätigt, gilt der Trend als *aufsteigend*; ein neues Hoch dreht es auf *unten*.
- **Swing-Tracking** – jeder kurzfristige Pivot setzt den internen Zustand zurück und speichert den gewichteten Preis dieses Swings.
- **Eintrittsbedingungen**
  - Aufwärtstrend + letzter Pivot ist ein Tief: Kaufen, wenn der gewichtete Preis um mindestens einen Pip über den gespeicherten Pip steigt.
  - Abwärtstrend + letzter Pivot ist ein Hoch: Verkaufen, wenn der gewichtete Preis um mindestens einen Pip unter den gespeicherten Pip fällt.
- **Ausstiegsbedingung** – wenn sich der Preis über den gespeicherten Pivot zurückbewegt, während der Trend nicht mit dem aktiven Swing übereinstimmt, werden alle offenen Positionen geschlossen.
- **Bestelldrosselung** – die gesamte absolute Positionsgröße ist auf `MaxOrders × Volume` begrenzt. Zusätzliche Signale werden ignoriert, sobald diese Obergrenze erreicht ist.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | `1 Minute` | Kerzentyp, der für beide ZigZag-Auswertungen verwendet wird. |
| `Lots` | `0.1` | Gewünschte Handelsgröße in Lots. Die endgültige Lautstärke wird an die Lautstärkestufe des Instruments angepasst. |
| `TrendDepth` | `3` | Rückblick (in Kerzen) auf den langfristigen ZigZag, der den Trend definiert. |
| `ExitDepth` | `3` | Rückblick (in Kerzen) auf den kurzfristigen ZigZag, der zu Swing-Einstiegen und -Ausstiegen führt. |
| `MaxOrders` | `1` | Maximale Anzahl gleichzeitiger Aufträge/Positionseinheiten. |
| `StopLossPips` | `0` | Schützende Stop-Loss-Distanz in Pips (`0` deaktiviert den Stop). |
| `TakeProfitPips` | `0` | Take-Profit-Distanz in Pips (`0` deaktiviert das Ziel). |

## Risikomanagement
`StartProtection` wird automatisch aktiviert. Wenn die Stop-Loss- oder Take-Profit-Distanz auf einen Wert größer als Null eingestellt ist, werden jeder Marktorder feste Schutzaufträge unter Verwendung der angegebenen Pip-Distanz und der Tick-Größe des Instruments beigefügt.

## Visualisierung
Die Strategie zeichnet Candlesticks und ausgeführte Trades im Standard-Chartbereich. Es wird kein benutzerdefinierter Indikator dargestellt, da die Ein- und Ausstiegslogik interne ZigZag-Tracker verwendet.

## Notizen
- Die gewichtete Preisformel ist identisch mit dem Indikator MetaTrader und vermeidet den direkten Zugriff auf den Indikatorpuffer.
- Die Ausbruchsschwelle entspricht einem Instrumenten-Pip und spiegelt den ursprünglichen Code wider, der erforderte, dass die Bewegung den aktuellen Spread überschreitet.
- Der Port speichert alle Kommentare und Protokolle auf Englisch, wie es die Projektrichtlinien erfordern.
