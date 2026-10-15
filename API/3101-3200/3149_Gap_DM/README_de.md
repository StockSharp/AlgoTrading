# Gap DM-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Gap DM ist eine konträre Gap-Trading-Strategie, die den Abstand zwischen dem Schluss der vorherigen Sitzung und der Eröffnung der nächsten Sitzung verfolgt. Wenn der Markt mit einem sichtbaren Gap eröffnet, handelt die Strategie sofort in die entgegengesetzte Richtung und erwartet, dass der Preis umkehrt und das Gap füllt. Die Implementierung folgt dem ursprünglichen MetaTrader 5-Algorithmus "Gap DM" von cmillion, angepasst an StockSharpss High-Level-API. Alle Handelsentscheidungen werden aus abgeschlossenen Kerzen des gewählten Zeitrahmens abgeleitet und gewährleisten deterministisches Verhalten in Backtests und Live-Ausführung.

## Signallogik
1. Die durch `CandleType` angegebene Kerzenreihe abonnieren.
2. Warten, bis jede Kerze abgeschlossen ist (`CandleStates.Finished`).
3. Den Schlusskreis der vorherigen Kerze mit dem Eröffnungskurs der aktuellen Kerze vergleichen.
4. Die Gap-Größe in Pips berechnen unter Verwendung des Preisschritts des Instruments. Ein Multiplikator von 10 wird automatisch für 3- und 5-stellige Kurse angewendet, reproduzierend die MT5-Punkt-zu-Pip-Umrechnung.
5. Wenn die aktuelle Eröffnung **unterhalb** des vorherigen Schlusses um mindestens `Minimum Gap (pips)` liegt, als bärisches Gap behandeln und **Long einsteigen**.
6. Wenn die aktuelle Eröffnung **oberhalb** des vorherigen Schlusses um mindestens `Minimum Gap (pips)` liegt, als bullisches Gap behandeln und **Short einsteigen**.
7. Einstiege überspringen, wenn der Handel nicht erlaubt ist (z.B. Strategie ist getrennt oder noch im Aufwärmen).

## Positionsgrößenbestimmung und Limits
- `Order Volume` gibt die Lotgröße für jeden neuen Trade an. Die Strategie verwendet den Wert auch zum Schließen oder Umkehren bestehender Exposition, um die Nettoposition konsistent mit StockSharpss Netto-Buchungsmodell zu halten.
- `Max Positions` definiert das maximale aggregierte Volumen (in Lots), das in einer Richtung gehalten werden kann. Wenn das Limit erreicht ist, werden neue Einstiege in derselben Richtung ignoriert.
- Beim Umkehren von Short zu Long (oder umgekehrt) fügt die Strategie automatisch das notwendige Volumen hinzu, um die entgegengesetzte Exposition zu schließen, bevor die neue Position eröffnet wird.

## Risikomanagement
- `Stop Loss (pips)` platziert einen schützenden Stop relativ zum Einstiegspreis. Der Stop wird bei jeder abgeschlossenen Kerze ausgewertet. Wenn der Kerzenbereich durch das Stop-Niveau handelt, wird die Position sofort mit einer Marktorder geschlossen.
- `Take Profit (pips)` funktioniert symmetrisch zum Stop-Loss. Den Parameter auf null setzen, um das Ziel zu deaktivieren.
- Standardmäßig wird kein Trailing Stop angewendet; die Ausstiegslogik entspricht dem Quell-Expertenberater.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Order Volume` | Handelsvolumen für jeden Einstieg in Lots. | `1` |
| `Stop Loss (pips)` | Abstand des schützenden Stops. Auf `0` setzen zum Deaktivieren. | `0` |
| `Take Profit (pips)` | Abstand des Gewinnziels. Auf `0` setzen zum Deaktivieren. | `0` |
| `Minimum Gap (pips)` | Minimale Differenz zwischen dem vorherigen Schluss und der aktuellen Eröffnung, die für ein Signal erforderlich ist. | `1` |
| `Max Positions` | Maximale aggregierte Exposition in einer einzelnen Richtung (in Lots). | `15` |
| `Candle Type` | Zeitrahmen zur Messung von Sitzungsgaps. | `1 Stunde` |

## Ausführungsablauf
1. Gecachten Zustand bei jedem Neustart zurücksetzen (Gap-Schwellenwerte, Stop-Niveaus, vorheriger Schluss).
2. Kerzenabonnement starten und Chart-Elemente (Kerzen und Trades) zeichnen, wenn ein Chart-Bereich verfügbar ist.
3. Bei jeder fertigen Kerze:
   - Den aktiven Stop und das Ziel je nach aktueller Position aktualisieren oder zurücksetzen.
   - Gap-Bedingungen auswerten und Marktorders platzieren, wenn ein gültiges Signal erscheint.
   - Schutz-Orders neu prüfen, damit Stop-Loss- oder Take-Profit-Ereignisse innerhalb derselben Kerze ohne Verzögerung behandelt werden.
4. Den letzten Schluss für die nächste Auswertung speichern.

## Hinweise und Unterschiede zur originalen MT5-Version
- StockSharp-Strategien operieren mit Nettopositionen. Der Algorithmus emuliert mehrere Einstiege durch Skalierung der Netto-Exposition statt durch Erstellen separater Tickets.
- Alle Kommentare im Quellcode sind auf Englisch, konform mit den Projektrichtlinien.
- Geldverwaltung über Risikoanteil (Modus `risk` im MT5-Skript) wird nicht reproduziert; stattdessen wird ein fester Volumenparameter bereitgestellt.

## Anforderungen
- Kompatibel mit jedem Instrument, das einen gültigen `PriceStep` exponiert.
- Funktioniert mit zeit-, volumen- oder rangbasierten Kerzen, die von StockSharp unterstützt werden, solange das Gap-Konzept sinnvoll ist.
- Erfordert eine StockSharp-Umgebung, die Marktorders ausführen und eigene Trades überwachen kann.
