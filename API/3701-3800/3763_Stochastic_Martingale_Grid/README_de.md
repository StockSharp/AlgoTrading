# Stochastic Martingale Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader-Expertenberaters `rmkp_9yj4qp1gn8fucubyqnvb`. Es kombiniert einen stochastischen Oszillator-Eintrittsfilter mit einem Martingal-Mittelungsgitter. Der Algorithmus überwacht abgeschlossene Kerzen, wartet darauf, dass die stochastische Signallinie vordefinierte überkaufte oder überverkaufte Zonen verlässt, und eröffnet dann eine Position in Richtung der Umkehr. Wenn sich der Preis gegen den Handel bewegt, werden durchschnittliche Aufträge mit doppeltem Volumen und festen Pip-Abständen hinzugefügt. Jedes Segment verfügt über ein eigenes Take-Profit-Ziel und ein eigenes Trailing-Stop-Management, sodass Positionen unabhängig voneinander skaliert werden können, sobald sich der Preis erholt.

## Handelslogik
- **Signalerkennung:**
  - Die %K- und %D-Linien eines konfigurierbaren stochastischen Oszillators werden an abgeschlossenen Kerzen ausgewertet.
  - Ein Long-Setup wird ausgelöst, wenn bei der vorherigen Kerze %K über %D und %D unter dem Schwellenwert `ZoneBuy` lag.
  - Ein kurzer Setup wird ausgelöst, wenn bei der vorherigen Kerze %K unter %D und %D über dem Schwellenwert `ZoneSell` lag.
- **Erste Ausführung:**
  - Bei einem gültigen Signal und während das Konto leer ist, sendet die Strategie eine Marktorder mit dem `BaseVolume`.
  - Der Einstiegspreis wird gespeichert, um Trailing Stops und spätere Durchschnittsaufträge zu verwalten.
- **Martingale Mittelung:**
  - Während eine Position offen bleibt, achtet der Algorithmus auf eine ungünstige Preisbewegung von `StepPips` gegenüber der zuletzt ausgeführten Order.
  - Jede neue Durchschnittsorder verdoppelt das Volumen des vorherigen Abschnitts (klassische Martingal-Progression) und wird nur dann platziert, wenn die Gesamtzahl der offenen Abschnitte unter `MaxOrders` liegt und der Handel weiterhin zulässig ist.
- **Exit-Management:**
  - Jedes Bein definiert ein individuelles Take-Profit-Level, das `TakeProfitPips` von seinem Einstiegspreis entfernt liegt.
  - Trailing Stops werden aktiviert, sobald der nicht realisierte Gewinn `TrailingStopPips` erreicht; Der Schleppanker wird festgezogen, wenn die Gewinne weiter steigen.
  - Wenn der Preis auf das Trailing-Niveau zurückfällt oder das Take-Profit-Niveau erreicht, wird das entsprechende Segment geschlossen, während der Rest des Clusters aktiv bleibt.
  - Wenn alle Zweige austreten, setzt die Strategie ihren internen Zustand zurück und wartet auf das nächste stochastische Signal.

## Risikomanagement
- Die Martingal-Erweiterung ist durch `MaxOrders` und die Sicherheitsvolumenbeschränkungen begrenzt.
- Die Lautstärken werden auf den `VolumeStep` des Instruments normalisiert und die minimalen/maximalen Lautstärkebeschränkungen werden eingehalten.
- Trailing-Stops tragen dazu bei, schwankende Gewinne vor einer vollständigen Umkehrung zu schützen.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Kerzenabonnement, das für Indikatorberechnungen verwendet wird. | 15-minütiger Zeitrahmen |
| `BaseVolume` | Erstbestellungsvolumen beim ersten Signal. | `0.1` |
| `TakeProfitPips` | Pip-Abstand zwischen jedem Einstiegspreis und seinem Take-Profit-Ziel. | `50` |
| `TrailingStopPips` | Pip-Abstand, der für die Aktivierung und Verfolgung von Trailing-Stops pro Bein verwendet wird. | `20` |
| `MaxOrders` | Maximale Anzahl gleichzeitiger Mittelwertbildungsabschnitte (einschließlich der Ersteingabe). | `7` |
| `StepPips` | Mindestens erforderliche Gegenbewegung in Pips, bevor eine weitere Durchschnittsorder hinzugefügt werden kann. | `7` |
| `KPeriod` | Lookback-Länge für die stochastische %K-Linie. | `5` |
| `DPeriod` | Glättungslänge für die stochastische %D-Linie. | `3` |
| `Slowing` | Zusätzliche Glättung auf die %K-Berechnung angewendet. | `3` |
| `ZoneBuy` | Obere Grenze, die lange Setups ermöglicht, wenn %K über %D liegt. | `30` |
| `ZoneSell` | Untere Grenze, die kurze Setups ermöglicht, wenn %K unter %D liegt. | `70` |

## Notizen
- Die Strategie verwendet das übergeordnete StockSharp API mit Kerzenabonnements und Indikatorbindungen, wobei die Implementierung nah an der ursprünglichen MetaTrader-Logik bleibt und gleichzeitig die Risiko- und Visualisierungstools von StockSharp nutzt.
- Da Durchschnittstransaktionen das Volumen verdoppeln, stellen Sie sicher, dass das maximal zulässige Volumen des Instruments die Martingalleiter aufnehmen kann.
- Wie bei jedem Martingale-System werden vor der Bereitstellung auf einem Live-Konto dringend eine ordnungsgemäße Kapitalverwaltung und zusätzliche Risikobeschränkungen empfohlen.
