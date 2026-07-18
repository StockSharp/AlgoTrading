# OHLC-Prüfungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die OHLC-Prüfungsstrategie repliziert den klassischen MetaTrader-Expert-Advisor, der die Eröffnungs-, Hoch-, Tief- und Schlusskursstruktur der vorherigen Kerze untersucht. Die Strategie bewertet den Kerzenkörper bei einem konfigurierbaren historischen Versatz und eröffnet eine neue Position in der Richtung dieses Körpers, wobei das Signal optional gespiegelt werden kann. Sie ist für eine einfache preisaktionsbasierte Ausführung ohne Abhängigkeit von Oszillatoren oder gleitenden Durchschnitten ausgelegt.

## Handelslogik
1. Die Strategie abonniert die konfigurierte Kerzenserie und wartet, bis die Kerze abgeschlossen ist, bevor sie verarbeitet wird.
2. Für jede abgeschlossene Kerze speichert die Engine den Eröffnungs- und Schlusskurs, sodass der benutzerdefinierte Versatz ("SignalShift") auf ältere Kerzen verweisen kann.
3. Ein bullischer Körper (Schluss über Eröffnung) erzeugt ein Long-Signal, während ein bärischer Körper (Schluss unter Eröffnung) ein Short-Signal erzeugt. Bei einem flachen Körper wird kein Trade erstellt.
4. Das `ReverseSignals`-Flag kann die Handelsrichtung umkehren und reproduziert den Reverse-Trading-Modus des ursprünglichen Expert-Advisors.
5. Wenn keine aktive Position vorhanden ist, versucht die Strategie, eine Marktorder in der erkannten Richtung zu eröffnen, solange der aktuelle Spread innerhalb des erlaubten `SpreadLimitPips`-Schwellenwerts liegt. Der Spread wird über den Orderbuch-Feed überwacht.
6. Wenn bereits eine Position vorhanden ist, löst das entgegengesetzte Signal ein Schließen der Position aus, anstatt einer vollständigen Umkehrung, entsprechend der MQL-Logik.
7. Optionale Stop-Loss- und Take-Profit-Schutzmaßnahmen werden beim Start unter Verwendung von Pip-Distanzen gestartet, die in den Preis-Schritt des Instruments umgerechnet werden, und recreieren das MQL-Money-Management-Verhalten.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 5-Minuten-Zeitrahmen | Für die OHLC-Auswertung verwendete Datenserie. |
| `StopLossPips` | 50 | Stop-Loss-Abstand gemessen in Pips; `0` deaktiviert den Stop. |
| `TakeProfitPips` | 100 | Take-Profit-Abstand gemessen in Pips; `0` deaktiviert das Ziel. |
| `ReverseSignals` | `false` | Kehrt die Richtung von Long- und Short-Signalen um. |
| `SpreadLimitPips` | 1 | Maximaler Spread in Pips, der beim Eröffnen einer neuen Position erlaubt ist. |
| `SignalShift` | 1 | Anzahl der abgeschlossenen Kerzen zurück für die Signalberechnung (1 = vorherige Kerze). |
| `OrderVolume` | 1 | Mit jeder Marktorder gesendetes Volumen. |

## Verwendungshinweise
- Die Strategie verwendet die Instrumentenmetadaten, um Pip-Werte in Preisschrittabstände umzurechnen. Instrumente mit 3 oder 5 Dezimalstellen erhalten automatisch die standardmäßige Zehn-Punkt-Pip-Anpassung.
- Das Orderbuch-Abonnement sollte im Datenfeed aktiviert sein, damit Spread-Prüfungen korrekt funktionieren. Wenn keine Bid/Ask-Kurse verfügbar sind, überspringt die Strategie das Eröffnen neuer Trades.
- Schutz-Stops werden einmalig während `OnStarted` initiiert. Das nachträgliche Ändern von Stop-Parametern erfordert einen Neustart der Strategie, um neue Schutzmaßnahmen anzuwenden.
- Da die Strategie nur auf den Kerzenkörper reagiert, werden Hoch- und Tiefwerte genau wie in der ursprünglichen MetaTrader-Version ignoriert.

## Bereitstellungsschritte
1. Fügen Sie die Strategie einem Instrument hinzu, das sowohl Kerzen als auch Orderbuch-Kurse liefert.
2. Konfigurieren Sie die Parameter entsprechend dem gewünschten Handelsstil (Zeitrahmen, Pip-Abstände und Volumen).
3. Starten Sie die Strategie. Sie wartet auf die nächste abgeschlossene Kerze, bevor sie eine Aktion durchführt.
4. Überwachen Sie das Protokoll auf Spread-Ablehnungen oder ausgeführte Trades und passen Sie die Parameter nach Bedarf an.
