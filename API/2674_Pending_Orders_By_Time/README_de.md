# Strategie Pending Orders Nach Zeit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert den klassischen MetaTrader Expert "Pending orders by time" für StockSharp. Sie läuft nach einem diskreten Zeitplan: Jeden Tag platziert sie symmetrische Stop-Orders rund um den Markt, wenn eine neue Sessionsstunde beginnt, und löscht alle Orders plus offenen Positionen zu einer angegebenen Schlussstunde. Die Implementierung behält die ursprünglichen pip-basierten Eingaben bei, konvertiert sie in native Kurseinheiten und verwendet die High-Level-API zur Risikoverwaltung.

## Funktionsweise

1. **Zeitbasierter Trigger** – Wenn eine Kerze empfangen wird, die zur konfigurierten Eröffnungsstunde endet, sendet die Strategie einen Buy Stop über dem Ask und einen Sell Stop unter dem Bid. Beide Orders werden um den Parameter `Distance (pips)` verschoben, der in Kurseinheiten umgerechnet wird.
2. **Schutzbefehle** – `StartProtection` fügt automatisch Stop-Loss- und Take-Profit-Schutz mit den in den Parametern definierten Pip-Abständen hinzu. `ManageRisk` fungiert auch als Absicherung und schließt jede Restposition, wenn eine abgeschlossene Kerze zeigt, dass die Schwellenwerte überschritten wurden.
3. **Sitzungsschluss** – Wenn die Schlussstunde ankommt, storniert die Strategie alle verbleibenden Pending Orders und schließt offene Trades unabhängig von Gewinn oder Verlust zwangsweise. Dies reproduziert das Verhalten des ursprünglichen Experts, am Ende der Sitzung zurückzusetzen.
4. **Stellengerechte Pip-Größe** – Der Pip-Multiplikator emuliert die MetaTrader-Implementierung, indem der Preisschritt für Symbole mit drei oder fünf Dezimalstellen (z.B. JPY oder 5-stellige FX-Paare) mit zehn multipliziert wird. Dies hält Legacy-Eingaben konsistent über Broker hinweg.

Der Standard-Kerzentyp sind 30-Minuten-Balken, um unter der ursprünglichen Beschränkung von Perioden kürzer als H1 zu bleiben. Jeder andere Zeitrahmen kann verwendet werden, solange die resultierenden Stunden-Zeitstempel den gewünschten Sessionsstunden entsprechen.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Opening Hour` | Stunde (0-23), zu der die Strategie das Paar Stop-Orders platziert. | 9 |
| `Closing Hour` | Stunde (0-23), zu der alle Orders storniert und Positionen geschlossen werden. | 2 |
| `Distance (pips)` | Versatz in Pips zwischen dem aktuellen Kurs und den ausstehenden Stop-Einstiegen. | 20 |
| `Stop Loss (pips)` | Pip-Abstand für den Schutz-Stop, sobald eine Position offen ist. | 20 |
| `Take Profit (pips)` | Pip-Abstand für das Gewinnziel, sobald eine Position offen ist. | 500 |
| `Order Volume` | Menge für jede ausstehende Stop-Order. | 0.1 |
| `Candle Type` | Zeitrahmen, der den Stundenplan steuert. | 30-Minuten-Zeitrahmen |

Alle Parameter können optimiert werden. Pip-basierte Eingaben werden intern mit dem Preisschritt des Instruments konvertiert, sodass sie zwischen FX-Symbolen mit unterschiedlicher Dezimalpräzision portabel bleiben.

## Täglicher Arbeitsablauf

1. **Bei jedem Kerzenschluss** prüft die Strategie, ob der Stop-Loss- oder Take-Profit-Abstand erreicht wurde. Wenn ja, schließt sie die aktive Position zum Marktpreis.
2. **Wenn die Schlussstunde erreicht wird**, storniert sie alle ungefüllten Pending Orders und schließt die Position, um sicherzustellen, dass das Buch vor der nächsten Sitzung flach ist.
3. **Wenn die Eröffnungsstunde erreicht wird** (und die Strategie flach ist), storniert sie vorsichtshalber alte Orders und sendet einen neuen Sell Stop unter dem Bid und einen Buy Stop über dem Ask. Die Orders sind um den Spread gespiegelt, damit jeder Ausbruch erfasst werden kann.
4. **Während der gesamten Sitzung** hält der von `StartProtection` erstellte Plattformschutz einen Stop-Loss und Take-Profit angehängt, der sofort reagiert, wenn die Intrabar-Kursaction die Schwellenwerte erreicht.

## Verwendungshinweise

- Verwenden Sie Instrumente, deren Tick-Größe einen einzelnen "Punkt" darstellt, damit die Pip-Anpassung den ursprünglichen Expert widerspiegelt. Exotische Tick-Größen können manuelle Abstimmung der Abstandsparameter erfordern.
- Die Logik nimmt einen Handelszyklus pro Tag an. Wenn Sie Intraday-Daten mit mehreren Öffnungs-/Schließungs-Übereinstimmungen verwenden, passen Sie die Stunden entsprechend an.
- Da alle Aktionen beim Kerzenschluss stattfinden, wählen Sie eine Kerzengröße, die Ihrer gewünschten Bewertungshäufigkeit entspricht. Zum Beispiel bieten Stundenkerzen die gleiche Kadenz wie die MetaTrader-Version.
- Die Strategie platziert neue Pending Orders nur wenn die Position flach ist, um Überexponierung zu vermeiden, wenn ein Ausbruchstrade während der nächsten Eröffnungsstunde noch aktiv ist.

## Unterschiede zur MQL-Version

- Schutzausstiege werden über `StartProtection` plus explizite Prüfungen verwaltet und nutzen die High-Level-API von StockSharp anstatt direkter Stop-Loss-Zuweisung am Pending-Order-Ticket.
- Bid/Ask-Preise werden aus `Security.BestBid` und `Security.BestAsk` gelesen. Wenn diese Kurse nicht verfügbar sind, wird der Kerzenschluss als Fallback-Referenz verwendet.
- Marktorders werden verwendet, um Positionen zur Schlussstunde der Einfachheit halber zu liquidieren und brokerspezifische Verhaltensweisen zu vermeiden.
