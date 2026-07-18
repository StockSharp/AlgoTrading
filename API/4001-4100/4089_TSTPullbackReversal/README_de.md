# TST-Pullback-Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **TST Pullback Reversal Strategy** achtet auf aggressive Intrabar-Umkehrungen. Es wurde vom ursprünglichen MetaTrader 4 Expert Advisor `TST.mq4` konvertiert und unter Verwendung des High-Level-StockSharp API neu erstellt. Die Methode sucht nach Kerzen, bei denen sich der Preis stark von der Kerzenöffnung entfernt hat, nachdem er ein Intraday-Extrem erreicht hat, und schwächt diese Bewegung dann in Erwartung einer Mean-Reversion ab. Die Strategie handelt sowohl Long- als auch Short-Positionen und verwendet statische Stop-Loss- und Take-Profit-Level, die in Preisschritten gemessen werden.

## Signallogik
- **Lange Einrichtung**
  1. Die Kerze schließt unter ihrem Eröffnungswert (`Open > Close`).
  2. Der Abstand zwischen dem Kerzenhoch und dem Schlusskurs ist größer als `GapPoints * PriceStep`.
  3. Auf demselben Balken wurde zuvor kein Trade ausgeführt.
Wenn die Strategie zufrieden ist, schließt sie alle Short-Positionen und kauft `OrderVolume` Einheiten (zuzüglich der Größe, die zum Umwandeln von einer Short- in eine Long-Position erforderlich ist).

- **Kurze Einrichtung**
  1. Die Kerze schließt über ihrem Eröffnungskurs (`Close > Open`).
  2. Der Abstand zwischen dem Schlusskurs und dem Kerzentief ist größer als `GapPoints * PriceStep`.
  3. Auf demselben Balken wurde zuvor kein Trade ausgeführt.
Wenn die Strategie zufrieden ist, schließt sie alle Long-Positionen und verkauft `OrderVolume` Einheiten (zuzüglich der Größe, die zum Umwandeln von einer Long- in eine Short-Position erforderlich ist).

## Positionsmanagement
- Ein neuer Trade weist sofort statische Stop-Loss- und Take-Profit-Level zu, die aus dem Füllpreis und den Parametern `StopLossPoints`/`TakeProfitPoints` berechnet werden.
- Bei jeder fertigen Kerze überprüft die Strategie das Hoch/Tief der Kerze, um festzustellen, ob der Stopp oder das Ziel berührt wurde, und verlässt die Position, wenn dies ausgelöst wird. Stop-Loss-Checks haben Vorrang vor Take-Profit-Checks.
- Nach einem Ausstieg werden die gespeicherten Risikostufen gelöscht, aber die Strategie merkt sich weiterhin die Balkenzeit, um einen erneuten Eintritt während derselben Kerze zu vermeiden (Reproduzieren des `NevBar()`-Schutzes aus der MQL4-Version).

## Parameter
- `StopLossPoints` (Standard `500`): Abstand vom Einstieg bis zum Schutzstopp, ausgedrückt in Preisschritten.
- `TakeProfitPoints` (Standard `100`): Abstand vom Einstieg bis zum Gewinnziel, ausgedrückt in Preisschritten.
- `GapPoints` (Standardwert `500`): Mindestrückgang zwischen dem Kerzenextrem und dem Schlusskurs, der erforderlich ist, um ein Signal zu erzeugen.
- `OrderVolume` (Standard `0.1`): Menge, die mit jeder neuen Market-Order gesendet wird.
- `CandleType` (Standard `1 hour`): Zeitrahmen der über `SubscribeCandles` bereitgestellten Kerzen.

Alle entfernungsbasierten Einstellungen werden mit dem `PriceStep` des Instruments multipliziert. Wenn das Wertpapier keinen Schritt meldet, fällt die Strategie auf `1` zurück.

## Implementierungshinweise
- Die Konvertierung verwendet die übergeordnete API von StockSharp und erstellt keine benutzerdefinierten Indikatorsammlungen.
- Um mit dem Strategy Designer kompatibel zu bleiben, werden nur fertige Kerzen verarbeitet; Dies nähert sich den Intrabar-Entscheidungen des MT4-Roboters an, indem vervollständigte Bardaten verwendet werden.
- Ein dediziertes Flag `_lastSignalBarTime` repliziert den `NevBar()`-Guard aus dem MQL4-Code, sodass nur eine Order pro Kerze geöffnet werden kann.
- Die Handhabung des Ordervolumens spiegelt das MT4-Verhalten wider: Bestehende gegensätzliche Positionen werden abgeflacht, bevor die neue Richtung in einer einzelnen Marktorder festgelegt wird.
- Stop-Loss- und Take-Profit-Orders werden innerhalb der Strategielogik simuliert (anstelle serverseitiger Orders), um den ursprünglichen Abständen zu entsprechen und gleichzeitig die Kontrolle innerhalb von StockSharp zu behalten.

## Nutzungstipps
- Wählen Sie `GapPoints` relativ zur Volatilität des gehandelten Instruments; Größere Werte verringern die Handelshäufigkeit, filtern jedoch kleinere Rückschläge.
- Da Stop- und Zielprüfungen auf fertigen Kerzen basieren, sollten Sie kürzere Kerzendauern in Betracht ziehen, wenn Sie eine engere Ausführung benötigen.
- Kombinieren Sie die Strategie mit zusätzlichen Filtern (Trend, Tageszeit, Volumen), wenn Sie sie auf Live-Märkten einsetzen, um Whipsaw-Trades zu reduzieren.
