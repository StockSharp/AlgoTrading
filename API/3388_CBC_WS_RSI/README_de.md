# CBC_WS_RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **CBC_WS_RSI-Strategie** ist eine High-Level-StockSharp-Implementierung des MQL5-Expertenberaters, der die Candlestick-Muster „Three White Soldiers“ und „Three Black Crows“ mit einer RSI-Bestätigung kombiniert. Die Strategie konzentriert sich auf die Identifizierung starker Multi-Candle-Umkehrungen und geht nur dann einen Handel ein, wenn die Marktdynamik, gemessen durch RSI, mit dem Muster übereinstimmt. Ausstiege werden durch Schwellenwertüberschreitungen von RSI und optionales Risikomanagement durch Stop-Loss- und Take-Profit-Schutz kontrolliert.

Die Strategie abonniert eine konfigurierbare Kerzenserie und verarbeitet ausschließlich Daten zu vollständig geformten Kerzen. Die gesamte Logik wird mithilfe des High-Level-API (`SubscribeCandles().Bind(...)`) von StockSharp ohne direkten Zugriff auf Indikatorpuffer implementiert.

## Handelslogik
### Lange Einrichtung
1. Erkennt drei aufeinanderfolgende bullische Kerzen, die das **Three White Soldiers**-Muster bilden:
   - Jede Kerze schließt über ihrem Eröffnungskurs.
   - Jeder Schlusskurs ist höher als der vorherige Schlusskurs.
   - Die zweite und dritte Kerze öffnen sich im Körper der vorherigen Kerze.
2. Bestätigt, dass der RSI-Wert der aktuellen Kerze **unter oder gleich dem langen Bestätigungsniveau** liegt (Standard 40).
3. Wenn das Konto flach ist, kauft die Strategie `Volume` Lots zum Marktwert. Wenn eine Short-Position besteht, wird diese abgedeckt, bevor eine neue Long-Position eröffnet wird.

### Kurze Einrichtung
1. Erkennt drei aufeinanderfolgende bärische Kerzen, die das Muster **Three Black Crows** bilden:
   - Jede Kerze schließt unterhalb ihrer Eröffnung.
   - Jeder Schlusskurs ist niedriger als der vorherige Schlusskurs.
   - Die zweite und dritte Kerze öffnen sich im Körper der vorherigen Kerze.
2. Bestätigt, dass der RSI-Wert der aktuellen Kerze **über oder gleich dem Short-Bestätigungsniveau** liegt (Standard 60).
3. Wenn das Konto flach ist, verkauft die Strategie `Volume` Lose zum Marktwert. Wenn eine Long-Position besteht, wird diese geschlossen, bevor eine neue Short-Position eröffnet wird.

### Ausgangsregeln
- **Long-Positionen schließen:** RSI unterschreitet entweder das obere Ausgangsniveau (Standard 70) oder das untere Ausgangsniveau (Standard 30).
- **Shorts schließen:** RSI überschreitet entweder die untere Ausgangsebene (Standard 30) oder die obere Ausgangsebene (Standard 70).
- **Schutz:** Optionale Stop-Loss- und Take-Profit-Werte können als Prozentsätze des Einstiegspreises definiert werden. Wenn sie ungleich Null sind, werden sie über `StartProtection` verwaltet.

Alle Ausstiegsbedingungen verwenden die letzten beiden RSI-Werte, um einen Level-Crossover zu erkennen und sicherzustellen, dass Trades geschlossen werden, sobald das Momentum der aktiven Position widerspricht.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Kerzendatentyp und Zeitrahmen zum Abonnieren. | 1-stündiger Zeitrahmen |
| `RsiPeriod` | RSI Zeitraum für die Bestätigung verwendet. | 37 |
| `LongConfirmationLevel` | Maximaler RSI-Wert, der eine lange Eingabe zulässt. | 40 |
| `ShortConfirmationLevel` | Mindestwert RSI, der eine kurze Eingabe zulässt. | 60 |
| `LowerExitLevel` | Das Niveau RSI wird verwendet, um eine Momentumumkehr in der Nähe des überverkauften Bereichs zu erkennen. | 30 |
| `UpperExitLevel` | Das Niveau RSI wird verwendet, um eine Momentumumkehr in der Nähe des überkauften Bereichs zu erkennen. | 70 |
| `StopLossPercent` | Optionaler Stop-Loss in Prozent; 0 deaktiviert den Schutz. | 1 |
| `TakeProfitPercent` | Optionaler Take-Profit in Prozent; 0 deaktiviert den Schutz. | 2 |

Alle numerischen Parameter können dank `SetCanOptimize(true)` über den integrierten Optimierer optimiert werden.

## Visualisierung
Wenn ein Diagrammbereich verfügbar ist, zeichnet die Strategie Folgendes:
- Die ausgewählte Kerzenserie.
- Der Indikator RSI.
- Ausgeführte Trades erleichtern die Überprüfung von Mustererkennungen und -ausstiegen.

## Nutzungshinweise
- Stellen Sie sicher, dass `Volume` konfiguriert ist, bevor Sie mit der Strategie beginnen.
- Funktioniert auf jedem Instrument, das OHLC Kerzendaten unterstützt.
- Die Mustererkennungslogik filtert Doji-ähnliche Kerzen heraus, indem sie Kerzenkörper ungleich Null erfordert.
- RSI-Bestätigungen schützen vor falschen Signalen bei schwachen Umkehrungen und halten die Strategie im Einklang mit der Dynamik.
