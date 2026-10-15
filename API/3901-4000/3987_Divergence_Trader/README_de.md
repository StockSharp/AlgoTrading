# Divergenzhändler (klassische Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert das Verhalten des MetaTrader 4 Expertenberaters **Divergenzhändler** innerhalb des StockSharp hohen Niveaus API. Für den ausgewählten Kerzenpreis (standardmäßig offen) werden zwei einfache gleitende Durchschnitte berechnet. Das System überwacht, wie sich der Abstand zwischen den schnellen und langsamen Durchschnittswerten von einem Balken zum nächsten ändert:

* Wenn sich der Spread nach oben ausdehnt und der Divergenzwert zwischen der *Kaufschwelle* und der *Stay-Out-Schwelle* bleibt, wird eine Long-Position eröffnet oder eine bestehende Short-Position gedeckt.
* Wenn sich der Spread innerhalb der gespiegelten Schwellenwerte nach unten ausweitet, wird eine Short-Position eingegangen oder ein bestehender Long-Trade geschlossen.

Es werden nur fertige Kerzen verwendet, entsprechend der Bar-für-Bar-Verarbeitung des Original-Expertenberaters. Alle Verwaltungsregeln werden mit ereignisgesteuerten High-Level-Aufrufen (`BuyMarket` / `SellMarket`) implementiert.

## Handelsregeln

1. Abonnieren Sie den konfigurierten Kerzentyp und berechnen Sie zwei SMAs mit den Perioden *Fast SMA* und *Slow SMA*.
2. Berechnen Sie den aktuellen Spread (`fast - slow`) und vergleichen Sie ihn mit dem vorherigen Spread, um den Divergenzwert zu erhalten.
3. Geben Sie „long“ ein, wenn die Divergenz positiv ist, größer oder gleich dem *Kaufschwellenwert* und kleiner oder gleich dem *Stay-Out-Schwellenwert*.
4. Geben Sie kurz ein, wenn die Divergenz negativ, kleiner oder gleich `-Buy Threshold` und größer oder gleich `-Stay Out Threshold` ist.
5. Kehren Sie eine bestehende Position um, wenn ein entgegengesetztes Signal auftritt.
6. Beschränken Sie neue Einträge auf das lokale Zeitfenster zwischen *Start Hour* und *Stop Hour* (Umbruch über Mitternacht wird unterstützt).

## Risikomanagement

* Optionale feste Werte für *Take Profit (Pips)* und *Stop Loss (Pips)* werden auf Kerzenhochs/-tiefs überwacht.
* Der *Break-Even-Trigger (Pips)* verschiebt den Stop auf `entry ± Break-Even Buffer`, sobald die Position die angegebene Anzahl an Pips erreicht.
* Der *Trailing Stop (Pips)* folgt dem günstigsten Preis, sobald der Handel profitabel ist. Durch die Einstellung 9999 wird der Trailing Stop deaktiviert und der ursprüngliche EA-Standardwert widergespiegelt.
* Das Korbmanagement schließt alle offenen Positionen, wenn die nicht realisierten Gewinne und Verluste den *Korbgewinn* erreichen oder unter `-Basket Loss` in der Kontowährung fallen.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Order Volume` | Volumen, das beim Eröffnen einer neuen Position verwendet wird. |
| `Fast SMA` / `Slow SMA` | Perioden für die beiden einfachen gleitenden Durchschnitte. |
| `Applied Price` | Kerzenkomponente wird in beide gleitenden Durchschnitte weitergeleitet. |
| `Buy Threshold` | Untere Divergenzgrenze, die Long-Trades ermöglicht. |
| `Stay Out Threshold` | Obere Divergenzgrenze, oberhalb derer keine neuen Geschäfte getätigt werden. |
| `Take Profit (pips)` / `Stop Loss (pips)` | Optionale Hard-Exits, gemessen in Pips. |
| `Trailing Stop (pips)` | Nachlaufdistanz, die angewendet wird, nachdem der Handel profitabel wird. |
| `Break-Even Trigger (pips)` | Erforderlicher Gewinn in Pips, bevor der Stopp auf die Gewinnschwelle verschoben wird. |
| `Break-Even Buffer (pips)` | Zusätzlicher Puffer zum Break-Even-Stopp hinzugefügt. |
| `Basket Profit` / `Basket Loss` | Globale Eigenkapitalgrenzen in Kontowährung. |
| `Start Hour` / `Stop Hour` | Lokales Handelssitzungsfenster. |
| `Candle Type` | Zeitrahmen für Kerzenabonnements und Berechnungen. |

## Nutzungshinweise

* Hängen Sie die Strategie an ein Wertpapier an und legen Sie den Kerzentyp fest, der dem ursprünglichen Zeitrahmen des Diagramms entspricht.
* Stellen Sie sicher, dass die `PriceStep`/`StepPrice`-Eigenschaften des Instruments so konfiguriert sind, dass Pip-basierte Steuerungen ordnungsgemäß funktionieren.
* Um Funktionen wie Trailing Stop oder Break-Even-Shift zu deaktivieren, belassen Sie deren Parameter auf dem alten Sentinel-Wert (9999) oder Null.
