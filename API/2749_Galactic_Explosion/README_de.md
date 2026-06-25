# Galaktische-Explosion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Galaktische-Explosion-Strategie baut den ursprünglichen MetaTrader-5-Grid-Experten in StockSharp nach. Sie operiert auf fertigen Kerzen, verwendet einen langfristigen gleitenden Durchschnitt zur Definition des Richtungs-Bias und entfaltet ein expandierendes Order-Grid. Das System akkumuliert Trades, wenn der Preis auf einer Seite des gleitenden Durchschnitts bleibt, und schließt den gesamten Korb, sobald ein vordefiniertes Gewinnziel erreicht wird.

## Marktlogik
1. **Richtungsfilter** – die Strategie vergleicht den letzten Kerzenschluss mit einem gleitenden Durchschnitt. Wenn der Preis unter dem Durchschnitt schließt, wird der Bias bullisch, wenn der Preis über dem Durchschnitt schließt, wird er bearisch.
2. **Progressives Grid** – die ersten acht Einstiege werden genommen, wenn der Bias es erlaubt. Nach der achten Position steuert der Abstand zwischen dem aktuellen Preis und sowohl dem letzten als auch dem ersten Einstieg, ob weitere Trades erlaubt sind.
3. **Abstandskontrolle** – Abstände werden in Preisschritten gemessen. Wenn sich der Preis weit genug vom letzten Einstieg entfernt hat, fügt die Strategie dem Korb hinzu. Je nach Abstand zum allerersten Einstieg handelt sie sofort, überspringt drei Kerzen oder überspringt sechs Kerzen, bevor sie erneut hinzufügt.
4. **Gewinnrealisierung** – realisierter PnL plus offener Gewinn des Korbs wird mit dem minimalen Gewinnziel verglichen. Wenn der Schwellenwert erreicht ist, werden alle offenen Positionen in einer einzigen Marktorder geschlossen.

## Trade-Management
- **Einstiegsvolumen** – jeder Trade wird mit dem konfigurierten Ordervolumen ausgeführt. Wenn das Signal kippt, während eine Position gehalten wird, sendet die Strategie eine einzelne Order, die die alte Seite schließt und eine neue mit dem erforderlichen zusätzlichen Volumen öffnet.
- **Positions-Tracking** – die Strategie hält den Durchschnittspreis und den Erst-/Letzteinstiegspreis für Long- und Short-Körbe unabhängig voneinander. Dies ermöglicht ihr, die abstandsbasierten Skalierungsregeln des ursprünglichen Experten zu reproduzieren.
- **Session-Filter** – der Handel ist nur zwischen den konfigurierten Start- und Endstunden aktiv. Die Logik verwendet die Kerzen-Eröffnungszeit und ignoriert Signale außerhalb dieses Fensters.
- **Sicherheitscheck** – wenn das Handelsfenster falsch konfiguriert ist (zum Beispiel ist die Startstunde nicht früher als die Endstunde), überspringt die Strategie den Handel und protokolliert eine Warnung.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| **Order Volume** | Volumen für jeden neuen Einstieg. Dieser Wert wird auch verwendet, um zu schätzen, wie viele Grid-Schritte aktuell offen sind. |
| **Start Hour** | Beginn der Handelssitzung in Börsenzeit. Signale vor dieser Stunde werden ignoriert. |
| **End Hour** | Ende der Handelssitzung (exklusiv). Signale nach dieser Stunde werden ignoriert. |
| **Minimal Profit** | Kombination aus realisiertem und nicht realisiertem Gewinn, die das Schließen aller offenen Positionen auslöst. |
| **Indent After 8th** | Mindestabstand (in Preisschritten) vom letzten Einstieg nach acht Trades, bevor eine weitere Position eröffnet werden kann. |
| **Skip 3 Min** | Untere Grenze (in Preisschritten) zur Aktivierung der „Drei-Kerzen-Überspringen"-Regel. |
| **Skip 3 Max** | Obere Grenze (in Preisschritten), die die „Drei-Kerzen-Überspringen"-Regel aktiv hält. |
| **Skip 6 Max** | Obere Grenze (in Preisschritten), die die „Sechs-Kerzen-Überspringen"-Regel aktiv hält. |
| **MA Length** | Länge des einfachen gleitenden Durchschnitts, der den Richtungs-Bias definiert. |
| **Candle Type** | Von der Strategie abonnierte Kerzenserie. Gleitender Durchschnitt und Grid-Logik laufen auf diesem Datenstrom. |

## Implementierungshinweise
- Die Strategie verwendet `SubscribeCandles` mit einem `SimpleMovingAverage`-Indikator und verarbeitet nur fertige Kerzen.
- Positionsstatistiken werden durch `OnNewMyTrade` gepflegt, was ein präzises Tracking der Erst- und Letzteinstiegspreise sowie Durchschnittspreise für offene Körbe ermöglicht.
- Abstandsschwellen werden mit dem Wertpapier-`PriceStep` skaliert, was die ursprüngliche pip-basierte Konfiguration des MT5-Experten reproduziert.
- Die Implementierung vermeidet benutzerdefinierte Sammlungen und fokussiert auf skalare Zustandsvariablen, um den Projektrichtlinien zu entsprechen.
