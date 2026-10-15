# NTK 07 Range-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die NTK 07 Range-Trader-Strategie ist ein Port des MetaTrader-Expertenberaters "NTK 07". Der Algorithmus hält symmetrische Stop-Aufträge rund um den aktuellen Marktpreis und verwaltet offene Positionen mit konfigurierbarer Trailing- und Take-Profit-Logik. Das Ziel ist es, Ausbrüche zu erfassen, die in der Nähe der Grenzen oder der Mitte einer kurzfristigen Preisspanne auftreten, während strenge Risikokontrollen eingehalten werden.

## Kernideen

- **Einstiegsauslöser** – Wenn die Strategie flach ist, bewertet sie eine konfigurierbare Lookback-Spanne. Wenn der Preis an den Rändern der Spanne oder in der Nähe ihres Mittelpunkts liegt (abhängig vom gewählten Trade-Modus), platziert sie sowohl Buy-Stop- als auch Sell-Stop-Aufträge in einem in Preisschritten definierten Offset.
- **Bereichsbewusstsein** – Die höchsten und niedrigsten Preise der letzten *N* abgeschlossenen Kerzen definieren den Handelsbereich. Eine Länge von null deaktiviert den Filter und erlaubt die sofortige Platzierung von Aufträgen.
- **Adaptives Risiko** – Jeder Einstieg verwendet das Basisvolumen, während ein optionaler Losmultiplikator zusätzliche Stop-Aufträge pyramidieren kann, nachdem eine Position eröffnet wurde. Ein portfolioweites Volumenlimit blockiert neue Aufträge, wenn die Exposition das Cap überschreiten würde.
- **Exit-Management** – Sobald eine Position gefüllt ist, wird der entgegengesetzte Stop-Auftrag storniert. Die Strategie registriert dann Schutz-Stop- und optionale Take-Profit-Aufträge mit den konfigurierten Offsets. Trailing kann dem Hoch/Tief der vorherigen Kerze, einem gleitenden Durchschnitt oder einem Festabstandspuffer folgen.
- **Sitzungsfilter** – Handel ist nur zwischen den ausgewählten Start- und Endstunden erlaubt und wird an Wochenenden automatisch deaktiviert.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Entry Volume** | Basisgröße für jeden Einstiegsauftrag. |
| **Total Volume Limit** | Maximale kumulative Positionsgröße. Ein Wert von `0` deaktiviert das Cap. |
| **Net Step** | Abstand in Preisschritten zwischen dem Markt und den Einstiegs-Stop-Aufträgen. |
| **Stop Loss** | Anfänglicher Stop-Loss-Offset in Preisschritten relativ zum Einstiegspreis. |
| **Take Profit** | Take-Profit-Abstand in Preisschritten. Auf `0` setzen, um Gewinnziele zu deaktivieren. |
| **Trailing Stop** | Abstand in Preisschritten für die Trailing-Logik. |
| **Lot Multiplier** | Multiplikator beim Pyramidieren in eine bestehende Position. |
| **Trail High/Low** | Wenn aktiviert, folgen Schutz-Stops den vorherigen Kerzenextremen. |
| **Trail Moving Average** | Aktiviert Trailing mit einem gleitenden Durchschnittswert. Nur ein Trailing-Modus kann aktiv sein. |
| **Trading Start/End Hour** | Einschließendes Plattform-Zeitfenster für den Handel. |
| **Range Bars** | Anzahl abgeschlossener Kerzen zur Berechnung des Handelsbereichs. `0` deaktiviert den Filter. |
| **Trade Mode** | `EdgesOfRange` erfordert, dass der Preis die Bereichsgrenzen berührt, `CenterOfRange` wartet, bis der Preis nahe dem Bereichsmittelpunkt ist. |
| **MA Period** | Länge des gleitenden Durchschnitts für Trailing. |
| **Candle Type** | Kerzen-Aggregation für alle Berechnungen. |

## Arbeitsablauf

1. **Daten-Abonnement** – Die Strategie abonniert die konfigurierte Kerzenserie und berechnet den gleitenden Durchschnitt sowie den höchsten und niedrigsten Preis über die gewählte Bereichslänge.
2. **Flacher Zustand** – Während keine Position offen ist, bewertet die Strategie die Bereichsbedingung. Wenn sie erfüllt ist, platziert sie gepaarte Buy-Stop- und Sell-Stop-Aufträge beim angegebenen Offset, wobei das globale Volumenlimit eingehalten wird.
3. **Positionshandling** – Wenn ein Einstieg gefüllt wird, wird der gegenüberliegende Stop storniert. Die Strategie platziert sofort Schutz-Stop-Loss- und optionale Take-Profit-Aufträge. Die Trailing-Logik aktualisiert dann den Schutz-Stop bei jeder neuen abgeschlossenen Kerze.
4. **Pyramidierung** – Wenn der Losmultiplikator größer als `1` ist, wird ein zusätzlicher Stop-Auftrag in die Richtung der aktuellen Position platziert, solange das Gesamtvolumenlimit dies erlaubt.
5. **Exit** – Stops oder Take-Profits flachen die Position ab und stornieren verbleibende Schutzaufträge. Das System kehrt dann zur Überwachung für die nächste Bereichsinteraktion zurück.

## Hinweise

- Die Strategie arbeitet ausschließlich mit Preisschritten, was sie für Instrumente mit unterschiedlichen Tick-Größen geeignet macht.
- Der Handel wird automatisch an Samstagen und Sonntagen deaktiviert, um das Verhalten der ursprünglichen MQL-Implementierung zu spiegeln.
- Es kann nur jeweils ein Trailing-Modus aktiviert sein; das Aktivieren beider löst beim Start einen Konfigurationsfehler aus.
