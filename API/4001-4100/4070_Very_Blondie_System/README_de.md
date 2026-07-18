# Sehr Blondie-System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Das Very Blondie System ist eine kurzfristige Mean-Reversion-Grid-Strategie, die ursprünglich als MetaTrader 4 Expert Advisor „VBS – Very Blondie System“ vertrieben wurde. Der Port behält die ursprüngliche Idee bei, einen Ausbruch aus der jüngsten Handelsspanne abzuschwächen: Wenn sich der Preis weit genug vom höchsten Hoch oder tiefsten Tief der letzten `PeriodX` Kerzen entfernt, beginnt die Strategie sofort mit einer Marktorder und fügt vier Limit-Orders im Martingal-Stil hinzu, um sich an die Bewegung anzupassen, wenn der Preis weiter steigt.

## Daten und Indikatoren
- **Primärdaten**: eine einzelne Kerzenserie, die durch den Parameter `CandleType` konfiguriert wird (Version MQL wird im Zeitrahmen des Diagramms gehandelt).
- **Indikatoren**: Die Indikatoren `Highest` und `Lowest` (Länge = `PeriodLength`) verfolgen die gleitenden Bereichsextreme, die für die Ausbruchserkennung verwendet werden.
- **Quotes der Stufe 1**: Die besten Geld-/Briefkurse werden verwendet, um Markt- und Limitaufträge zum ursprünglichen MT4-Offset zu platzieren.

## Eingabelogik
1. Berechnen Sie für jede fertige Kerze das höchste Hoch und das niedrigste Tief der letzten `PeriodLength` Balken.
2. Lesen Sie den aktuell besten Geld-/Briefkurs (Fallback auf den Kerzenschluss, wenn Quotes fehlen).
3. **Lange Einrichtung**: Wenn `highest - bid > LimitPoints * PointValue`, senden Sie eine Kauf-Market-Order mit dem Basisvolumen und platzieren Sie vier Kauf-Limit-Orders unter dem Brief. Jede Limit-Order liegt `GridPoints * PointValue` weiter entfernt und verdoppelt das Volumen der vorherigen Order (1×, 2×, 4×, 8×, 16×).
4. **Kurzes Setup**: Wenn `bid - lowest > LimitPoints * PointValue`, senden Sie eine Verkaufs-Market-Order und vier Verkaufs-Limit-Orders über dem Gebot in den gleichen Abständen und Volumenmultiplikatoren wie die Kauflogik.
5. Es kann jeweils nur ein Warenkorb aktiv sein. Neue Signale werden ignoriert, bis alle Positionen und ausstehenden Aufträge aus dem vorherigen Zyklus verschwunden sind.

## Positionsmanagement
- **Floating-Gewinnziel**: Der ursprüngliche `Amount`-Parameter überwacht `OrderProfit + OrderSwap` über alle Trades hinweg. Der Port gibt dies mit der aggregierten Position wieder: `(close - entryPrice) * position * conversionFactor >= ProfitTarget`. Wenn der Schwellenwert erreicht ist, wird jede Position mit Marktaufträgen geschlossen und alle verbleibenden Rasteraufträge werden storniert.
- **Lockdown-Breakeven**: Bei `LockDownPoints > 0` verschob der MT4-Code den Stop-Loss jeder ausgeführten Order auf `entry price ± Point`, sobald der Trade einen Gewinn von `LockDownPoints` Punkten hatte. Die StockSharp-Version verfolgt die Nettoposition; Sobald der Preis um `LockDownPoints * PointValue` steigt, liegt die Gewinnschwelle bei `entryPrice ± PointValue`. Wenn eine spätere Kerze dieses Niveau berührt (tief für Long-Positionen, hoch für Shorts), wird der gesamte Korb abgeflacht und alle ausstehenden Orders werden storniert.
- **Manuelle Ausstiege**: Das Stoppen der Strategie oder das Erreichen der Gewinn-/Break-Even-Bedingungen storniert immer die vier ausstehenden Limit-Orders, um die `CloseAll()`-Routine von MT4 nachzuahmen.

## Money-Management
- **Basisvolumen**: entspricht dem MT4-Ausdruck `MathRound(AccountBalance()/100) / 1000`. Die Strategie liest den aktuellen Portfoliowert (oder den Anfangswert, wenn keine Geschäfte getätigt wurden), rundet ihn von Null ab und wandelt ihn in Lots um. Das Ergebnis wird an `Security.VolumeStep` ausgerichtet, folgt `MinVolume`/`MaxVolume` und greift auf die Strategie `Volume` (oder `1`) zurück, wenn der Portfolio-Snapshot nicht verfügbar ist.
- **Martingale-Raster**: Jede zusätzliche Limit-Order verdoppelt das Grundvolumen auf bis zu vier Stufen (1×, 2×, 4×, 8×, 16×). Die Volumina werden mit demselben Helfer normalisiert, um zu vermeiden, dass Bruchteile von Losen gesendet werden, die der Veranstaltungsort ablehnt.
- **PointValue-Parameter**: MT4s `Point` kann von `Security.PriceStep` abweichen (insbesondere bei 5-stelligen FX-Kursen). `PointValue` verwendet standardmäßig die automatische Erkennung von `PriceStep`/`Step`, Sie können diese jedoch überschreiben, um das Verhalten des ursprünglichen EA genau anzupassen.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `PeriodLength` | Lookback-Fenster für das höchste Hoch und das niedrigste Tief | `60` |
| `LimitPoints` | Mindestabstand (in MT4-Punkten) zwischen dem aktuellen Preis und dem Extremwert der Spanne, um einen Korb auszulösen | `1000` |
| `GridPoints` | Abstand (in MT4-Punkten) zwischen aufeinanderfolgenden Rasterreihenfolgen | `1500` |
| `ProfitTarget` | Variables Gewinnziel, ausgedrückt in der Kontowährung | `40` |
| `LockDownPoints` | Gewinndistanz (in MT4-Punkten), die den Break-even-Ausgang ermöglicht | `0` |
| `PointValue` | Preisänderung durch einen MT4-Punkt (`0` = automatische Erkennung) | `0` |
| `CandleType` | Kerzenserien dienten als Antrieb für die Strategie | `TimeFrameCandle, 1 minute` |

## Portierungshinweise
- Der variable PnL wird anhand der aggregierten Position angenähert, anstatt die `OrderProfit + OrderSwap` jeder Bestellung zu summieren. Dies entspricht dem ursprünglichen Verhalten, wenn alle Trades in die gleiche Richtung verlaufen, wie es bei EA der Fall ist.
- Die Stop-Loss-Modifikation wird durch einen sofortigen Marktausstieg zum bewaffneten Break-Even-Preis nachgeahmt; StockSharp behält die Logik in der Strategieebene bei, anstatt `OrderModify` Anfragen zu senden.
- Ausstehende Limitaufträge werden mit normalisierten Preisen unter Verwendung von `Security.ShrinkPrice` registriert. Wenn in den Sicherheitsmetadaten ein `PriceStep` fehlt, legen Sie `PointValue` manuell fest, um falsch ausgerichtete Raster zu vermeiden.
- Die Strategie geht von einem Instrument aus und verwendet hochrangige API-Helfer (`SubscribeCandles`, `SubscribeLevel1`, `BuyLimit`, `SellLimit` usw.), wie in den Konvertierungsrichtlinien gefordert.
