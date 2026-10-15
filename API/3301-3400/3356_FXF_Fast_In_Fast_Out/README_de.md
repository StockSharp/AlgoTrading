# FXF Fast-in-Fast-out-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **FXF Fast in Fast out**-Strategie ist ein volatilitätsgesteuertes Breakout-System, das den ursprünglichen MetaTrader 4 Expert Advisor in eine StockSharp High-Level-Strategie umwandelt. Es überwacht einen konfigurierbaren Zeitrahmen für große Kerzen, misst den Spread und reagiert, indem es ausstehende Stop-Orders platziert, die versuchen, eine sofortige Fortsetzung der Dynamik zu erreichen. Die Logik verwendet nur fertige Kerzen zur Signalgenerierung, während Kurse (Level1-Daten) für Spread-Filter, Auftragserteilung und Trailing-Stop-Management verwendet werden.

Wenn sich die aktuelle Kerze über einen Volatilitätsschwellenwert hinaus ausdehnt, bewertet die Strategie den mittleren Preis im Verhältnis zur Öffnung der Kerze. Wenn der Mittelkurs über dem Eröffnungskurs schließt, wird ein Kaufstopp über dem besten Briefkurs platziert; schließt er darunter, wird ein Verkaufsstopp unter dem besten Gebot platziert. Ausstehende Aufträge werden mit schützenden Stop-Loss- und Take-Profit-Levels versehen, und eine optionale Trailing-Logik schützt offene Positionen, sobald sie gefüllt sind. Das Money Management kann die Ordergröße basierend auf dem Portfoliowert und der Stop-Distanz dynamisch anpassen.

## Handelslogik
- **Signalerkennung** – Bei jeder fertigen Kerze prüft die Strategie, ob die in Preisschritten ausgedrückte Kerzenspanne `VolatilitySizePoints` überschreitet. Wenn die Spanne groß genug ist, wird der mittlere Preis anhand des letzten besten Bid/Ask-Snapshots berechnet.
- **Richtungstendenz** – Ein mittlerer Preis über der Eröffnung der Kerze erzeugt eine bullische Tendenz (Kauf-Stopp-Order), während ein mittlerer Preis unter der Eröffnung eine bärische Tendenz erzeugt (Verkaufs-Stopp-Order). Es erfolgt keine Ordererteilung, wenn der Mittelpreis dem Eröffnungspreis entspricht oder die Volatilitätsanforderung nicht erfüllt ist.
- **Spread-Filter** – Kurse werden kontinuierlich überwacht. Ausstehende Aufträge werden nur erstellt, wenn der aktuelle Spread unter `MaxSpreadPoints` liegt. Wenn sich der Spread über diese Grenze hinaus ausdehnt, werden alle bestehenden ausstehenden Aufträge storniert, bis der Spread wieder ein akzeptables Niveau erreicht.
- **Pending-Order-Verwaltung** – Pro Bar kann nur ein Pending-Order aktiv sein. Jede Bestellung wird um `EnterOffsetPoints` vom besten Angebot abgezogen. Stop-Loss- und Take-Profit-Distanzen werden in Punkten definiert und automatisch in Preise umgerechnet.
- **Risikokontrolle** – Wenn `UseMoneyManagement` aktiviert ist, wird das Ordervolumen anhand des Portfoliowerts, des Risikoprozentsatzes und der Stop-Loss-Distanz unter Verwendung des Instrumentenschrittpreises dimensioniert. Andernfalls wird die Standardeigenschaft `Volume` verwendet.
- **Trailing Stop** – Wenn `EnableTrailing` wahr ist, behält die Strategie einen internen Trailing Stop für die aktive Position bei, basierend auf `TrailingStopPoints` plus dem aktuellen Spread. Wenn der Marktpreis den Trailing Stop überschreitet, wird die Position zum Marktwert geschlossen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `EnterOffsetPoints` | Abstand in Preisschritten zwischen dem besten Quote und dem ausstehenden Stop-Order-Preis. |
| `MaxSpreadPoints` | Maximal zulässiger Spread (in Preisschritten). Der Spread oberhalb dieses Limits blockiert neue Eingaben und storniert aktive ausstehende Aufträge. |
| `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten, die auf ausstehende Aufträge angewendet werden. Auf Null setzen, um die Take-Profit-Platzierung zu überspringen. |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. Erforderlich für die Größenbestimmung des Geldmanagements. Auf Null setzen, um die Stop-Loss-Platzierung zu deaktivieren. |
| `VolatilitySizePoints` | Mindestkerzenspanne (in Preisschritten), die erforderlich ist, um ein neues Ausbruchssignal zu generieren. |
| `EnableTrailing` | Aktiviert oder deaktiviert die Trailing-Stop-Logik für offene Positionen. |
| `TrailingStopPoints` | Basis-Trailing-Distanz in Preisschritten. Das tatsächliche Trailing-Level umfasst auch die aktuelle Ausbreitung, um das ursprüngliche EA-Verhalten nachzuahmen. |
| `UseMoneyManagement` | Ermöglicht die portfoliobasierte Positionsgrößenbestimmung mithilfe des `RiskPercent`-Werts. |
| `RiskPercent` | Risikoprozentsatz pro Trade, der bei aktivem Money Management verwendet wird. |
| `MaxOrdersPerBar` | Maximal zulässige Anzahl ausstehender Aufträge während eines einzelnen Balkens. Normalerweise auf 1 gesetzt, um den ursprünglichen Expert Advisor widerzuspiegeln. |
| `CandleType` | Der Zeitrahmen der für Signalberechnungen verwendeten Kerzen. Der Standardwert beträgt 15 Minuten. |

## Bestellworkflow
1. **Erkennung** – Eine fertige Kerze, die das Volatilitätskriterium erfüllt, gibt die gewünschte Handelsrichtung vor.
2. **Validierung** – Quotes müssen verfügbar sein, der Handel muss erlaubt sein, es darf keine offene Position vorhanden sein und es darf keine andere aktive Order vorhanden sein.
3. **Platzierung** – Die Strategie platziert einen Kauf- oder Verkaufsstopp mit dem berechneten Offset und fügt Stop-Loss- und Take-Profit-Level hinzu.
4. **Trailing und Exit** – Nachdem eine Order ausgeführt wurde, überwacht das Trailing-Modul die neuesten Kurse. Beim Durchbrechen des Trailing-Levels wird die Position mit einer Marktorder geschlossen. Take-Profit- und Stop-Loss-Orders bleiben an die Position gebunden und werden vom Broker oder Simulator automatisch ausgeführt.

## Notizen
- Die Strategie erfordert sowohl Kerzen- als auch Level1-Datenabonnements, um ordnungsgemäß zu funktionieren.
- Die risikobasierte Größenanpassung fällt auf den konfigurierten `Volume` zurück, wenn Stop-Loss-Parameter oder Wertpapiermetadaten (Preisschritt oder Stufenpreis) nicht verfügbar sind.
- Trailing Stops werden intern über Marktausgänge verwaltet, um dem MetaTrader-Verhalten zu entsprechen und so die Kompatibilität zwischen verschiedenen Ausführungsplätzen sicherzustellen.
