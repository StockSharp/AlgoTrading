# MTrendLine-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MTrendLine-Strategie** portiert das MetaTrader-Skript `MTrendLine.mq4` zur übergeordneten Strategie API von StockSharp. Das Original
Der Expertenberater passt den Preis bestehender ausstehender Aufträge wiederholt an, sodass diese an einer auf dem Markt eingezeichneten Trendlinie ausgerichtet bleiben
Diagramm. Die StockSharp-Version automatisiert das gleiche Verhalten, indem sie die sich bewegende Trendlinie mit einem konfigurierbaren Element neu erstellt
`LinearRegression`-Anzeige. Bis zu drei unabhängige Pending-Order-Slots können der berechneten Regressionslinie folgen
eigene Orderart, Distanz und Volumen. Jedes Mal, wenn eine neue Kerze schließt, berechnet die Strategie den Linienwert neu und wertet die aus
Erforderliche Offsets und aktualisiert die ausstehenden Bestellungen entsprechend.

Der Port bietet moderne Risiko- und Benutzerfreundlichkeitsverbesserungen wie strukturierte Parameter und automatische Konvertierung von MetaTrader Punkten
in reale Preisschritte und optionale Stop-Loss-/Take-Profit-Abstände, die sich zusammen mit den ausstehenden Aufträgen bewegen. Bieten/fragen
Aktualisierungen werden über `SubscribeLevel1()` überwacht, sodass die Strategie den Mindestabstand berücksichtigt, den Broker zwischen den verlangen
aktueller Marktpreis und Restaufträge.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie über `SubscribeCandles()` und füttern Sie jeweils einen `LinearRegression`-Indikator
fertige Bar. Der Indikator stellt die manuelle Trendlinie aus der MetaTrader-Version dar.
2. Pflegen Sie Abonnements der Stufe 1, um die neuesten besten Geld- und Briefwerte zwischenzuspeichern. Sie dienen der Durchsetzung des Mindestmaßes
Abstandsparameter vor dem Verschieben einer ausstehenden Bestellung.
3. Berechnen Sie für jeden aktivierten Slot den gewünschten Preis als **Regressionswert + Distanz × Punktgröße**. Die Punktgröße ist standardmäßig auf
der Wertpapierpreisschritt, kann aber überschrieben werden, um der `Point`-Konstante von MetaTrader zu entsprechen.
4. Konvertieren Sie die Slot-Konfiguration in StockSharp Bestellhelfer (`BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`). Optional
Stop-Loss- und Take-Profit-Preise werden aus der angeforderten Distanz in Punkten abgeleitet, sodass sie die Order nach jeder Bewegung verfolgen.
5. Wenn für den Slot bereits eine aktive Pending Order existiert und der neue Zielpreis abweicht, stornieren Sie zunächst die aktuelle Order und
Warten Sie auf die nächste Kerze, um die aktualisierte Kerze zu platzieren. Dies spiegelt das Verhalten von `OrderModify` aus dem MQL-Code ohne wider
Es besteht die Gefahr doppelter Anfragen.
6. Wenn ein Slot deaktiviert ist oder der berechnete Preis ungültig (z. B. negativ) wird, stornieren Sie die zugehörige ausstehende Bestellung
und den zwischengespeicherten Zustand löschen.

## Ausstehende Bestellplätze
Jeder Slot emuliert einen Aufruf von `modify()` im ursprünglichen EA. Slots können unabhängig voneinander konfiguriert werden:
- **Typ** – Wählen Sie zwischen Kauflimit, Kaufstopp, Verkaufslimit oder Verkaufsstopp.
- **Entfernung** – Entfernung in MetaTrader Punkten, addiert zum Regressionswert, um den neuen Preis zu erhalten. Verwenden Sie negative Werte, um
Positionsaufträge unterhalb der Regressionslinie.
- **Volumen** – Größe der ausstehenden Bestellung. Bei Null oder negativ greift die Strategie auf den globalen `TradeVolume` zurück.
- **Aktivierungsflag** – ermöglicht das Deaktivieren eines Steckplatzes, ohne seine Konfiguration zu entfernen. Deaktivierte Slots löschen automatisch alle aktiven
Aufträge, die ihnen gehören.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-Stunden-Kerzen | Primärer Zeitrahmen, der zum Erstellen der Regressionstrendlinie verwendet wird. |
| `RegressionLength` | `int` | `24` | Anzahl der abgeschlossenen Kerzen, die in den Indikator `LinearRegression` eingespeist werden. |
| `PointValue` | `decimal` | `0` | Geldwert eines MetaTrader Punktes. Bei Null verwendet die Strategie den Wertpapierpreisschritt. |
| `TradeVolume` | `decimal` | `1` | Von allen Slots verwendetes Standardvolumen, wenn das eigene Volumen Null ist. |
| `StopLossPoints` | `decimal` | `0` | Stop-Loss-Distanz in Punkten. Auf Null setzen, um die automatische Stop-Loss-Platzierung zu deaktivieren. |
| `TakeProfitPoints` | `decimal` | `0` | Take-Profit-Distanz in Punkten. Auf Null setzen, um die automatische Take-Profit-Platzierung zu deaktivieren. |
| `MinDistancePoints` | `decimal` | `0` | Mindestlücke (in Punkten), die zwischen dem besten Geld-/Briefkurs und der ausstehenden Order bestehen muss. |
| `PendingOrder{1,2,3}Enabled` | `bool` | Slot-spezifisch | Aktiviert oder deaktiviert den angegebenen Steckplatz. |
| `PendingOrder{1,2,3}Mode` | `enum` | Slot-spezifisch | Ausstehender Ordertyp: BuyLimit, BuyStop, SellLimit oder SellStop. |
| `PendingOrder{1,2,3}DistancePoints` | `decimal` | Slot-spezifisch | Distanz (in Punkten), addiert zum Regressionswert, um den Bestellpreis zu berechnen. |
| `PendingOrder{1,2,3}Volume` | `decimal` | Slot-spezifisch | Lautstärke für den Steckplatz. Keine Wiederverwendung von `TradeVolume`. |

## Unterschiede zum ursprünglichen MetaTrader-Skript
- MetaTrader ändert vorhandene Bestellungen. StockSharp verwendet die Semantik „Abbrechen und Ersetzen“, während auf die Bestätigung gewartet wird
bevor Sie die Ersatzbestellung für die nächste Kerze registrieren.
- Der Originalcode liest den Wert einer manuell gezeichneten Trendlinie. Der Port ersetzt dies durch eine Automatik
`LinearRegression`-Indikator, damit das Verhalten deterministisch ist und unbeaufsichtigt ausgeführt werden kann.
- `MODE_STOPLEVEL` ist auf StockSharp nicht verfügbar. Stattdessen stellt die Strategie das konfigurierbare `MinDistancePoints` bereit.
Parameter und erzwingt ihn mithilfe von Gebots-/Briefaktualisierungen in Echtzeit.
- Stop-Loss- und Take-Profit-Distanzen sind optionale Parameter, anstatt bestehende Ordereinstellungen zu lesen. Dadurch bleiben die Werte erhalten
konsistent über alle Neuregistrierungen von Bestellungen hinweg.

## Anwendungstipps
- Stellen Sie `PointValue` so ein, dass es mit der Punktdefinition des Brokers übereinstimmt, wenn diese von der Sicherheit `PriceStep` abweicht. Dies garantiert die
Distanzparameter spiegeln ihre MetaTrader-Gegenstücke wider.
- Aktivieren Sie nur die Slots, die Sie benötigen. Jeder Slot verfügt über eine eigene ausstehende Reihenfolge und einen eigenen Kommentar (`"MTrendLine slot N"`) zur Identifizierung
sie in Berichten oder im Bestellprotokoll zu erfassen, ist unkompliziert.
- Erwägen Sie die Kombination der Strategie mit den integrierten Risikoschutz-Helfern von StockSharp, wenn Sie Trailing Stops oder ein Konto benötigen
Pegelkontrollen. Der Schwerpunkt der Implementierung liegt auf der Spiegelung der ursprünglichen Auftragsänderungslogik.

## Indikatoren
- `LinearRegression` wird auf fertige Kerzen angewendet.
