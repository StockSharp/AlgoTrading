# Straddle Trail v2.40 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Straddle Trail v2.40-Strategie** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „Straddle&Trail“ (Version 2.40). Der Algorithmus bereitet vor einem einschneidenden Ereignis ein symmetrisches Paar von Stop-Orders vor, verwaltet die ausgelöste Position automatisch mit Break-Even- und Trailing-Stop-Logik und kann auf manuelle Trades reagieren, die bereits auf dem Konto vorhanden sind.

## Kernworkflow

1. **Vorbereitung**
   - Die Strategie abonniert Orderbuchaktualisierungen, um den besten Geld-/Briefkurs zu verfolgen, und Minute Candles (konfigurierbar) für Planungsentscheidungen.
   - Pips werden anhand der Instrumenteneinstellungen berechnet, sodass alle in Pips definierten Distanzen ordnungsgemäß in Preise umgewandelt werden.
2. **Straddle-Platzierung**
   - Zur konfigurierten Vorlaufzeit vor dem Ereignis (`PreEventEntryMinutes`) oder sofort, wenn `PlaceStraddleImmediately` aktiviert ist, werden eine Buy-Stop- und eine Sell-Stop-Order bei `DistanceFromPrice` Pips über und unter dem Markt platziert.
   - Vor dem Ereignis können ausstehende Orders jede Minute neu zentriert werden, wenn `AdjustPendingOrders` aktiviert ist. Anpassungen werden `StopAdjustMinutes` vor dem Ereignis beendet.
3. **Auftragsverwaltung**
   - Sobald eine Seite ausgelöst wird, verhindert die optionale Entfernung der gegenüberliegenden ausstehenden Bestellung (`RemoveOppositeOrder`) eine Doppelbelichtung.
   - `ShutdownNow` zusammen mit `ShutdownOption` ermöglicht es, offene Positionen zu reduzieren und/oder ausstehende Aufträge bei Bedarf zu stornieren.
4. **Positionssicherung**
   - Anfängliche Stop-Loss- und Take-Profit-Level werden aus den Pip-basierten Parametern abgeleitet.
   - Wenn der Preis den Break-Even-Trigger erreicht, wird der Stop verschoben, um einen Gewinn von `BreakevenLockPips` zu sichern.
   - Das Trailing beginnt entweder sofort oder nach der Gewinnschwelle (abhängig von `TrailAfterBreakeven`).
   - Wenn `ManageManualTrades` wahr ist, werden alle von der Strategie erkannten manuellen Positionen mit denselben Regeln geschützt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `ShutdownNow` | Erzwingt die Ausführung der Shutdown-Logik beim nächsten Schließen der Kerze. |
| `ShutdownOption` | Wählt aus, was geschlossen werden soll: alles, nur ausgelöste Positionen, nur Long-Positionen, nur Short-Positionen, alle ausstehenden Orders, nur Kauf-Stopps oder nur Verkaufs-Stopps. |
| `DistanceFromPrice` | Abstand in Pips zwischen dem aktuellen Preis und den ausstehenden Stop-Orders. |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz in Pips. |
| `TakeProfitPips` | Anfängliche Take-Profit-Distanz in Pips. Auf 0 setzen, um die Take-Profit-Ebene zu deaktivieren. |
| `TrailPips` | Trailing-Stop-Distanz in Pips. Auf 0 setzen, um das Nachziehen zu deaktivieren. |
| `TrailAfterBreakeven` | Bei „true“ beginnt das Trailing erst, nachdem der Break-Even-Trigger erreicht wurde. |
| `BreakevenLockPips` | Der Gewinn (in Pips) wird gesichert, sobald der Break-Even-Trigger ausgelöst wird. |
| `BreakevenTriggerPips` | Gewinnschwelle (in Pips), die die Break-Even-Bewegung aktiviert. |
| `EventHour` / `EventMinute` | Geplante Nachrichtenereigniszeit (Brokerzeit). Setzen Sie beide auf 0, um den Zeitplan zu deaktivieren und den manuellen/sofortigen Modus zu verwenden. |
| `PreEventEntryMinutes` | Minuten vor der Veranstaltung, wenn der Straddle platziert wird. |
| `StopAdjustMinutes` | Minuten vor der Veranstaltung, wenn die Auftragsanpassungen eingestellt werden. Der Mindestwert beträgt 1 Minute. |
| `RemoveOppositeOrder` | Entfernt die entgegengesetzte ausstehende Order, nachdem eine Seite des Straddles gefüllt ist. |
| `AdjustPendingOrders` | Zentriert die ausstehenden Aufträge jede Minute neu, bis das Stopp-Anpassungsfenster erreicht ist. |
| `PlaceStraddleImmediately` | Platziert den Straddle, sobald die Strategie beginnt, und ignoriert dabei den Event-Zeitplan. |
| `ManageManualTrades` | Erweitert die Break-Even- und Trailing-Logik auf manuelle Positionen. |
| `CandleType` | Kerzenserie, die für die Timing- und Planungslogik verwendet wird (Standard ist ein Zeitrahmen von 1 Minute). |

## Nutzungshinweise

- Konfigurieren Sie über die Sicherheitseinstellungen immer die richtige Pip-Größe für das Instrument, damit Pip-basierte Abstände genau in Preise übersetzt werden.
- Die Strategie schließt Positionen mithilfe von Marktaufträgen, wenn eine Stop-Loss- oder Take-Profit-Bedingung erfüllt ist, was widerspiegelt, wie das ursprüngliche EA manuelle Stop-Anpassungen durchgeführt hat.
- Wenn `PlaceStraddleImmediately` deaktiviert und der Zeitplan aktiv ist, wird der Straddle nur einmal pro Handelstag platziert. Setzen Sie die Strategie zurück, um sich auf eine weitere Veranstaltung am selben Tag vorzubereiten.
- Die Abschaltkontrollen können als Notbremse verwendet werden, um die Gefährdung schnell einzudämmen und ausstehende Befehle in allen Szenarios zu entfernen.

## Konvertierungsdetails

- Alle Kommentare im Code wurden ins Englische übersetzt und zur Verdeutlichung um zusätzliche Erläuterungen erweitert.
- Hochrangige StockSharp API-Methoden (`BuyStop`, `SellStop`, `ClosePosition`) werden verwendet, um die Implementierung nahe an den Best Practices des Frameworks zu halten.
- Der Algorithmus vermeidet die direkte Suche nach Indikatoren und verlässt sich stattdessen auf die gebundenen Kerzen- und Orderbuchabonnements, wie in den Projektrichtlinien gefordert.
