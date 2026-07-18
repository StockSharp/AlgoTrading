# Straddle Trailing-Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Straddle Trailing-Manager-Strategie** repliziert das Verhalten des ursprünglichen MetaTrader 5 "Straddle&Trail" Expert Advisors. Die Strategie platziert ein Paar von Stop-Orders (einen Straddle) um den aktuellen Preis herum vor geplanten Nachrichtenereignissen oder sofort auf Anfrage. Sobald eine Position ausgelöst wird, verwaltet der Algorithmus Break-Even-Übergänge, Trailing Stops und optionale Shutdown-Befehle, die ausstehende Orders stornieren oder offene Positionen schließen.

Diese Implementierung basiert auf der StockSharp High-Level-API. Orderplatzierung, Positionsmanagement und Risikokontrollen sind implementiert ohne Low-Level-Nachrichtenverarbeitung.

## Handelslogik

1. **Straddle-Platzierung**
   * Zwei Stop-Orders (Buy Stop oberhalb und Sell Stop unterhalb) werden erstellt, sobald das geplante Ereignisfenster erreicht ist oder sofort, wenn `PlaceStraddleImmediately` aktiviert ist.
   * Die Orderpreise werden vom aktuellen Bid/Ask um `DistanceFromPrice` (in Pips ausgedrückt) versetzt. Der Versatz wird mithilfe des Instrument-Preisschritts in Preiseinheiten umgerechnet.
   * Die Strategie verhindert das erneute Erstellen des Straddles mehrmals am selben Tag, es sei denn, die Orders werden angepasst oder explizit storniert.

2. **Pre-Event-Order-Management**
   * Wenn `AdjustPendingOrders` aktiviert ist, werden die Stop-Orders jede neue Minute storniert und neu platziert, damit sie mit dem aktuellen Preis ausgerichtet bleiben.
   * Anpassungen stoppen `StopAdjustMinutes` vor dem Ereignis, um zu vermeiden, den Preis zu jagen, wenn die Volatilität steigt.
   * Wenn `RemoveOppositeOrder` aktiviert ist, wird die verbleibende Stop-Order automatisch storniert, sobald eine Seite des Straddles ausgelöst wird und eine Position öffnet.

3. **Risikomanagement**
   * Initiale Stop-Loss- und Take-Profit-Levels werden aus `StopLossPips` und `TakeProfitPips` berechnet und intern verfolgt.
   * Wenn der offene Gewinn `BreakevenTriggerPips` erreicht, wird das Stop-Level auf den Einstiegspreis plus `BreakevenLockPips` verschoben (oder den symmetrischen Wert für Short-Trades).
   * Wenn `TrailPips` größer als null ist, folgt ein Trailing Stop dem Preis. Trailing kann sofort starten oder erst nach der Break-Even-Bedingung, abhängig von `TrailAfterBreakeven`.
   * Gewinnmitnahme- und Stop-Ausstiege werden mit Marktorders für Zuverlässigkeit ausgeführt.

4. **Manuelles Herunterfahren**
   * Das Setzen von `ShutdownNow` auf `true` löst eine sofortige Bereinigung gemäß der `ShutdownMode`-Option aus. Mögliche Aktionen umfassen das Schließen von Long-/Short-Positionen und das Stornieren ausstehender Long-/Short-Orders.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `ShutdownNow` | Löst das Shutdown-Verfahren beim nächsten Kerzen-Update aus. Setzt sich nach der Ausführung automatisch auf `false` zurück. |
| `ShutdownMode` | Definiert, was storniert oder geschlossen werden soll (`All`, `LongPositions`, `ShortPositions`, `PendingLong`, `PendingShort`). |
| `DistanceFromPrice` | Abstand zwischen dem aktuellen Preis und jeder Stop-Order, gemessen in Pips. |
| `StopLossPips` | Initialer Stop-Loss-Abstand für ausgelöste Positionen. Auf `0` setzen, um zu deaktivieren. |
| `TakeProfitPips` | Initialer Take-Profit-Abstand. Auf `0` setzen, um zu deaktivieren. |
| `TrailPips` | Trailing-Stop-Abstand. Auf `0` setzen, um Trailing zu deaktivieren. |
| `TrailAfterBreakeven` | Wenn `true`, beginnt Trailing erst, nachdem die Break-Even-Bedingung erfüllt ist. |
| `BreakevenLockPips` | Gesicherter Gewinn, wenn der Break-Even-Trigger aktiviert wird. |
| `BreakevenTriggerPips` | Gewinnschwelle, die die Break-Even-Logik aktiviert. |
| `EventHour` / `EventMinute` | Geplante Ereigniszeit (Broker/Server-Zeit). Beide auf `0` setzen, um den Ereignis-Scheduler zu deaktivieren. |
| `PreEventEntryMinutes` | Minuten vor dem Ereignis, wenn der Straddle platziert werden soll. Wird ignoriert, wenn das Ereignis deaktiviert ist oder sofortige Platzierung aktiviert ist. |
| `StopAdjustMinutes` | Anzahl der Minuten vor dem Ereignis, wenn die automatische Anpassung ausstehender Orders stoppt. |
| `RemoveOppositeOrder` | Storniert die unerfüllte Stop-Order, wenn die erste Seite des Straddles ausgelöst wird. |
| `AdjustPendingOrders` | Aktiviert automatisches Neu-Zentrieren ausstehender Orders während des Wartens auf das Ereignis. |
| `PlaceStraddleImmediately` | Platziert den Straddle direkt nach dem Strategiestart, unter Umgehung des Ereignis-Zeitplans. |
| `CandleType` | Kerzen-Abonnement für die Zeitverfolgung. Standardmäßig 1-Minuten-Kerzen. |

> **Volumen** – die StockSharp-Eigenschaft `Volume` steuert die Ordergröße. Sie ist standardmäßig auf `1` gesetzt und kann vor dem Start der Strategie geändert werden.

## Datenabonnements

Die Strategie abonniert:

* Die konfigurierte Kerzenserie (Standard 1 Minute) zum Ausführen des Schedulers, der Trailing-Logik und der Shutdown-Prüfungen.
* Das Orderbuch, um die aktuellen Bid/Ask-Preise für präzise Stop-Order-Ausrichtung zu verfolgen.

## Hinweise und Einschränkungen

* Stop-Loss- und Take-Profit-Management wird über Marktorders ausgeführt, anstatt broker-seitige Schutzorders zu modifizieren. Dies spiegelt das ursprüngliche Verhalten wider, während die Implementierung einfach bleibt.
* Die Strategie verwendet den Instrument-`PriceStep` zur Annäherung der Pip-Größe. Für exotische Instrumente passen Sie die Parameter entsprechend an.
* Der Shutdown-Befehl wird nur ausgewertet, wenn neue Kerzendaten eintreffen. Für sofortige Aktion reduzieren Sie den Kerzen-Zeitrahmen.
* Die Python-Implementierung wird absichtlich ausgelassen, wie angefordert.

## Konversionshinweise

* Die Break-Even- und Trailing-Logik ist Zeile für Zeile aus der MQL-Version portiert. Die StockSharp-Version behält die gleichen numerischen Beziehungen bei, arbeitet aber mit Dezimalpreisen und verwendet Marktausstiege.
* Die manuelle Trade-Behandlung (Magic Number `0` in MQL) wird nicht reproduziert, da StockSharp-Strategien ihre eigenen Positionen verwalten. Die gesamte Schutzlogik gilt nur für strategiegenerierte Trades.
* Die `CalcMagic`-Funktion ist in StockSharp unnötig und wurde daher entfernt. Der Strategie-Status wird intern durch das Framework verfolgt.

