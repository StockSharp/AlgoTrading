# MT45-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die MT45-Strategie ist eine direkte Konvertierung des ursprünglichen MetaTrader Expert Advisors. Sie wechselt bei jeder abgeschlossenen Bar zwischen Long- und Short-Marktpositionen, während jeder Trade mit denselben fixen Take-Profit- und Stop-Loss-Abständen geschützt wird, die in der MQL-Implementierung verwendet wurden. Die Positionsgröße folgt einer Martingal-Wiederherstellungsregel, sodass der nächste Trade sein Volumen nur nach einem Verlustergebnis erhöht.

## Handelslogik
1. Die Strategie abonniert eine einzelne Kerzenserie, die durch den **Candle Type**-Parameter definiert wird, und wartet auf abgeschlossene Kerzen, um Intrabar-Rauschen zu vermeiden.
2. Wenn keine Position offen ist und die vorherige Einstiegsorder vollständig verarbeitet wurde, sendet der Algorithmus eine Market Order in die für diesen Turn geplante Richtung (Kauf, dann Verkauf, dann Kauf, ...).
3. Die Richtung wechselt nur nach Ausführung der entsprechenden Order, um sicherzustellen, dass die Wechselmethode dem Verhalten des MQL-Experten entspricht, bei dem jeder abgeschlossene Trade die Seite für das nächste Signal umschaltet.
4. Schützende Stop-Loss- und Take-Profit-Orders werden automatisch über `StartProtection` verwaltet, sodass die Strategie den Markt verlässt, wenn eine der Abstände erreicht wird.

## Positionsdimensionierung
* **Base Volume** legt die anfängliche Losgröße fest. Sie wird nach jedem gewinnbringenden oder ausgeglichenen Trade wiederhergestellt.
* Nach einem Verlust-Trade wird das Volumen des nächsten Einstiegs mit **Martingale Multiplier** multipliziert. Wenn der skalierte Wert **Max Volume** überschreiten würde, fällt die Strategie auf das Basisvolumen zurück, um unkontrolliertes Wachstum zu vermeiden.
* Der realisierte Gewinn oder Verlust wird durch den Vergleich des Ausstiegspreises mit dem gespeicherten Einstiegspreis gemessen, was die `Lot()`-Funktion des ursprünglichen Expert Advisors reproduziert.

## Risikomanagement
* **Stop Points** und **Take Points** werden in Preisschritten ausgedrückt und spiegeln den `_Point`-Multiplikator wider, der auf MetaTrader verwendet wurde. Die Strategie konvertiert diese Werte über den `PriceStep` des Instruments in absolute Preisabstände, bevor `StartProtection` aktiviert wird.
* Schützende Orders werden automatisch an jede Position angehängt und werden symmetrisch für Long- und Short-Trades platziert.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| Stop Points | Abstand zum Schutz-Stop in Instrumenten-Preisschritten. | 600 |
| Take Points | Abstand zum Take-Profit-Ziel in Instrumenten-Preisschritten. | 700 |
| Base Volume | Basisvolumen für neue Positionen nach Gewinnen. | 0.01 |
| Martingale Multiplier | Volumen-Multiplikator nach Verlusten. | 2 |
| Max Volume | Maximal erlaubtes Volumen für die Martingal-Skalierung. | 10 |
| Candle Type | Kerzenserie zur Erkennung des Balkenabschlusses (Standard: 1 Minute). | 1 Minute |

## Verwendungshinweise
* Wähle den Kerzen-Zeitrahmen, der dem Chart-Zeitrahmen des ursprünglichen Experten entspricht. Die Logik operiert ausschließlich auf abgeschlossenen Kerzen.
* Die Strategie stellt keinen weiteren Einstieg in die Warteschlange, während eine Order aussteht oder eine Position aktiv ist; sie wartet immer, bis der bestehende Trade über Stop-Loss oder Take-Profit schließt.
* Es gibt derzeit keine separate Python-Version für diese Strategie, entsprechend den Projektrichtlinien.
