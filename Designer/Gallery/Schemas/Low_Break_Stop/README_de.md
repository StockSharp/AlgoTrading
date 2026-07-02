# Beispiel einer Tief-Ausbruch-Strategie mit Stop im StockSharp Strategy Designer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Dieses Beispiel demonstriert eine „Tief-Ausbruch mit Stop"-Handelsstrategie, die im StockSharp Strategy Designer konfiguriert ist. Sie ist darauf ausgelegt, Trades auf Basis spezifischer Tief-Ausbruchsbedingungen auszuführen und dabei Stop-Loss-Parameter zum Risikomanagement einzusetzen. Die Strategie nutzt Echtzeit-Marktdaten, um zu erkennen, wann der Kurs eines Wertpapiers über einen bestimmten Zeitraum unter ein vordefiniertes Tief fällt, und initiiert dann Trades mit definierten Stop-Bedingungen.

![schema](schema.png)

## Beschreibung des Schemas

Das im JSON-File beschriebene Schema skizziert einen detaillierten Arbeitsablauf für den Handel auf Basis der Preisentwicklung relativ zu historischen Tiefs:

1. **Instrument-Knoten**: Dies ist der primäre Eingabeknoten, in dem das Zielinstrument [definiert wird](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) und als Basis für die Eingabe von Marktpreisdaten dient.

2. **TimeFrameCandle-Knoten**: Verarbeitet die eingehenden Marktdaten, um [Kerzen](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) zu erzeugen, die für die Analyse von Kursbewegungen über bestimmte Zeitintervalle unerlässlich sind.

3. **Tief-Indikator-Knoten**: Diese Knoten [berechnen den niedrigsten Kurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) über eine bestimmte Anzahl von Perioden und identifizieren potenzielle Ausbruchsniveaus für die Trade-Initiierung.

4. **Vergleichsknoten**: Dienen zum [Vergleich](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) des aktuellen Kurses mit dem historischen Tief und lösen Handelssignale aus, wenn der aktuelle Kurs unter den festgelegten Schwellenwert fällt, was einen Bären-Ausbruch anzeigt.

5. **Chart-Panel-Knoten**: Visualisiert die Handelsdaten und Indikatoren und liefert eine [grafische Darstellung](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html) der Strategie-Operationen, die für die Echtzeit-Überwachung und Strategieanpassungen unerlässlich ist.

6. **Trade-Ausführungsknoten (Kauf/Verkauf)**: Sind für die [Ausführung von Trades](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) gemäß der Strategie-Logik verantwortlich. In diesem Fall kann eine Verkaufsorder ausgeführt werden, um von der erwarteten Abwärtsbewegung zu profitieren.

7. **Stop-Order-Knoten**: Implementiert [Stop-Loss](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)-Bedingungen zur effektiven Risikosteuerung. Dadurch wird sichergestellt, dass Trades bei einem vordefinierten Verlustschwellenwert geschlossen werden, um sich gegen signifikante nachteilige Bewegungen zu schützen.

## Arbeitsablauf

- Der **Instrument-Knoten** liefert die notwendigen Marktdaten für die Strategie.
- Diese Daten fließen in den **TimeFrameCandle-Knoten**, wo sie in verwertbare Kerzenformate umgewandelt werden.
- Die **Tief-Indikator-Knoten** analysieren diese Kerzen, um historische Tiefs zu ermitteln.
- **Vergleichsknoten** überwachen den aktuellen Marktpreis im Vergleich zu diesen Tiefs und aktivieren Trades, wenn der Kurs unter das historische Tief fällt.
- Die **Trade-Ausführungsknoten** nutzen diese Signale, um Verkaufsorders in Erwartung einer Fortsetzung des Abwärtstrends auszuführen.
- Gleichzeitig legen **Stop-Order-Knoten** Stop-Loss-Orders auf Basis vordefinierter Kriterien fest, um potenzielle Verluste zu begrenzen.
- Der **Chart-Panel-Knoten** zeigt alle Transaktionen und Kursbewegungen an und liefert visuelles Feedback über die Performance der Strategie.

## Praktische Anwendung

Diese Konfiguration ist besonders nützlich für Trader, die sich auf Ausbruchsstrategien konzentrieren, bei denen das Erkennen und Reagieren auf signifikante Kursbewegungen zu profitablen Gelegenheiten führen kann. Die Strategie eignet sich für:
- hochvolatile Märkte, in denen Kursschwankungen erhebliche Handelsmöglichkeiten bieten können;
- Day-Trader, die schnelle Kursbewegungen nutzen und robuste Mechanismen zur effektiven Risikosteuerung benötigen.

## Fazit

Das Beispiel der „Tief-Ausbruch mit Stop"-Strategie im StockSharp Strategy Designer zeigt einen fortgeschrittenen Ansatz des algorithmischen Handels durch die Kombination von Echtzeit-Datenverarbeitung mit ausgefeilten Risikomanagement-Techniken. Diese Strategie bietet einen dynamischen Rahmen zur Ausnutzung von Kursausbrüchen, während gleichzeitig sichergestellt wird, dass Risikoparameter strikt eingehalten werden — ein unverzichtbares Werkzeug für Trader, die ihre Renditen durch präzise und kontrollierte Handelsmethoden maximieren möchten.
