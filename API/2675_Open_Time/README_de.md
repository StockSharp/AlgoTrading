# Strategie Öffnungszeit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie Öffnungszeit ist ein zeitgesteuertes Handelssystem, das das Verhalten des MetaTrader 5 Expert Advisors *OpenTime* repliziert. Die Strategie beobachtet die Marktzeit anhand abgeschlossener Kerzen und öffnet Trades nur innerhalb eines konfigurierbaren Zeitfensters. Sie kann jede aktive Position während eines dedizierten Ausstiegsfensters schließen, einen optionalen Trailing Stop anwenden und grundlegende Stop-Loss- und Take-Profit-Regeln in Pips durchsetzen.

Im Gegensatz zur ursprünglichen Hedging-Version arbeitet dieser StockSharp-Port auf einem Netto-Portfolio: Wenn ein Signal erscheint, das mit der aktuellen Position in Konflikt steht, schließt die Strategie zuerst das entgegengesetzte Exposure und öffnet dann die gewünschte Richtung mit dem konfigurierten Volumen.

## Handelsablauf
1. **Schließfenster** – Wenn das Flag *Use Close Window* aktiviert ist und die aktuelle Zeit innerhalb des Schließfensters liegt, beendet die Strategie sofort jede offene Position. Kein neuer Trade ist erlaubt, bis das Fenster endet.
2. **Trailing-Aktualisierung** – Wenn Trailing aktiviert ist und sich der Markt mindestens `TrailingStop + TrailingStep` Pips zugunsten der aktuellen Position bewegt hat, wird der Trailing Stop um die in `TrailingStop` definierte Distanz näher an den Preis gezogen. Dies reproduziert die MT5-Logik, bei der das Stop-Level nur nach einem Mindestschritt geändert wird.
3. **Risikoprüfungen** – Bei jeder abgeschlossenen Kerze prüft die Strategie, ob Stop-Loss- oder Take-Profit-Schwellen berührt wurden. Wenn ein Level getroffen wird, wird die Position geschlossen und der gesamte interne Zustand für diese Seite zurückgesetzt.
4. **Einstiegsfenster** – Wenn die Zeit im Handelsfenster liegt, bewertet die Strategie die Richtungsschalter:
   - Wenn Long-Einstiege aktiviert sind und die aktuelle Netto-Position flat oder Short ist, kauft sie das konfigurierte Volumen plus jede Menge, die zum Abdecken einer bestehenden Short-Position erforderlich ist.
   - Wenn Short-Einstiege aktiviert sind und die Netto-Position flat oder Long ist, verkauft sie das konfigurierte Volumen plus jede Menge, die zum Glätten einer bestehenden Long-Position erforderlich ist.

Jeder ausgeführte Einstieg speichert den Einstiegspreis zusammen mit Stop- und Zielversätzen (falls ungleich null). Diese Werte werden von der Trailing-Logik und den nachfolgenden Ausstiegsprüfungen wiederverwendet.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| Candle Type | 1-Minuten-Kerzen | Datentyp für die Zeitverfolgung; die Strategie reagiert nur auf abgeschlossene Kerzen. |
| Use Close Window | true | Aktiviert das automatische Schließfenster. |
| Close Hour / Close Minute | 20:50 | Beginn des Schließfensters. Stunde unterstützt Werte 0–24; 24 rollt auf den nächsten Tag. |
| Enable Trailing | false | Aktiviert die Trailing-Stop-Logik. |
| Trailing Stop | 30 Pips | Abstand zwischen Preis und Trailing Stop. Wird je nach Tick-Größe des Instruments in Preiseinheiten umgerechnet. |
| Trailing Step | 3 Pips | Zusätzliche Bewegung, die erforderlich ist, bevor der Trailing Stop erneut vorgerückt wird. |
| Trade Hour / Trade Minute | 18:50 | Startzeit des Handelsfensters, das neue Einstiege erlaubt. |
| Duration | 300 Sekunden | Dauer, die von Öffnungs- und Schließfenster gemeinsam genutzt wird. |
| Enable Sell / Enable Buy | Sell = true, Buy = false | Wählt aus, welche Richtungen erlaubt sind. |
| Volume | 0.1 | Ordervolumen, das mit neuen Einstiegen eingereicht wird. Beim Umkehren wird zusätzliches Volumen hinzugefügt, um das entgegengesetzte Exposure zu glätten. |
| Stop Loss | 0 Pips | Anfängliche Stop-Loss-Distanz. Ein Wert von null deaktiviert den statischen Stop und überlässt die Ausstiegskontrolle dem Trailing oder dem Schließfenster. |
| Take Profit | 0 Pips | Anfängliche Take-Profit-Distanz. Ein Wert von null deaktiviert das Gewinnziel. |

## Implementierungsdetails
- Pip-Werte werden aus `Security.PriceStep` neu berechnet. Bei Symbolen mit drei oder fünf Dezimalstellen wird der Schritt mit zehn multipliziert, um die ursprüngliche MT5-"Pip"-Konvertierung zu reproduzieren.
- Sowohl Trailing als auch statische Risikolevel operieren auf Kerzenextremen (`HighPrice`/`LowPrice`), um das Tick-für-Tick-Verhalten anzunähern, während in der kerzenzbasierten High-Level-API gearbeitet wird.
- Die Strategie setzt den internen Zustand nach jedem Ausstieg zurück, um zu vermeiden, veraltete Stops oder Ziele im nächsten Trade wiederzuverwenden.
- Da StockSharp standardmäßig mit Netto-Positionen arbeitet, werden gleichzeitige Long- und Short-Positionen nicht unterstützt. Die Umkehrlogik imitiert MT5-Hedging, indem das bestehende Exposure ausgeglichen wird, bevor die gewünschte Seite geöffnet wird.

## Verwendungshinweise
- Wähle einen Kerzentyp, der der vom Handelsfenster geforderten Zeitgranularität entspricht. Ein kürzerer Zeitrahmen (z. B. 1 Minute) bietet präziseres Timing.
- Schließ- und Öffnungsfenster teilen denselben Dauerparameter. Um eines der Fenster zu deaktivieren, setze die Dauer auf null oder schalte *Use Close Window* aus.
- Trailing Stops aktivieren sich nur, wenn der Markt mindestens `Trailing Stop + Trailing Step` Pips vom aufgezeichneten Einstiegspreis vorgerückt ist, was das ursprüngliche Trailing-Step-Verhalten reproduziert.
