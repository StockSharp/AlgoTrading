# PLC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die PLC-Strategie repliziert das Verhalten des MetaTrader Expert Advisors `PLC (barabashkakvn's edition)` mithilfe der StockSharp High-Level-API. Der Algorithmus arbeitet auf dem durch den Parameter `Entry Timeframe` angegebenen hohen Zeitrahmen und platziert Breakout-Stop-Orders ober- und unterhalb der zuletzt abgeschlossenen Kerze. Fraktale niedrigerer Zeitrahmen (M5 und H1 standardmäßig) werden verwendet, um das Ordervolumen dynamisch zu skalieren. Sobald der schwebende Gewinn aller offenen Positionen den konfigurierten Schwellenwert übersteigt, liquidiert die Strategie die gesamte Position und wartet auf das nächste Setup.

## Handelslogik

1. **Neue Kerzenverarbeitung** – die Strategie reagiert nur, wenn eine Kerze auf dem Hauptzeitrahmen vollständig geschlossen ist. Alle Berechnungen werden mit den Schlusskerzendaten durchgeführt, um Neuzeichnen zu vermeiden.
2. **Order-/Positionswartung** – vor der Auswertung eines neuen Setups storniert der Algorithmus ausstehende Stop-Orders, die zum Löschen vorgemerkt sind, und schließt Positionen, wenn das Gewinnziel auf einer vorherigen Kerze erreicht wurde.
3. **Preisoffsets** – das Hoch und Tief der zuletzt fertigen Kerze werden um die über `Shift OHLC` konfigurierte Anzahl von Pips verschoben. Die Pip-Größe wird automatisch für 3- oder 5-stellige Forex-Symbole angepasst.
4. **Fraktal-Aktualisierungen** – dedizierte Abonnements verfolgen Fraktal-Muster auf den M5- und H1-Zeitrahmen. Die aktuellsten aufwärts- und abwärts-Fraktalwerte werden gespeichert, wenn ein klassisches Fünf-Balken-Muster abgeschlossen wird.
5. **Abstandsprüfung** – ein neuer Kauf-Stop wird nur platziert, wenn das verschobene Hoch mindestens `Shift Position` Pips über dem höchsten Einstiegspreis offener Long-Trades liegt, oder wenn keine Long-Trades und keine aktiven Kauf-Stops vorhanden sind. Dieselbe Regel mit umgekehrten Vergleichen gilt für Verkauf-Stops.
6. **Dynamische Lot-Größenbestimmung** – das Basisvolumen (`Buy Volume` oder `Sell Volume`) wird mit dem M5- oder H1-Multiplikator multipliziert, wenn das Stop-Niveau über das entsprechende Fraktal bricht. Das Setzen eines Multiplikators auf null deaktiviert das Skalieren für diesen Zeitrahmen.
7. **Order-Registrierung** – Stop-Orders werden über `BuyStop`/`SellStop` gesendet. Referenzen auf die registrierten Orders werden verfolgt, um spätere Stornierungen zu vereinfachen.
8. **Gewinnüberwachung** – nach der Summierung des offenen Gewinns aller Long- und Short-Lots (unter Verwendung des Schrittwerts des Instruments) schaltet die Strategie den `Positionen schließen`-Modus um, sobald der Gewinn den `Minimum Profit` übersteigt. Marktorders werden auf der nächsten Kerze verwendet, um die Exposition aufzulösen.
9. **Trade-Feedback** – wenn eine ausstehende Stop-Order ausgeführt wird, werden alle anderen ausstehenden Stops storniert, um die ursprüngliche MQL-Logik zu imitieren.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `Shift OHLC` | Anzahl der Pips, die oberhalb des letzten Kerzenhochs und unterhalb des letzten Kerzenliefs hinzugefügt werden, um die Stop-Aktivierungsniveaus zu bestimmen. |
| `Minimum Profit` | Gewinn (in der Instrumentwährung), der das Schließen aller offenen Positionen auslöst. |
| `Shift Position` | Mindestabstand in Pips zwischen dem neuen Stop-Niveau und dem extremen Eröffnungspreis bestehender Positionen. Verhindert das Stapeln von Orders zu nah an vorherigen Einstiegen. |
| `Buy Volume` / `Sell Volume` | Basis-Ordergröße (Lots). Wird vor der Anwendung von Fraktal-Multiplikatoren verwendet. |
| `M5 Multiplier` / `H1 Multiplier` | Volumen-Multiplikatoren, die aktiviert werden, wenn der Stop-Preis über (für Longs) oder unter (für Shorts) dem neuesten Fraktal im jeweiligen Zeitrahmen liegt. `0` verwenden, um das Skalieren zu deaktivieren. |
| `Entry Timeframe` | Haupt-Zeitrahmen zur Generierung von Einstiegen. Jede fertige Kerze auf diesem Zeitrahmen löst eine neue Auswertung aus. |
| `M5 Fractal Timeframe` | Zeitrahmen, der den unteren Fraktal-Detektor speist (Standard 5 Minuten). |
| `H1 Fractal Timeframe` | Zeitrahmen, der den höheren Fraktal-Detektor speist (Standard 1 Stunde). |

## Positionsverwaltung

- **Stornierung** – Die Strategie hält Referenzen zu allen ausstehenden Stop-Orders. Wenn eine Stop-Order gefüllt wird, werden alle verbleibenden ausstehenden Orders beim nächsten Bewertungszyklus storniert.
- **Auflösung** – Wenn `Minimum Profit` überschritten wird, wird die Nettoposition mit Marktorders aufgelöst (`SellMarket` für Longs, `BuyMarket` für Shorts). Die Markierung wird gelöscht, sobald die Positionsgröße auf null zurückgeht.
- **Bestandsverfolgung** – Gefüllte Orders werden als einzelne Lots aufgezeichnet, um das MetaTrader-Verhalten zu replizieren, das zwischen dem höchsten Kauf- und niedrigsten Verkaufseinstiegspreis unterscheidet.

## Hinweise

- Die Standardparameter spiegeln die ursprüngliche Expert Advisor-Konfiguration wider. Sie können die Fraktal-Zeitrahmen wechseln, indem Sie die Parameter `M5 Fractal Timeframe` und `H1 Fractal Timeframe` bearbeiten, wenn das Instrument andere Kontextfenster benötigt.
- Volumen werden auf den Exchange-Volumenschritt abgerundet, bevor Orders gesendet werden. Wenn der resultierende Wert null ist, wird die Order übersprungen.
- Die Gewinnberechnung verwendet den Preis- und Schrittwert des Instruments, um die Kompatibilität mit Instrumenten zu gewährleisten, die einen Nicht-Einheit-Tick-Wert haben.
