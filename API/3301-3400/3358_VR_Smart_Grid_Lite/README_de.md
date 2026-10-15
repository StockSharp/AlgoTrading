# VR Smart Grid Lite-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **VR Smart Grid Lite-Strategie** repliziert die Logik des gleichnamigen Expertenberaters MetaTrader. Die Strategie erstellt mithilfe von Marktaufträgen ein Mittelungsgitter im Martingal-Stil. Die Positionsgröße beginnt bei einem Basisvolumen und verdoppelt sich jedes Mal, wenn sich der Preis um eine benutzerdefinierte Distanz gegenüber der bestehenden Position bewegt. Die Strategie unterstützt zwei Ausstiegsmodi: die Schließung der Extremgeschäfte zu einem gewichteten Take-Profit-Preis oder die teilweise Reduzierung des Engagements bei gleichzeitig aktivem Netz.

## Parameter
- **Take Profit (Pips)** – Abstand in Pips, der zum Ausstieg benötigt wird, wenn nur eine Position aktiv ist.
- **Startvolumen** – anfängliches Ordervolumen für den ersten Trade in jede Richtung.
- **Maximales Volumen** – feste Obergrenze für jede einzelne vom Raster geöffnete Bestellung.
- **Schließmodus** – `Average` schließt die ältesten und neuesten Bestellungen zu einem gewichteten Ziel; `PartClose` schließt einen Teil der neuesten Bestellung und die gesamte älteste Bestellung.
- **Order Step (Pips)** – Mindestpreisentfernung, die gegenüber der Position zurückgelegt werden muss, bevor ein neuer Trade eröffnet wird.
- **Minimaler Gewinn (Pips)** – zusätzliche Gewinnspanne, die zum gewichteten durchschnittlichen Ausstiegspreis hinzugefügt wird.
- **Slippage (Pips)** – Platzhalterparameter, der der Vollständigkeit halber vom ursprünglichen EA übernommen wurde.
- **Kerzentyp** – Zeitrahmen, der zur Entscheidungsfindung verwendet wird (die zuvor abgeschlossene Kerze bestimmt die Handelsausrichtung).

## Algorithmus
1. Bei jeder fertigen Kerze bewertet die Strategie die Richtung der vorherigen Kerze.
2. Wenn die vorherige Kerze bullisch schloss und entweder keine Long-Trades vorhanden sind oder der Preis um den konfigurierten Schritt nach unten gesunken ist, wird eine neue **Kaufmarkt**-Order platziert.
3. Wenn die vorherige Kerze bärisch geschlossen hat und entweder keine Short-Trades vorhanden sind oder der Preis um die konfigurierte Stufe gestiegen ist, wird eine neue **Verkaufsmarkt**-Order platziert.
4. Die Volumina werden ausgehend von der Position mit dem niedrigsten Preis in der Richtung berechnet und auf jeder neuen Ebene verdoppelt, wobei das maximale Volumen und die Broker-Volumenschritte berücksichtigt werden.
5. Wenn nur noch eine Position übrig ist, wendet die Strategie die einfache Take-Profit-Distanz an und steigt bei Berührung aus.
6. Bei mehreren Positionen berechnet die Strategie gewichtete Durchschnitte anhand der extremen Einträge:
   - Der **Durchschnittsmodus** schließt beide Extreme, wenn der Preis das gewichtete Ziel plus den minimalen Gewinnpuffer erreicht.
   - **PartClose-Modus** schließt einen Teil der neuesten Bestellung, der dem Startvolumen entspricht, und schließt die älteste Bestellung vollständig, sodass das Raster mit reduzierter Belichtung weiterlaufen kann.
7. Alle gefüllten und geschlossenen Positionen werden verfolgt, um den internen Rasterstatus mit dem Live-Portfolio synchronisiert zu halten.

## Notizen
- Die Strategie basiert auf Marktaufträgen, sodass die tatsächliche Ausführungsqualität und der Slippage von den Bedingungen des Brokers abhängen.
- Stellen Sie sicher, dass die Volumenbeschränkungen des Instruments (Mindestvolumen und Volumenschritt) mit dem ausgewählten Startvolumen kompatibel sind.
- Wie bei jedem Grid- oder Martingal-Ansatz kann das Risiko schnell wachsen, wenn die Märkte stark gegen die Position tendieren; Verwenden Sie ein umsichtiges Geldmanagement.
