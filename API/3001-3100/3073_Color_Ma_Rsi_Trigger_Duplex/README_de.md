# Color Ma RSI Trigger Duplex Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den Experten-Berater **Exp_ColorMaRsi-Trigger_Duplex.mq5** auf die StockSharp High-Level-API.
Sie betreibt zwei unabhängige MaRsi-Trigger-Detektoren: Der **Long-Block** entscheidet, wann Long-Positionen geöffnet oder geschlossen werden sollen, während der **Short-Block** dieselbe Aufgabe für Short-Positionen übernimmt. Jeder Detektor bewertet, ob ein benutzerdefinierter Indikator bullischen (`+1`), neutralen (`0`) oder bärischen (`-1`) Marktdruck meldet. Die ursprüngliche MetaTrader-Logik wird beibehalten, einschließlich der verzögerten Bestätigung, die auf zwei abgeschlossene Balken wartet, bevor sie reagiert, und der separaten Geldverwaltungseinstellungen pro Richtung.

## Trading-Idee

1. Berechnung zweier exponentieller gleitender Durchschnitte (schnell und langsam) und zweier RSI-Oszillatoren (schnell und langsam) auf einer wählbaren Kerzenserie für jeden Block.
2. Bei jeder abgeschlossenen Kerze gibt der Indikator `+1` zurück, wenn beide schnellen Studien ihre langsamen Gegenstücke übersteigen, `-1` wenn beide schwächer sind und `0` sonst. Der Rohwert wird auf den Bereich `[-1, 1]` begrenzt, wie im MT5-Indikator.
3. Die Strategie speichert eine fortlaufende Geschichte von Indikatorwerten. Für einen konfigurierten `SignalBar`-Versatz vergleicht sie den Wert der Kerze `SignalBar + 1` Perioden zurück (genannt `older`) mit dem Wert der Kerze `SignalBar` Perioden zurück (genannt `recent`).
4. Long-Logik:
   - Wenn `older < 0` schließt der Long-Block alle aktiven Long-Positionen (sofern Long-Ausstiege aktiviert sind).
   - Wenn `older > 0` **und** `recent <= 0` bereitet der Long-Block einen neuen Long-Einstieg vor (sofern Long-Einstiege aktiviert sind).
5. Die Short-Logik spiegelt den Long-Block:
   - Wenn `older > 0` verlässt der Short-Block bestehende Short-Positionen (wenn Short-Ausstiege aktiviert sind).
   - Wenn `older < 0` **und** `recent >= 0` öffnet der Block eine neue Short-Position (wenn Short-Einstiege aktiviert sind).
6. Optionale Stop-Loss- und Take-Profit-Niveaus, ausgedrückt in Instrumentenpreisschritten, schließen Positionen, wenn der Preis die konfigurierten Niveaus kreuzt.

Die beiden Blöcke können unterschiedliche Kerzen-Zeitrahmen und Preisquellen abonnieren, sodass der Benutzer das ursprüngliche Dual-Zeitrahmen-Verhalten replizieren oder alternative Kombinationen ausprobieren kann.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `LongCandleType`, `ShortCandleType` | Von Long- und Short-Block verwendete Kerzen-Datenserien. Standardmäßig 4-Stunden-Kerzen. |
| `LongVolume`, `ShortVolume` | Gehandeltes Marktvolumen, wenn der entsprechende Block eine neue Position eröffnet. |
| `LongAllowOpen`, `ShortAllowOpen` | Öffnen neuer Positionen für jeden Block aktivieren oder deaktivieren. |
| `LongAllowClose`, `ShortAllowClose` | Schließsignale für jeden Block aktivieren oder deaktivieren. |
| `LongStopLossPoints`, `ShortStopLossPoints` | Stop-Loss-Abstand gemessen in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `LongTakeProfitPoints`, `ShortTakeProfitPoints` | Take-Profit-Abstand gemessen in Preisschritten. Auf `0` setzen zum Deaktivieren. |
| `LongSignalBar`, `ShortSignalBar` | Anzahl abgeschlossener Balken zwischen der aktuellen Kerze und der für die Entscheidungslogik verwendeten. |
| `LongRsiPeriod`, `LongRsiLongPeriod`, `ShortRsiPeriod`, `ShortRsiLongPeriod` | Längen der schnellen und langsamen RSI-Oszillatoren. |
| `LongMaPeriod`, `LongMaLongPeriod`, `ShortMaPeriod`, `ShortMaLongPeriod` | Längen der schnellen und langsamen gleitenden Durchschnitte. |
| `LongRsiPrice`, `ShortRsiPrice` | Preisquelle für den schnellen RSI (Schluss, Eröffnung, Hoch, Tief, Median, typisch oder gewichtet). |
| `LongRsiLongPrice`, `ShortRsiLongPrice` | Preisquelle für den langsamen RSI. |
| `LongMaPrice`, `ShortMaPrice` | Preisquelle für den schnellen gleitenden Durchschnitt. |
| `LongMaLongPrice`, `ShortMaLongPrice` | Preisquelle für den langsamen gleitenden Durchschnitt. |
| `LongMaType`, `ShortMaType` | Gleitender-Durchschnitt-Methode für die schnelle Linie (einfach, exponentiell, geglättet oder gewichtet). |
| `LongMaLongType`, `ShortMaLongType` | Gleitender-Durchschnitt-Methode für die langsame Linie. |

## Handelsregeln

1. Warten, bis die ausgewählte Kerzenserie abgeschlossene Balken produziert und alle Indikatoren vollständig aufgewärmt sind.
2. Für jeden Block den MaRsi-Trigger-Wert berechnen und den Historienpuffer aktualisieren.
3. Wenn die Historie mindestens `SignalBar + 2` Einträge enthält, die Long- und Short-Bedingungen aus dem Abschnitt Trading-Idee auswerten.
4. Vor dem Öffnen einer Position neutralisiert die Strategie alle entgegengesetzten Exposures (wenn das entsprechende Schließ-Flag aktiviert ist). Ein neuer Long-Einstieg kauft beispielsweise genug Volumen, um eine Short-Position zu schließen, und fügt dann erst das Long-Volumen hinzu.
5. Nachdem eine Position eröffnet wurde, werden optionale Stop-Loss- und Take-Profit-Niveaus bei jeder abgeschlossenen Kerze überwacht.
6. Eröffnungs- und Schließorders werden als Marktorders über die High-Level-Hilfsmethoden `BuyMarket` und `SellMarket` gesendet.

## Risikomanagement

* Stops und Ziele werden mit `Security.PriceStep` gemessen. Wenn das Instrument keinen Preisschritt bereitstellt, wird ein Standardwert von `1` angenommen, entsprechend dem Verhalten vieler bestehender Strategien in diesem Repository.
* Long- und Short-Blöcke behalten unabhängige Stop- und Take-Einstellungen.
* Die Strategie platziert keine zusätzlichen Schutzorders (wie Trailing Stops); das Verhalten spiegelt den MT5-Experten, der Trades nur schließt, wenn der Indikator feuert oder der harte Stop/Ziel erreicht wird.

## Hinweise

* Der StockSharp-Port gibt Marktorders sofort nach Abschluss der auswertenden Kerze aus. In MetaTrader plante der Experte seine Orders für den Eröffnungszeitpunkt des nächsten Balkens über Zeitstempel-Offsets; beide Verhaltensweisen richten sich effektiv aus, da StockSharp das Signal verarbeitet, sobald die Kerze schließt.
* Der ursprüngliche EA exponierte mehrere Geldverwaltungsmodi (`LOT`, `BALANCE` usw.). StockSharp-Strategien arbeiten mit direkten Volumenwerten, daher hält der Port das Volumen als einfachen Parameter (`LongVolume`/`ShortVolume`).
* Slippage und Magic-Number-spezifische Logik aus der MT5-Hilfsbibliothek sind in StockSharp nicht notwendig und wurden weggelassen.
* Indikatorberechnungen nutzen die eingebauten StockSharp-Implementierungen für gleitende Durchschnitte und RSI; die Ausgabe wird auf `[-1, 1]` begrenzt, um dem ursprünglichen `ColorMaRsi-Trigger`-Indikator zu entsprechen.
