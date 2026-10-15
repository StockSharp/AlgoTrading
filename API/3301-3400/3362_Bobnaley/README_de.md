# Bobnaley-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Bobnaley-Strategie reproduziert den MetaTrader 5 Expertenberater „bobnaley“ unter Verwendung des StockSharp hohen Levels API. Es kombiniert einen einfachen Trendfilter für gleitende Durchschnitte mit dem stochastischen Oszillator, um nach Umkehrmöglichkeiten zu suchen. Der ursprüngliche Gutachter bewertete die Tick-Preise; Der Port verwendet fertige Kerzen und behält die Regeln für die Auftragsverwaltung bei.

## Wie es funktioniert
1. **Indikatoren**
   - Ein einfacher gleitender Durchschnitt mit der konfigurierten Periode filtert die vorherrschende Richtung.
   - Ein stochastischer Oszillator (Haupt- und Signallinien) identifiziert überverkaufte und überkaufte Situationen. Für Signale wird nur die Hauptleitung benötigt; Die Signalleitung wird der Vollständigkeit halber berechnet.
2. **Eintrittsbedingungen**
   - Die Strategie wartet, bis die aktuelle Kerze fertig ist und alle Indikatoren gebildet sind.
   - Bei Long-Einstiegen muss der gleitende Durchschnitt während der letzten drei Stichproben streng fallen, während der Preis über dem letzten Durchschnitt schließt. Gleichzeitig muss die stochastische Hauptlinie unter dem überverkauften Niveau liegen und ihr vorheriger Wert muss höher sein als der davor, was die ursprüngliche EA-Anforderung `stochVal[1] > stochVal[2]` widerspiegelt.
   - Short-Einstiege sind das Spiegelbild: Der gleitende Durchschnitt muss in den letzten drei Stichproben steigen, während der Preis darunter schließt, und die stochastische Hauptlinie muss über dem überkauften Niveau liegen, während ihr vorheriger Wert niedriger als der frühere ist.
   - Neue Trades werden nur eröffnet, wenn derzeit keine Position aktiv ist, wodurch der `PositionSelect`-Schutz von MetaTrader repliziert wird.
3. **Risikomanagement**
   - Wenn eine Position eröffnet wird, verlässt sich die Strategie auf den Schutzdienst von StockSharp, um einen Take-Profit und einen Stop-Loss in absoluten Preiseinheiten zu platzieren. Diese Abstände stimmen mit den MetaTrader-Eingaben überein (standardmäßig 0,007 und 0,0035).
   - Vor jeder Entscheidung wird der Portfoliowert mit dem Parameter `Minimum Balance` verglichen, der den Free-Margin-Filter (`ACCOUNT_FREEMARGIN > 5000`) des Originalcodes widerspiegelt. Wenn der Kontowert bekannt ist und unter dem Schwellenwert liegt, wird die Eingabe übersprungen.
4. **Volumenhandhabung**
   - Bestellungen verwenden einen festen `Base Volume`-Parameter. Dies reproduziert die Chargeneinstellung, die das MetaTrader-Skript nach Anwendung seiner eigenen Rundungsroutine verwendet hat.

## Parameter
| Kategorie | Name | Beschreibung | Standard |
| --- | --- | --- | --- |
| Allgemein | Kerzentyp | Kerzendatentyp, der für Indikatorberechnungen verwendet wird. | Zeitrahmen von 5 Minuten |
| Handel | Grundvolumen | Auf jede neue Position wird ein festes Auftragsvolumen angewendet. | 5 |
| Indikatoren | MA-Zeitraum | Länge des einfachen gleitenden Durchschnitts. | 76 |
| Indikatoren | Stochastic Zeitraum | Rückblick auf die stochastische Hauptlinie. | 5 |
| Indikatoren | Stochastic %K | Glättungslänge für die %K-Linie. | 3 |
| Indikatoren | Stochastic %D | Glättungslänge für die %D-Linie. | 3 |
| Indikatoren | Stochastic Überverkauft | Schwellenwert, der den überverkauften Bereich für die Hauptlinie definiert. | 30 |
| Indikatoren | Stochastic Überkauft | Schwellenwert, der den überkauften Bereich für die Hauptlinie definiert. | 70 |
| Risikomanagement | Nehmen Sie Gewinn mit | Abstand zwischen Einstiegspreis und Take-Profit in Preiseinheiten. | 0,007 |
| Risikomanagement | Stop-Loss | Abstand zwischen Einstiegspreis und Stop-Loss in Preiseinheiten. | 0,0035 |
| Risikomanagement | Mindestguthaben | Mindestportfoliowert erforderlich, bevor eine neue Bestellung gesendet werden kann. | 5000 |

## Notizen
- Der ursprüngliche Experte verwendete Bid/Ask-Kurse; In StockSharp wird der Kerzenschluss als Proxy für den Ausführungspreis verwendet.
- Es werden keine Trailing-Exits implementiert – der Handel wird nur durch die Schutzaufträge geschlossen.
- Stochastic-Berechnungen folgen den Standardeinstellungen von MetaTrader (5/3/3), können aber über die bereitgestellten Parameter optimiert werden.
