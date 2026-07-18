# Volatilitätsausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Volatilitätsausbruch-Strategie sucht nach kurzen Ausbrüchen der Intrabar-Volatilität. Sie wartet auf eine Kerze, deren Spanne über die der vorherigen Kerze hinaus wächst, jedoch nur innerhalb eines engen Bands (zwei Pip-Äquivalente nach der Stellen-Normalisierung). Schließt eine solche Kerze bullisch, kauft die Strategie; schließt sie bärisch, verkauft sie. Schutz-Stops, ein optionaler Trailing Stop und eine automatische Reverse-on-Loss-Sequenz steuern das Risiko und versuchen, ungünstige Bewegungen auszugleichen.

## Handelslogik

1. **Filter für Spannenausweitung**
   - Berechnen Sie die aktuelle Kerzenspanne (`High - Low`) und vergleichen Sie sie mit der vorherigen Kerze.
   - Verlangen Sie, dass die aktuelle Spanne größer ist, die vorherige Spanne jedoch um nicht mehr als zwei normalisierte Pips überschreitet.
   - Dadurch entsteht ein Setup, in dem die Volatilität zunimmt, aber weiterhin begrenzt bleibt, was auf einen möglichen Ausbruch ohne übermäßiges Rauschen hindeutet.
2. **Richtungsneigung**
   - Wenn die Kerze über ihrem Eröffnungskurs schließt, wird Long eingestiegen.
   - Wenn die Kerze unter ihrem Eröffnungskurs schließt, wird Short eingestiegen.
   - Die Strategie kann optional mehr als einen Einstieg pro Bar verbieten, um wiederholte Signale auf derselben Kerze zu vermeiden.
3. **Positionsverwaltung**
   - Anfänglicher Stop-Loss und Take-Profit werden in Punkten (Pip-Äquivalenten) relativ zum Einstiegspreis gesetzt.
   - Ein optionaler Trailing Stop zieht das Schutzniveau enger, sobald sich der Preis um eine bestimmte Distanz zugunsten des Trades bewegt hat. Ein Trailing-Schritt verhindert winzige Anpassungen.
   - Wird eine Position mit Verlust geschlossen, kann die Strategie die Richtung sofort umkehren. Jede Umkehr erhöht die Take-Profit-Distanz, um das zusätzliche Risiko zu kompensieren. Eine Obergrenze für die Anzahl aufeinanderfolgender Umkehrungen verhindert unbegrenztes Martingale-Verhalten.

## Parameter

| Name | Beschreibung | Standard | Optimierbar |
| --- | --- | --- | --- |
| `TradeVolume` | Basis-Ordervolumen für Markteinstiege. | `0.1` | Ja |
| `StopLossPoints` | Stop-Loss-Distanz in Punkten. | `20` | Ja |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. | `10` | Ja |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Punkten. Auf `0` setzen, um zu deaktivieren. | `25` | Nein |
| `TrailingStepPoints` | Minimaler inkrementeller Schritt beim Verschieben des Trailing Stops. | `5` | Nein |
| `OnlyOnePositionPerBar` | Verbietet mehrere Einstiege während derselben Kerze. | `true` | Nein |
| `UseAutoDigits` | Multipliziert die Punktgröße bei Symbolen mit 3 oder 5 Dezimalstellen mit 10, um in Pip-Einheiten umzuwandeln. | `true` | Nein |
| `ReverseAfterStop` | Aktiviert den Reverse-on-Loss-Ablauf. | `true` | Nein |
| `MaxReverseOrders` | Maximale Anzahl aufeinanderfolgender Reverse-Trades. | `2` | Nein |
| `TakeProfitIncrease` | Zusätzliche Take-Profit-Punkte, die für jede Reverse-Order hinzugefügt werden. | `100` | Nein |
| `CandleType` | Kerzentyp und Zeitrahmen für Berechnungen. | `TimeSpan.FromMinutes(1)` | Nein |

## Risikomanagement

- Stop-Loss- und Take-Profit-Offsets werden anhand des Instrumenten-Preisschritts neu berechnet. Die automatische Stellen-Erkennung wandelt Fünfstellenkurse in pipgroße Distanzen um.
- Die Trailing-Logik wird erst aktiviert, nachdem der Markt um die angegebene Trailing-Distanz vorangekommen ist, und erzwingt einen Mindestschritt vor der Stop-Anpassung.
- Reverse-Trading wird nach einem profitablen Ausstieg oder nach Erreichen der konfigurierten Grenze aufeinanderfolgender Umkehrungen zurückgesetzt.

## Praktische Hinweise

- Funktioniert am besten bei Währungspaaren mit engen Spreads, bei denen kleine Volatilitätsänderungen Momentum-Schübe anzeigen können.
- Erwägen Sie, den Kerzenzeitrahmen an die Zielmarktsitzung anzupassen; der Standardzeitrahmen von 1 Minute erfasst hochfrequente Ausbrüche.
- Da Umkehrungen unmittelbar nach einem Verlustschluss ausgeführt werden, sollte genügend Margin für aufeinanderfolgende Trades verfügbar sein.
