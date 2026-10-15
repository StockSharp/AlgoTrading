# Strategie XP Trade Manager Grid (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **XP Trade Manager Grid**-Strategie ist eine direkte Portierung des MetaTrader 4 Expertenberaters `XP Trade Manager Grid.mq4`. Es automatisiert ein symmetrisches Raster, das jedes Mal, wenn sich der Markt um eine konfigurierbare Anzahl von Punkten vom zuletzt gefüllten Zweig entfernt, kontinuierlich neue Positionen hinzufügt. Der ursprüngliche Experte verwaltete Gewinne mit teilweisen Take-Profit-Niveaus für die ersten drei Aufträge, einem Break-Even-Cluster, wenn die Leiter größer wird, und einem globalen Risikoschutz basierend auf dem Kontoprozentsatz. Die StockSharp-Implementierung behält die gleichen Ideen bei, nutzt jedoch übergeordnete API-Primitive (Marktaufträge, Kerzenabonnements und Strategieparameter).

## Handelslogik

1. **Ersteingabe** – Die Strategie eröffnet sofort die allererste Marktorder in der vom Benutzer ausgewählten Richtung (standardmäßig verkaufen). Alle nachfolgenden Trades werden in der Rasterleiter gruppiert.
2. **Rastererweiterung** – Immer wenn der Schlusskurs um `StepPoints` * Preisschritt über den letzten Abschnitt auf einer Seite hinaus driftet, wird eine neue Marktorder in diese Richtung platziert, vorausgesetzt, dass die Gesamtzahl der gleichzeitigen Abschnitte unter `MaxOrders` liegt.
3. **Dedizierte TP für die ersten drei Etappen** – die ersten drei Aufträge jeder Seite erben ihre einzigartigen Take-Profit-Offsets (`TakeProfit1Partitive`, `TakeProfit2`, `TakeProfit3`). Sobald die Kerzenhochs/-tiefs diese Niveaus berühren, wird das Bein abgeflacht.
4. **Break-even-Cluster** – wenn die Gesamtzahl der offenen Beine vier oder mehr erreicht, berechnet die Strategie den gewichteten Break-even-Preis der gesamten Leiter. Je nachdem, welche Seite mehr Beine hat, wird dieser Break-Even um das entsprechende Gesamtziel (`TakeProfit4Total` … `TakeProfit15Total`), aufgeteilt auf die aktiven Aufträge, ausgeglichen. Wenn der Preis das berechnete Ziel erreicht, werden alle Engagements geschlossen.
5. **Zykluserneuerung** – Wenn die allererste Order eines Zyklus geschlossen wird, der gesammelte Gewinn in Punkten aber immer noch unter `TakeProfit1Total` liegt, wartet die Logik darauf, dass sich der Markt um `TakeProfit1Offset` Punkte über den letzten Ausstieg hinaus bewegt, und öffnet dann die ursprüngliche Order erneut.
6. **Risikokontrolle** – der variable Gewinn in der Kontowährung (realisiert + nicht realisiert) wird ständig mit `RiskPercent` Prozent des Portfolio-Startsaldos verglichen. Bei Überschreitung der Verlustschwelle wird die gesamte Leiter sofort abgeflacht.

Der C#-Port verfolgt intern jedes gefüllte Bein. Teilfüllungen werden unterstützt und abgesicherte Strukturen (gleichzeitige Käufe und Verkäufe) werden genau wie im MQL-Experten aufgelöst: Gegenseitige Füllungen heben zunächst ausstehende Positionen auf, bevor ein neues Engagement erfasst wird.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Datentyp, der zur Steuerung der Strategie verwendet wird (Standard: 1-Minuten-Kerzen). |
| `OrderVolume` | Volumen jeder Market-Order/jedem Leg. |
| `MaxOrders` | Maximal gleichzeitige Beine in beide Richtungen. |
| `StepPoints` | Abstand in Punkten zwischen aufeinanderfolgenden Gitterordnungen. |
| `RiskPercent` | Maximal tolerierbarer Floating-Verlust in % des Portfolio-Startguthabens. |
| `TakeProfit1Total` | Gesamtpunkteziel, das durch die Zyklen von Bestellung Nr. 1 angesammelt wurde, bevor keine automatische Verlängerung erfolgt. |
| `TakeProfit1Partitive` | Take-Profit-Distanz (Punkte) für die allererste Etappe. |
| `TakeProfit1Offset` | Erforderliche Mindest-Retracement-Distanz vor der Wiederherstellung der ersten Ordnung. |
| `TakeProfit2` / `TakeProfit3` | Individuelle TP-Offsets (Punkte) für die Strecken Nr. 2 und Nr. 3. |
| `TakeProfit4Total` … `TakeProfit15Total` | Break-Even-TP-Gesamtwerte werden verwendet, sobald die Leitergröße die entsprechende Anzahl an Aufträgen erreicht. |
| `InitialSide` | Richtung der allerersten Bestellung (Kauf oder Verkauf). |

> **Hinweis:** Alle punktbasierten Eingaben werden automatisch durch die Sicherheit `PriceStep` skaliert und entsprechen der ursprünglichen `Point()`-Logik von MetaTrader.

## Verhalten im Vergleich zur MetaTrader-Version

* Die StockSharp-Variante schließt die ersten drei Abschnitte über Marktaufträge ab, anstatt einzelne Take-Profit-Werte zu ändern, da die übergeordnete API-Variante keine direkte Auftragsänderung ermöglicht.
* Die Berechnung des variablen Gewinns basiert auf der Instrumentenstufe und dem Stufenpreis. Broker mit exotischen Vertragsspezifikationen müssen möglicherweise eine Feinabstimmung vornehmen, wenn sie diese Felder nicht offenlegen.
* In MT4 angezeigte Beschriftungen auf Plattformebene („Gewinn-Pips“ / „Gewinnwährung“) werden nicht reproduziert. Stattdessen werden interne Zyklusstatistiken verwendet, um zu entscheiden, wann die erste Bestellung wieder geöffnet werden soll.

## Anforderungen

* Hängen Sie die Strategie an ein Wertpapier an, das sowohl `PriceStep` als auch `StepPrice` verfügbar macht.
* Stellen Sie sicher, dass der Handelskonnektor sofortige oder stornierbare Marktaufträge unterstützt. Alle Rasterabschnitte werden über die Hilfsmethoden `BuyMarket`/`SellMarket` ausgeführt.

## Nutzungstipps

1. Beginnen Sie beim Testen mit kleinen `OrderVolume`-Werten, um zu bewerten, wie sich das Raster in Ihrem Feed verhält.
2. Passen Sie `StepPoints` sorgfältig an die Symbolvolatilität an. Größere Stufen reduzieren die Anzahl der offenen Beine und damit den Absenkvorgang.
3. Erhöhen Sie `TakeProfit1Offset` beim Handel mit Instrumenten mit größeren Spreads, um vorzeitige Wiedereinstiege zu vermeiden.
4. Kombinieren Sie die Strategie mit dem integrierten `StartProtection()`-Aufruf, der unerwartete Verbindungsabbrüche überwacht und die Verbindung ordnungsgemäß wiederherstellt.
