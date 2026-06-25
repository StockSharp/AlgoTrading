# II Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die II Ausbruch-Strategie ist ein Hochfrequenz-Ausbruchssystem, das ursprünglich für MetaTrader 4 geschrieben wurde. Es kombiniert einen proprietären Timing-Oszillator mit einer Volatilitätsdruck-Anzeige, um starke Richtungsbewegungen zu erfassen, und verwaltet dann Trades mit adaptiven Trailing Stops und Pyramidisierung. Diese Konvertierung reproduziert die ursprüngliche Logik über die StockSharp High-Level-API und behält dieselben Schutzmaßnahmen für Spread-, Volatilitäts- und Kalenderfilter bei.

## Konvertierte Handelslogik
### Timing-Oszillator
* Jede neue M1-Kerze trägt einen „typischen Preis" (Durchschnitt aus High, Low und Close multipliziert mit 100) bei, der die ursprüngliche Glättungskaskade speist.
* Die Kaskade rekonstruiert die ursprüngliche verschachtelte gleitende Durchschnitt-/Differenz-Pipeline (dtemp/atemp-Puffer), um einen Timing-Wert von 0 bis 100 zu erzeugen.
* Kaufsignal: Timing-Wert kreuzt aufwärts über seiner vorherigen Lesart (buffer[0] > buffer[1] mit buffer[1] ≤ buffer[2]).
* Verkaufssignal: Timing-Wert kreuzt abwärts (buffer[0] < buffer[1] mit buffer[1] ≥ buffer[2]).

### Volatilitätsfilter
* Eine 10-Perioden-Standardabweichung auf Schlusskursen muss unter `StdDevLimit` bleiben. Wenn das Limit überschritten wird, sind keine neuen Positionen erlaubt und optional wird eine Warnung protokolliert.
* Eine benutzerdefinierte Volatilitätsbewertung repliziert die ursprüngliche Amplitude × Tick-Dichte-Formel: Sie nutzt die Überlappung zwischen der aktuellen und der vorherigen Minutenkerze sowie die durchschnittliche Anzahl von Ticks pro Sekunde. Die Bewertung muss den konfigurierbaren `VolatilityThreshold` überschreiten.

### Einstiegsregeln
* Die Strategie arbeitet auf einem einzelnen Symbol-/Zeitrahmenpaar, das über den `CandleType`-Parameter bereitgestellt wird (standardmäßig 1-Minuten-Kerzen).
* Wenn keine Position offen ist und der Kalenderfilter den Handel erlaubt, aktualisiert die Engine die Losgröße über `CalculateOrderVolume()` und überprüft den aktuellen Spread gegen `SpreadThreshold` (mit Level-1-Bid/Ask-Daten).
* Eine Long-Position wird eröffnet, wenn der Timing-Oszillator ein Kaufsignal ausgibt und die Volatilitätsbewertung gültig ist. Eine Short-Position folgt der gespiegelten Bedingung. Beim Einstieg wird ein statischer Stop um das Doppelte von `TrailStopPoints` unterhalb/oberhalb des Ausführungspreises platziert.

### Pyramidisierung und Trailing
* Das Trailing-Modul aktiviert sich, sobald die aggregierte Position mindestens `TrailStopPoints + int(Commission) + SpreadThreshold` Punkte unrealisierten Gewinn erwirtschaftet.
* Der Stop wird auf `TrailStopPoints` hinter dem letzten Schlusskurs gestrafft (getrennt für Longs und Shorts verfolgt). Jede Verbesserung größer als ein Punkt aktualisiert den Trailing-Preis.
* Solange Volatilitäts-, Timing- und Spread-Bedingungen gültig bleiben, kann die Strategie alle `max(10, SpreadThreshold + 1)` Punkte zusätzlichen Gewinns neue Orders pyramidisieren. Neue Orders deaktivieren den statischen Stop und verlassen sich ausschließlich auf die Trailing-Logik.

### Risiko- und Kapitalmanagement
* Die Positionsgröße wird vor jeder Order neu berechnet: `Guthaben × MaximumRisk ÷ (500000 / AccountLeverage)`, gerundet auf den Volumen-Schritt des Wertpapiers. Wenn Guthabeninformationen nicht verfügbar sind, wird auf `Volume` oder Mindestlos zurückgegriffen.
* Eine vereinfachte Marginkontrolle approximiert die ursprüngliche MetaTrader-Schutzfunktion (`volume × price / leverage × (1 + MaximumRisk × 190)`). Orders werden ignoriert, wenn der Kontowert diesen Betrag nicht decken kann.
* Nachdem die Pyramidisierung aktiviert wurde, überwacht die Strategie den Floating-Verlust. Wenn der nicht realisierte Drawdown `TotalEquityRisk` Prozent des Kontowerts übersteigt, werden alle Positionen liquidiert.

### Kalender- und Spread-Schutzmaßnahmen
* Der Handel stoppt freitags nach 23:00 Uhr Serverzeit und während der letzten Handelstage des Jahres (Jahrestag 358, 359, 365 oder 366) nach 16:00 Uhr.
* Jeder Einstieg und jede Aufstockung überprüft den aktuellen Bid/Ask-Spread und überspringt die Ausführung, wenn der konfigurierte Schwellenwert überschritten wird.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `Commission` | 4 | Rundlot-Provision in Punkten, die bei der Berechnung des Trailing-Aktivierungsoffsets verwendet wird. |
| `SpreadThreshold` | 6 | Maximaler Spread (in Punkten), der für neue Einstiege oder Pyramidisierung erlaubt ist. |
| `TrailStopPoints` | 20 | Trailing-Stop-Abstand in Punkten; der anfängliche Stop ist doppelt so groß. |
| `TotalEquityRisk` | 0.5 | Prozentualer Kontowert-Verlust, der nach der Pyramidisierung einen Zwangsausstieg auslöst. |
| `MaximumRisk` | 0.1 | Anteil des Kontoguthabens, der bei jeder Order für die Volumensanpassung eingesetzt wird. |
| `StdDevLimit` | 0.002 | Maximale 10-Perioden-Standardabweichung, um neue Trades zu akzeptieren. |
| `VolatilityThreshold` | 800 | Minimale Volatilitätsbewertung (Amplitude × Tick-Dichte), die für den Handel erforderlich ist. |
| `AccountLeverage` | 100 | Kontohebel für Margiannäherung und Positionsdimensionierung. |
| `WarningAlerts` | true | Aktiviert die Protokollierung, wenn der Standardabweichungsfilter Einstiege blockiert. |
| `CandleType` | 1 Minute | Kerzentyp, der für alle Berechnungen verwendet wird. |

## Indikatoren
* `StandardDeviation(Length = 10)` auf Schlusskursen für den Volatilitätsfilter.
* Benutzerdefinierter Timing-Oszillator, reproduziert vom ursprünglichen EA (inline implementiert ohne StockSharp-Indikatorobjekte).

## Implementierungshinweise
* Die Spread-Filterung erfordert Live-Level-1-Daten (`Security.BestBid`/`BestAsk`). Wenn der Feed fehlt, geht die Strategie von einem Spread von null aus.
* Margin- und Eigenkapitalprüfungen sind Näherungen, da der ursprüngliche EA auf MetaTrader-spezifische Kontoeigenschaften und Kontraktgrößen angewiesen war. Passe `AccountLeverage`, `MaximumRisk` oder `Volume` an das Broker-Modell an.
* Die Konvertierung verwendet die StockSharp High-Level-API (Kerzenabonnements mit `Bind`) und hält alle Kommentare auf Englisch. Für diese Strategie wird kein Python-Port erstellt.
