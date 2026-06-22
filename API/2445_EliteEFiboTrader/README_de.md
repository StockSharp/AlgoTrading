# Elite eFibo Trader Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Elite eFibo Trader Strategie** ist eine Konvertierung des MQL5 Expert Advisors "Elite eFibo Trader". Sie implementiert ein Fibonacci-basiertes Mittelungsgitter, das eine erste Marktposition eröffnet und zusätzliche Stop-Orders in festen Abständen schichtet. Die Strategie arbeitet mit Tick-Daten und verwaltet Trailing Stops automatisch, während das Gitter sich ausdehnt.

## Funktionsweise
1. Wenn keine Positionen oder ausstehenden Orders aktiv sind und der Handel erlaubt ist, startet die Strategie einen neuen Zyklus in der ausgewählten Richtung (Kauf oder Verkauf).
2. Die erste Order wird zum Marktpreis mit dem für `LotsLevel1` konfigurierten Volumen gesendet. Dreizehn weitere Stop-Orders werden bei Vielfachen von `LevelDistance` vom aktuellen Preis platziert. Ihre Volumina folgen der durch `LotsLevel2` … `LotsLevel14` definierten Fibonacci-Sequenz.
3. Jede ausgeführte Order setzt ein individuelles Stop-Niveau `StopLossPoints` vom Einstandspreis entfernt. Das höchste (für Long-Positionen) oder niedrigste (für Short-Positionen) dieser Stops wird zum aktiven Trailing-Niveau für alle offenen Positionen.
4. Wenn der Preis das Trailing-Niveau erreicht, wird die gesamte Position geschlossen und alle verbleibenden ausstehenden Orders werden storniert.
5. Unrealisierter Gewinn wird in der Kontowährung überwacht. Sobald er `MoneyTakeProfit` erreicht, wird das Gitter geschlossen. Abhängig von `TradeAgainAfterProfit` startet die Strategie automatisch neu oder wartet auf manuelle Reaktivierung.

Die Strategie benötigt Tick-Level-Marktdaten über `SubscribeTrades()` und erwartet, dass nur eine Richtung (`OpenBuy` xor `OpenSell`) gleichzeitig aktiviert ist.

## Parameter
- `OpenBuy` – aktiviert die Long-only-Version des Gitters.
- `OpenSell` – aktiviert die Short-only-Version des Gitters.
- `TradeAgainAfterProfit` – startet automatisch einen neuen Zyklus nach der Gewinnmitnahme.
- `LevelDistance` – Abstand zwischen ausstehenden Orders, gemessen in Kursschritten des Instruments.
- `StopLossPoints` – Stop-Loss-Abstand von jedem Einstieg, gemessen in Kursschritten.
- `MoneyTakeProfit` – unrealisiertes Gewinnziel in Kontowährung.
- `LotsLevel1` … `LotsLevel14` – individuelle Volumina für jede Gitterstufe. Standardwerte folgen der Fibonacci-Sequenz (1, 1, 2, 3, 5, …, 377).

## Details zur Handelslogik
- Preisabstände werden mit dem `PriceStep` des Instruments berechnet; ist dieser null, platziert die Strategie keine Orders.
- Es ist immer nur ein Handelszyklus aktiv. Alle ausstehenden Orders werden zu Zyklusbeginn erstellt und bleiben bis zur Ausführung oder expliziten Stornierung bestehen.
- Trailing Stops werden neu berechnet, wenn eine neue Gitterstufe gefüllt wird oder Teile der Position geschlossen werden. Dies stellt sicher, dass alle Orders das beste verfügbare Schutzniveau teilen.
- Die Gewinnskontrolle basiert auf dem Floating PnL, abgeleitet aus `Position`, `PositionPrice`, `PriceStep` und `StepPrice`.
- Wenn `TradeAgainAfterProfit` deaktiviert ist, bleibt die Strategie nach Erreichen des Geldziels inaktiv, bis der Parameter manuell wieder aktiviert wird.

## Verwendungshinweise
- Konfigurieren Sie die korrekte Richtung vor dem Start (Long oder Short). Das gleichzeitige Aktivieren beider Richtungen verhindert den Gitterstart.
- Passen Sie Stufenabstände und Volumina gemäß der Volatilität des Instruments und der Kontraktgröße an. Große Fibonacci-Volumina erzeugen aggressives Skalieren und sollten sorgfältig mit historischen Daten getestet werden.
- Stellen Sie sicher, dass das Handelskonto und der Broker Stop-Orders zu den berechneten Kursniveaus unterstützen; andernfalls können Orders abgelehnt werden.
