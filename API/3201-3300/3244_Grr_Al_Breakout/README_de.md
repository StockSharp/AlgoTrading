# Grr Al Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Grr Al Breakout-Strategie** ist ein direkter Port des MetaTrader-Expertenberaters `grr-al.mq5`. Sie beobachtet den ersten Preis zu Beginn jeder Kerze und wartet darauf, dass sich der Markt eine konfigurierbare Distanz von diesem Ankerniveau entfernt. Wenn die Bewegung den Schwellenwert überschreitet, führt die Strategie genau einen Trade für diese Kerze aus und kehrt optional die bestehende Exposure um.

Die StockSharp-Implementierung behält das Verhalten des ursprünglichen timer-gesteuerten Roboters bei, übersetzt es aber in das High-Level-Kerzen-Abonnement-Modell. Jeder neue Kerzen-Snapshot liefert den anfänglichen Referenzpreis, während nachfolgende Aktualisierungen derselben Kerze den neuesten Schlusskurs als Live-Marktpreis bereitstellen. Dieser Ansatz recreiert die Tick-für-Tick-Ausbruchserkennung ohne auf Low-Level-Ereignisverarbeitung zurückgreifen zu müssen.

## Handelslogik
1. **Ankererkennung** – wenn eine neue Kerze beginnt, speichert die Strategie ihren Eröffnungspreis (oder den ersten verfügbaren Schlusskurs wenn die Eröffnung noch nicht verfügbar ist) und setzt den Pro-Kerzen-Trigger zurück.
2. **Ausbruchsprüfung** – solange während der aktuellen Kerze kein Trade ausgeführt wurde, wird der neueste Schlusskurs mit dem Anker verglichen. Wenn der Preis um mehr als `DeltaPoints` steigt (in Preis umgerechnet durch die Instrumenten-Punktgröße), wird eine Short-Position eröffnet. Wenn der Preis um dieselbe Distanz fällt, wird eine Long-Position eröffnet.
3. **Einzelausführung pro Kerze** – sobald ein Ausbruchs-Trade ausgelöst wird, sind keine weiteren Orders erlaubt bis die nächste Kerze beginnt, was das `br`-Flag des ursprünglichen EA imitiert.
4. **Risikomanagement** – optionale Stop-Loss- und Take-Profit-Abstände werden unmittelbar nach dem Öffnen einer Position angewendet. Wenn die Order nur eine entgegengesetzte Exposure reduziert, werden die Schutz-Brackets übersprungen, um zu vermeiden, Stops an ein flaches Portfolio anzuhängen.
5. **Positionsgrößenbestimmung** – die Strategie kann mit einem fixen Volumen handeln oder die Ordergröße auf einen Bruchteil des vom Broker gemeldeten Maximalvolumens begrenzen.

## Parameter
- `Volume` – Basisvolumen (in Kontrakten) wenn `RiskFraction` null ist. Entspricht der `BASELOT`-Konstante der MQL-Version.
- `RiskFraction` – Wert zwischen 0 und 1. Wenn größer als null, begrenzt die Strategie die Ordergröße durch Multiplikation des Broker-Maximalvolumens mit diesem Bruchteil und verwendet den kleineren Wert zwischen diesem Limit und `Volume`.
- `DeltaPoints` – Anzahl der Instrumentenpunkte, die der Preis von der Kerzeneröffnung weg bewegen muss, um einen Trade auszulösen. Äquivalent zur `DELTA`-Konstante.
- `StopLossPoints` – Schutz-Stop-Abstand in Punkten. Null deaktiviert den Stop, wie die `SL`-Konstante null in MQL.
- `TakeProfitPoints` – Take-Profit-Abstand in Punkten. Null deaktiviert das Ziel und repliziert das `TP`-Konstantenverhalten.
- `CandleType` – StockSharp-Kerzendeskriptor für den Zeitrahmen zum Ankern und Überwachen von Ausbrüchen. Standardmäßig Fünf-Minuten-Zeitrahmen, kann aber auf jeden unterstützten Zeitraum geändert werden.

## Hinweise und Unterschiede zur MQL-Version
- Der ursprüngliche EA verwendete Tick-Ereignisse mit einem Einsekundentimer. Dieser Port nutzt die StockSharp-Kerzen-Abonnement-API, die automatisch den neuesten Kerzenstatus liefert; kein manuelles Timer-Management erforderlich.
- Bid/Ask-Differenzierung ist in der High-Level-Schnittstelle nicht verfügbar, daher verwendet die Strategie den Kerzen-Schlusskurs als Proxy für den Transaktionspreis. Stop-Loss- und Take-Profit-Offsets werden weiterhin in Punkten angewendet, was dem MetaTrader-Punktarithmtik-Verhalten entspricht.
- Die risikobasierte Volumenberechnung in MetaTrader basierte auf Margenschätzung für eine feste Eins-Lot-Order. In diesem Port wird die Berechnung auf einen Maximalvolumen-Bruchteil vereinfacht, damit sie broker-agnostisch bleibt.
- Da StockSharp-Strategien netto-positionsbasiert sind, kann das Senden einer Order in die entgegengesetzte Richtung die Exposure automatisch glätten oder umkehren, ähnlich dem `OrderSend`-Aufruf mit Netting-Modus in MetaTrader 5.

## Verwendung
1. Die Strategie an ein Wertpapier und Portfolio in Designer, Runner oder einer benutzerdefinierten StockSharp-Host-Anwendung anhängen.
2. Gewünschten Kerzen-Zeitrahmen, Ausbruchsdistanz, Stop-Loss, Take-Profit und Volumen-Parameter konfigurieren.
3. Strategie starten. Sie abonniert automatisch die gewählten Kerzen, überwacht jede neue Kerze auf eine Ausbruchsbewegung und platziert Marktorders wenn die konfigurierten Bedingungen erfüllt sind.

## Originalquelle
- MetaTrader 5-Expertenberater: `MQL/244/grr-al.mq5`
