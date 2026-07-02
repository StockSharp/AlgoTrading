# SurefireThing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die SurefireThing-Strategie ist eine StockSharp High-Level-Portierung des MetaTrader 4 Expertenberaters *Surefirething*. Es arbeitet mit abgeschlossenen Kerzen, berechnet die Höhe der ausstehenden Aufträge aus dem Bereich der vorherigen Sitzung und setzt das Engagement am Ende jedes Handelstages zurück. Die Logik basiert auf der Bereitstellung eines symmetrischen Paars von Limit-Orders, die versuchen, eine Mean-Reversion um den vorherigen Schlusskurs herum zu erfassen.

## Handelslogik
- Am Ende jedes Handelstages versucht die Strategie, die Position zu reduzieren und alle aktiven ausstehenden Aufträge zu stornieren.
- Anhand der letzten abgeschlossenen Kerze des Vortages wird die Preisspanne `(High - Low)` gemessen und mit `RangeMultiplier` multipliziert (standardmäßig 1,1 wie im Original EA).
- Die Hälfte der angepassten Spanne wird zum vorherigen Schlusskurs addiert, um den Verkaufslimit-Einstiegspreis zu erhalten. Der gleiche Abstand wird vom Schlusskurs abgezogen, um die Kauf-Limit-Order zu platzieren.
- Stop-Loss- und Take-Profit-Offsets werden in Preisschritten ausgedrückt. Wenn das Instrument einen gültigen `Security.Step` anzeigt, werden diese in absolute Distanzen umgewandelt und über `StartProtection` verwaltet, sodass besetzte Positionen automatisch Schutzbefehle erhalten.
- Die Auftragserteilung erfolgt einmal pro Handelstag. Wenn Füllungen auftreten, verarbeitet der angehängte Schutz Exits; andernfalls bleiben Bestellungen bis zum nächsten täglichen Reset aktiv.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Mit jeder ausstehenden Bestellung übermitteltes Volumen. | `0.1` |
| `TakeProfitPoints` | Abstand (in Preisschritten) zum Gewinnziel. Wird in einen absoluten Offset umgewandelt, wenn der Schritt bekannt ist. | `10` |
| `StopLossPoints` | Abstand (in Preisschritten) für den Schutzanschlag. Auf die gleiche Weise umgerechnet wie das Gewinnziel. | `15` |
| `RangeMultiplier` | Faktor, der vor der Berechnung der Einstiegspreise auf den vorherigen Kerzenbereich angewendet wird. | `1.1` |
| `CandleType` | Primärer Zeitrahmen, der von der Strategie verarbeitet wird. Standardmäßig werden 1-Minuten-Kerzen verwendet, können aber an das Originaldiagramm angepasst werden. | `TimeSpan.FromMinutes(1)` |

## Implementierungshinweise
- High-Level API: Kerzen werden durch `SubscribeCandles(CandleType)` verbraucht und im `ProcessCandle`-Handler verarbeitet, sobald sie fertig sind.
- Tägliches Zurücksetzen: `CloseForNewDay` storniert ausstehende Aufträge und schließt Positionen, wenn anhand der Kerzenzeitstempel ein neuer Kalendertag erkannt wird.
- Schutzlogik: `ConfigureProtection` übersetzt die punktbasierten Risikokontrollen in `Unit` Instanzen und aktiviert `StartProtection`, sodass Stop-Loss- und Take-Profit-Orders nach Ausführungen automatisch neu erstellt werden.
- Auftragslebenszyklus: Verweise auf beide ausstehenden Aufträge werden gespeichert und über `CancelPendingOrder` sowie `OnOrderChanged` gelöscht, wenn die Aufträge abgeschlossen oder storniert werden.
- Preisnormalisierung: `Security.ShrinkPrice` wird verwendet, um berechnete Preise auf die Tick-Größe des Instruments zu runden, bevor neue Aufträge übermittelt werden.

## Nutzungsempfehlungen
- Richten Sie `CandleType` an dem Zeitrahmen aus, der vom ursprünglichen EA verwendet wurde (normalerweise das Diagramm, an dem es angehängt wurde), um die gleichen Referenzkerzen beizubehalten.
- Passen Sie `RangeMultiplier` an, wenn Instrumente unterschiedliche Volatilitätseigenschaften aufweisen, damit die ausstehenden Aufträge in realistischen Abständen bleiben.
- Wenn der Broker Mindesthalteabstände vorschreibt, stellen Sie sicher, dass `TakeProfitPoints` und `StopLossPoints` diese Einschränkungen nach der Umrechnung in absolute Preise einhalten.
- Die Strategie geht von kontinuierlichen Intraday-Daten aus. Wenn große Lücken auftreten (Wochenenden, Feiertage), löst die nächste verfügbare Kerze immer noch einen Reset und eine neue Auftragserteilung basierend auf dem zuletzt beobachteten Balken aus.
