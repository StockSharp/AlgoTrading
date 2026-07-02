# NRTR-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
## Überblick
Die NRTR-Umkehrstrategie ist eine StockSharp-Portierung des MetaTrader 4-Experten „NRTR_Revers“. Das ursprüngliche System zeichnet eine NRTR-Linie (Noise Reduction Trailing Range) auf, die aus der durchschnittlichen wahren Reichweite (ATR) abgeleitet ist, und kehrt die Positionen um, wenn der Preis diese adaptive Barriere überzeugend durchbricht. Die StockSharp-Version behält das Einzelpositionsverhalten des Expert Advisors bei, spiegelt die ATR-basierte Offset-Berechnung wider und verwaltet Exits über das integrierte Schutzmodul.

## Handelslogik
1. Abonnieren Sie die von `CandleType` konfigurierte Hauptkerzenserie und verarbeiten Sie nur fertige Kerzen, wobei Sie die Gegenprüfung `Bars` von MetaTrader reproduzieren.
2. Füttern Sie einen `AverageTrueRange`-Indikator mit dem Punkt `Period`. Der aktuellste ATR-Wert wird von Preiseinheiten in „Punkte“ (Preisschritte) übersetzt, bevor er mit `AtrMultiplier / 10` multipliziert wird, genau wie der MQL-Ausdruck `MathRound(k * (iATR / Point) / 10)`.
3. Behalten Sie einen rollierenden Cache der letzten Kerzen bei, um den NRTR-Pivot wiederherzustellen. Das niedrigste Tief (für einen Aufwärtstrend) oder das höchste Hoch (für einen Abwärtstrend) über die letzten `Period` Kerzen wird zum Basis-Pivot.
4. Verschieben Sie den Drehpunkt um den ATR-basierten Offset, um die Schlusslinie zu bilden:
   - Aufwärtstrend: `line = lowestLow - offset`.
   - Abwärtstrend: `line = highestHigh + offset`.
5. Erkennen Sie eine Umkehr, wenn eine der beiden Bedingungen erfüllt ist:
   - **Schlussdurchbruch:** Der letzte Kerzenschluss überschreitet die Linie um mehr als `offset` Punkte.
   - **Bereichserweiterung:** Die jüngsten `Period / 2` Kerzen reichen um mindestens `ReverseDistancePoints` Punkte über die Linie hinaus. Dies reproduziert den sekundären Umkehrtest aus dem MQL-Code, der weiter zurück in die Geschichte reicht.
6. Wenn sich die Richtung ändert, senden Sie eine Marktorder (`BuyMarket` oder `SellMarket`) mit einem Volumen von `TradeVolume + |Position|`. Dadurch wird sowohl das gegenüberliegende Exposure geschlossen als auch die neue Position geöffnet, was dem MetaTrader-Verhalten des sofortigen Schließens und Umkehrens entspricht.
7. Exits werden an den von `StartProtection` gestarteten Risikomanager delegiert, der die konfigurierten Stop-Loss- und Take-Profit-Abstände von Punkten in Broker-spezifische Preiseinheiten umrechnet.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15-minütiger Zeitrahmen | Für Berechnungen verwendete Kerzenreihe. |
| `TakeProfitPoints` | `decimal` | `4000` | Take-Profit-Distanz, ausgedrückt in Instrumentenpreisschritten. Zum Deaktivieren auf Null setzen. |
| `StopLossPoints` | `decimal` | `4000` | Stop-Loss-Distanz in Preisschritten. Zum Deaktivieren auf Null setzen. |
| `TrailingStopPoints` | `decimal` | `0` | Reservierter Parameter für externe nachgestellte Module. Wird innerhalb der Strategie nicht verwendet. |
| `TradeVolume` | `decimal` | `0.1` | Basisvolumen (Lots), gespiegelt von der Einstellung MetaTrader. |
| `Period` | `int` | `3` | Anzahl der Kerzen, die zur Berechnung des NRTR-Pivots verwendet werden. |
| `ReverseDistancePoints` | `int` | `100` | Zusätzliche Ausbruchsdistanz in Punkten zur Bestätigung erforderlich. |
| `AtrMultiplier` | `decimal` | `3.0` | Der Multiplikator wird auf ATR angewendet, bevor der Offset erstellt wird. |

## Risikomanagement
- Die Strategie ruft `StartProtection` mit `UnitTypes.Step` auf, sodass die konfigurierten Punktabstände automatisch in absolute Preisoffsets basierend auf `Security.PriceStep` umgewandelt werden.
- Wenn sowohl Stop-Loss als auch Take-Profit Null sind, wird `StartProtection()` trotzdem aufgerufen, um die Positionsüberwachung von StockSharp zu aktivieren und die von EA verwendeten Sicherheitsüberprüfungen zu replizieren.
- `TrailingStopPoints` wird der Vollständigkeit halber offengelegt, aber für zukünftige Erweiterungen übrig gelassen, da der ursprüngliche Experte trotz der Deklaration des Parameters keine abschließende Funktion implementiert hat.

## Details zur Implementierung
- Die Strategie basiert ausschließlich auf dem übergeordneten API (`SubscribeCandles().BindEx(...)`) mit Indikatorbindungen; Es werden keine manuellen Indikatorschleifen oder verbotenen `GetValue`-Aufrufe verwendet.
- Eine kompakte `CandleSnapshot`-Struktur speichert nur Hoch-/Tiefst-/Schlusswerte der letzten Kerzen und vermeidet so viel `ICandleMessage`-Speicher, reproduziert aber dennoch die NRTR-Lookback-Fenster.
- Bei der Umrechnung von ATR in Punkte wird die Formel MetaTrader berücksichtigt, indem ATR durch den Instrumentenschritt dividiert wird, bevor der Multiplikator und die Rundung angewendet werden.
- Durch das Trimmen des Verlaufs bleibt der Cache bei `Period * 3` Kerzen, um den ursprünglichen Lookback-Anforderungen ohne unkontrolliertes Wachstum zu entsprechen.

## Unterschiede zum MetaTrader-Experten
- Das Schließen von Orders wird vereinfacht: Anstatt jeden Trade zu durchlaufen und `OrderClose` aufzurufen, sendet der Port StockSharp eine einzelne Marktorder, die sowohl der bestehenden Position schmeichelt als auch die neue Richtung festlegt.
- Magische Zahlen, Slippage und Ticket-spezifische Parameter werden weggelassen, da StockSharp Bestellungen anders verwaltet.
- Diagrammanmerkungen sind optional; Wenn ein Diagrammbereich verfügbar ist, werden die ATR-Serie und eigene Trades zu Debugging-Zwecken aufgezeichnet.

## Anwendungstipps
- Richten Sie `TradeVolume` mit dem Börsenlosschritt (`Security.VolumeStep`) aus, bevor Sie den Live-Handel aktivieren.
- Stimmen Sie `Period`, `AtrMultiplier` und `ReverseDistancePoints` gemeinsam ab. Kürzere Zeiträume erfordern kleinere Rückwärtsabstände, um ein Überhandeln zu vermeiden.
- Stellen Sie die Stopp-/Zielentfernungen entsprechend der Tick-Größe des Instruments ein. Reduzieren Sie bei Instrumenten mit großem `PriceStep` die standardmäßigen 4000-Punkt-Offsets auf realistische Werte.

## Indikatoren
- `AverageTrueRange(Period)` berechnet auf Basis der Höchst-/Tiefst-/Schlusskurse.
