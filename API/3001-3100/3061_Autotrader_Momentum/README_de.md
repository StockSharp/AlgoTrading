# Autotrader Momentum Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Autotrader Momentum Strategie** ist eine Konvertierung des MetaTrader 5 Expert Advisors *Autotrader Momentum (barabashkakvn's Edition)*. Der Algorithmus bewertet den aktuellen Momentum, indem er den Schlusskurs der Überwachungskerze mit dem Schlusskurs einer historischen Referenzkerze vergleicht. Wird ein bullischer Momentum-Wechsel erkannt, kauft die Strategie; erscheint ein bärischer Wechsel, wird verkauft. Alle Orders werden zum Marktpreis über StockSharp's High-Level Trading-API ausgeführt.

Die Implementierung behält den ursprünglichen Fokus auf punktbasierter Risikosteuerung bei. Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände werden in Pips definiert und automatisch in Preisoffsets auf Basis des `PriceStep` des Instruments übersetzt. Die Unterstützung für drei- und fünfstellige Kursnotierungen bleibt durch Anwendung der gleichen 10x-Anpassung aus dem MQL-Code erhalten. Die Trailing-Logik wird bei jeder abgeschlossenen Kerze vor der Berücksichtigung neuer Einstiege ausgewertet, um sicherzustellen, dass das Risikomanagement das EA-Verhalten widerspiegelt, schützende Ausstiege zu priorisieren.

## Handelslogik
1. Den konfigurierten `CandleType` abonnieren und nur abgeschlossene Kerzen verarbeiten, passend zur "neuen Balken"-Logik des ursprünglichen EA.
2. Ein rollendes Fenster mit Schlusskursen der Größe `max(CurrentBarIndex, ComparableBarIndex) + 1` pflegen.
3. Den Schlusskurs der überwachten Kerze (`CurrentBarIndex`, Standard 0) mit dem Schlusskurs der historischen Kerze (`ComparableBarIndex`, Standard 15) vergleichen.
4. Ist der überwachte Schlusskurs größer als der Referenzschlusskurs, das Short-Exposure schließen und das konfigurierte Handelsvolumen kaufen.
5. Ist der überwachte Schlusskurs kleiner als der Referenzschlusskurs, das Long-Exposure schließen und das konfigurierte Handelsvolumen verkaufen.
6. Jeder Einstieg berechnet den durchschnittlichen Einstiegspreis neu und aktualisiert die Stop-Loss-, Take-Profit- und Trailing-Stop-Level.

Da StockSharp-Strategien mit einer Nettoposition arbeiten, kombinieren Umkehrungen das Volumen, das zum Schließen des entgegengesetzten Exposures erforderlich ist, mit dem konfigurierten Basisvolumen. Dies entspricht dem MQL-Verhalten, das zuerst die entgegengesetzte Seite schloss und dann eine neue Order der gewünschten Größe eröffnete.

## Parameter
- `CandleType` – Zeitrahmen für den Preisvergleich. Standard: 1 Stunde.
- `TradeVolume` – Basis-Marktordervolumen. Wird bei jedem Signal zusätzlich zu dem Volumen angewendet, das zum Umkehren einer bestehenden Position benötigt wird.
- `StopLossPips` – Schutz-Stop-Abstand in Pips. Auf 0 setzen, um den festen Stop-Loss zu deaktivieren.
- `TakeProfitPips` – Gewinnziel-Abstand in Pips. Auf 0 setzen, um den festen Take-Profit zu deaktivieren.
- `TrailingStopPips` – Abstand, der vom Trailing Stop eingehalten wird. Auf 0 setzen, um das Trailing zu deaktivieren.
- `TrailingStepPips` – Minimale günstige Bewegung, die erforderlich ist, bevor der Trailing Stop vorgerückt wird. Muss positiv sein, wenn Trailing aktiviert ist.
- `CurrentBarIndex` – Index der Überwachungskerze (0 = zuletzt abgeschlossener Balken).
- `ComparableBarIndex` – Index der historischen Kerze für den Momentum-Vergleich.

Alle Pip-basierten Einstellungen werden mithilfe des `PriceStep` des Instruments in Preisoffsets umgerechnet. Repräsentiert der Step drei oder fünf Dezimalstellen, wird der Offset mit 10 multipliziert, um die MetaTrader-Definition eines Pips zu reproduzieren.

## Risikomanagement
- **Feste Stops und Ziele:** Wenn `StopLossPips` oder `TakeProfitPips` größer als null sind, pflegt die Strategie entsprechende Preislevel relativ zum gemittelten Einstiegspreis.
- **Trailing Stop:** Aktiviert, wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind. Die Trailing-Logik verschiebt den Schutz-Stop erst, nachdem sich der Preis um mindestens `TrailingStopPips + TrailingStepPips` vom gemittelten Einstiegspreis entfernt hat, und repliziert damit die EA-Anforderung, die sicherstellte, dass die Bewegung groß genug ist, bevor der Stop angepasst wird.
- **Zustandsreset:** Kehrt die Position zu null zurück—entweder durch strategiegetriebene Ausstiege oder externe Eingriffe—wird der zwischengespeicherte Risikostand gelöscht, um veraltete Stop- oder Take-Profit-Level zu vermeiden.

## Implementierungshinweise
- Die Strategie stützt sich ausschließlich auf StockSharp's High-Level Markt-API (`BuyMarket`, `SellMarket`) und vermeidet Indikatorsammlungen, um den Konvertierungsrichtlinien treu zu bleiben.
- Schlusskurse werden in einer einfachen rollenden Liste gepuffert, sodass `CurrentBarIndex` und `ComparableBarIndex` zur Laufzeit ohne Neustart geändert werden können.
- Da StockSharp mit einer Nettoposition arbeitet, werden Stop-Loss- und Take-Profit-Level für das aggregierte Exposure verfolgt. Werden zusätzliche Orders in dieselbe Richtung geschichtet, berechnet der Code einen volumengewichteten Durchschnittseinstiegspreis, bevor die Risiko-Level aktualisiert werden.
- Trailing-Stop-Anpassungen und schützende Ausstiege werden vor neuen Signalen bei jeder Kerze verarbeitet, um zu verhindern, dass neue Einstiege ausgewertet werden, wenn bereits ein Ausstieg für diesen Balken ausgegeben wurde.

## Originalstrategie-Referenz
- **Quelle:** `MQL/22409/Autotrader Momentum.mq5`
- **Autor:** barabashkakvn (MetaTrader-Community)
