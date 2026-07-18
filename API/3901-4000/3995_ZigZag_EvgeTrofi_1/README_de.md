# ZigZag EvgeTrofi 1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)


## Überblick
ZigZag EvgeTrofi 1 reproduziert das Verhalten des ursprünglichen MetaTrader-Expertenberaters, der auf den neuesten ZigZag-Schwungpunkt reagiert. Die Strategie überwacht jede abgeschlossene Kerze, identifiziert den aktuellsten ZigZag-Pivot mithilfe der klassischen Tiefen-, Abweichungs- und Backstep-Konfiguration und steigt in den Markt ein, wenn der Pivot noch aktuell ist. Ein Swing-Hoch löst eine Long-Position aus, während ein Swing-Tief eine Short-Position eröffnet, was der ursprünglichen EA-Signalkarte entspricht.

## Handelslogik
- Abonnieren Sie den konfigurierten Kerzentyp und füttern Sie die höchsten/niedrigsten Indikatoren, deren Länge mit dem ZigZag-Tiefenparameter übereinstimmt. Das Indikatorenpaar emuliert die native ZigZag-Schwungerkennung, ohne auf benutzerdefinierte Puffer angewiesen zu sein.
- Wenn eine Kerze schließt, prüfen Sie, ob ihr Hoch das verfolgte Maximum oder ihr Tief das verfolgte Minimum berührt. Wechseln Sie nur dann zu einem neuen Pivot, wenn die erforderliche Abweichung in den Preisschritten erfüllt ist und der Backstep-Abstand (Mindestbalken zwischen gegenüberliegenden Pivots) eingehalten wird.
- Sobald ein Pivot aufgezeichnet wurde, zählen Sie weiter, wie viele Balken vergangen sind. Der Dringlichkeitsparameter definiert, wie viele Balken nach dem Pivot noch als umsetzbar gelten. Signale, die älter als dieser Grenzwert sind, werden ignoriert, wodurch verspätete Eingaben verhindert werden.
- Bei einem hohen Pivot bereitet die Strategie den Kauf vor, bei einem niedrigen Pivot bereitet sie den Verkauf vor. Wenn eine offene Position bereits der beabsichtigten Richtung entspricht, wird das Signal als bearbeitet markiert und es werden keine weiteren Aufträge übermittelt.
- Wenn das Konto derzeit ein Engagement in der entgegengesetzten Richtung aufweist, sendet die Strategie eine Marktorder zur Abflachung, bevor ein neuer Handel eröffnet wird. Anschließend wird umgehend eine Marktorder mit dem konfigurierten Volumen zum Aufbau der neuen Position übermittelt.
- Jede Aktion erfordert einen vollständig ausgebildeten Indikatorzustand, eine fertige Kerze und ein positives Handelsvolumen. Die Strategie überprüft die Konnektivität und Berechtigungen mithilfe von `IsFormedAndOnlineAndAllowTrading()`, bevor mit dem Markt interagiert wird, und stellt so sicher, dass Aufträge nur unter gesunden Handelsbedingungen gesendet werden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Depth` | Zickzacktiefe, die das Schwungerkennungsfenster definiert. | 17 |
| `Deviation` | Mindestpreisbewegung in Punkten, die zur Bestätigung des Pivots desselben Typs erforderlich ist. Intern in Instrumentenpreisschritte umgerechnet. | 7 |
| `Backstep` | Mindestanzahl an Balken, die durchlaufen werden müssen, bevor zu einem entgegengesetzten Pivot gewechselt wird. | 5 |
| `Urgency` | Maximale Anzahl an Balken nach einem Pivot, während derer Trades zulässig sind. | 2 |
| `Candle Type` | Für Berechnungen verwendeter Kerzendatentyp (Zeitrahmen oder benutzerdefinierte Aggregation). | Zeitrahmen von 5 Minuten |
| `Volume` | Marktauftragsvolumen, das bei jedem Eintrag übermittelt wird. | 0,1 |

## Implementierungshinweise
- Die Höchst-/Tiefstindikatoren sind über die obere Ebene `SubscribeCandles().Bind()` API gebunden, sodass die Strategie nur auf die letzten Kerzen angewendet wird und eine manuelle Pufferung vermieden wird.
- Der Abweichungsparameter wird mithilfe der Instrumentenpreisstufe in eine absolute Preisdifferenz umgewandelt. Wenn für das Symbol keine Preisschritt-Metadaten vorhanden sind, wird ein Wert von 1 als Fallback verwendet, um die Logik über alle Börsen hinweg konsistent zu halten.
- Ein boolescher Schutz verhindert doppelte Trades pro Pivot und entspricht dem MetaTrader EA-Verhalten, das nur einmal pro Schwung auftritt.
- Die integrierte Chart-Integration zeichnet Kerzen und ausgeführte Trades automatisch, wenn Charts verfügbar sind, was dabei hilft, Swing-Punkte und Einstiege visuell zu validieren.
- Das Positionsmanagement ist symmetrisch: Jedes gegensätzliche Engagement wird mit einer Marktorder gleichen Volumens abgeflacht, bevor der neue Handel eingerichtet wird, sodass das Portfolio wie beim ursprünglichen Expertenberater einseitig bleibt.
