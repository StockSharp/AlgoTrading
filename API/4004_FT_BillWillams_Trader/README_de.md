# FT Bill Williams Händlerstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **FT Bill Williams Trader Strategy** ist eine hochrangige StockSharp-Übersetzung des MetaTrader-Expertenberaters „FT_BillWillams_Trader“. Es kombiniert Bill Williams-Fraktale mit dem Alligator-Indikator, um Trendausbrüche zu handeln. Die Strategie sucht nach neuen Fraktalen, überprüft, ob die Alligator-Struktur die Ausbruchsrichtung bestätigt, und wendet optional Abstands-, Ausrichtungs- und Umkehrsignalfilter an, bevor eine Position eröffnet wird.

## Handelslogik

1. **Fraktale Erkennung** – die Strategie puffert die aktuellsten `FractalPeriod` Hochs und Tiefs. Wenn der mittlere Balken den höchsten (oder niedrigsten) Punkt im Fenster darstellt, wird ein neues Ausbruchsniveau aufgezeichnet. Über/unter dem Fraktal wird ein `IndentPoints`-Offset hinzugefügt, um vorzeitige Einträge zu vermeiden.
2. **Breakout-Bestätigung** – abhängig von `EntryConfirmation`:
   - `PriceBreakout` bestätigt, wann die Kerzenspanne das Ausbruchsniveau überschreitet.
   - `CloseBreakout` wartet darauf, dass der Schlusskurs der vorherigen Kerze über dem Niveau liegt.
3. **Entfernungsprüfung** – Eingaben werden abgelehnt, wenn das Ausbruchsniveau weiter als `MaxDistancePoints` von den Alligator-Lippen entfernt ist (vorheriger Balkenwert). Setzen Sie den Abstand auf Null, um den Filter zu deaktivieren.
4. **Zahnfilter** – wenn `UseTeethFilter` aktiviert ist, muss der vorherige Schlusskurs über (für Long-Positionen) oder unter (für Short-Positionen) den Alligator-Zähnen liegen.
5. **Trendausrichtung** – bei `UseTrendAlignment = true` müssen Lippen, Zähne und Kiefer mindestens `TeethLipsDistancePoints` bzw. `JawTeethDistancePoints` Punkte voneinander entfernt sein, um zu bestätigen, dass Alligator im Trend liegt.
6. **Umgekehrte Ausgänge** – wenn `ReverseExit = OppositeFractal`, schließt jedes neue entgegengesetzte Fraktal sofort die offene Position. Bei `OppositePosition` schließt die Strategie zunächst den aktuellen Trade ab, bevor sie einen in die entgegengesetzte Richtung eröffnet.
7. **Jaw Exit** – `JawExit` definiert, ob die Position geschlossen wird, wenn der Preis den Alligator-Kiefer kreuzt (Intrabar oder bei Kerzenschluss).
8. **Trailing Stop** – wenn `EnableTrailing` wahr ist und der Handel profitabel ist, bewegt sich der Stop zu den Lippen oder Zähnen, abhängig von der relativen Neigung der Lippen und dem `SlopeSmaPeriod` SMA. Anfängliche Schutzstopps und Gewinnziele werden durch `StopLossPoints` und `TakeProfitPoints` gesteuert.

## Parameter

| Eigentum | Beschreibung | Standard |
|----------|-------------|---------|
| `OrderVolume` | Handelsvolumen, das beim Versenden von Marktaufträgen verwendet wird. | `0.1` |
| `FractalPeriod` | Anzahl der Balken im Fraktalmuster (ungerade Werte empfohlen). | `5` |
| `IndentPoints` | Zum Ausbruchsniveau hinzugefügter Offset (in Punkten). | `1` |
| `EntryConfirmation` | Breakout-Bestätigungsmodus (`PriceBreakout`, `CloseBreakout`). | `CloseBreakout` |
| `UseTeethFilter` | Erfordern, dass sich der vorherige Abschluss auf der richtigen Seite der Alligator-Zähne befindet. | `true` |
| `MaxDistancePoints` | Maximaler Abstand zwischen Ausbruchsebene und Alligator Lippen (Punkten). | `1000` |
| `UseTrendAlignment` | Erzwingen Sie den Mindestabstand zwischen Alligator Zeilen. | `false` |
| `JawTeethDistancePoints` | Mindestabstand zwischen Kiefer und Zähnen, der im Ausrichtungsfilter verwendet wird. | `10` |
| `TeethLipsDistancePoints` | Mindestabstand zwischen Zähnen und Lippen, der im Ausrichtungsfilter verwendet wird. | `10` |
| `JawExit` | Modus zum Schließen von Positionen bei Backenkreuzung (`Disabled`, `PriceCross`, `CloseCross`). | `CloseCross` |
| `ReverseExit` | Umgang mit Gegensignalen (`Disabled`, `OppositeFractal`, `OppositePosition`). | `OppositePosition` |
| `EnableTrailing` | Aktivieren Sie die Alligator-basierte Trailing-Stop-Verwaltung. | `true` |
| `SlopeSmaPeriod` | Periode des SMA, der mit der Lippensteigung verglichen wird. | `5` |
| `StopLossPoints` | Stop-Loss-Distanz in Punkten (0 deaktiviert). | `50` |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten (0 deaktiviert). | `50` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Zeiträume für die Zeilen Alligator. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Vorwärtsverschiebung für jede Alligator-Zeile. | `8`, `5`, `3` |
| `MaMethod` | Typ des gleitenden Durchschnitts für Alligator (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Simple` |
| `AppliedPrice` | Der an Alligator gelieferte Kerzenpreis. | `CandlePrice.Median` |
| `CandleType` | Aus den Marktdaten abonnierter Kerzentyp. | `15-minute timeframe` |

## Zusätzliche Hinweise

- Die Strategie zeichnet die Alligator-Linien und führt Trades im Standard-Chartbereich aus.
- `FractalPeriod` sollte ungerade bleiben, sodass der mittlere Balken die fraktale Spitze darstellt; Der Standardwert entspricht dem ursprünglichen Expert Advisor.
- Entfernungsbasierte Parameter (`IndentPoints`, `MaxDistancePoints`, `JawTeethDistancePoints`, `TeethLipsDistancePoints`, `StopLossPoints`, `TakeProfitPoints`) werden in Brokerpunkten (`Security.PriceStep`) ausgedrückt.
- Trailing Stops und Jaw Exits basieren auf abgeschlossenen Kerzen und spiegeln die ursprüngliche MQL-Logik wider, die mit den vorherigen Balkenwerten der Alligator funktioniert.
