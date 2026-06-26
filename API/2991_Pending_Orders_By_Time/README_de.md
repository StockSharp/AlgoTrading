# Strategie für Pending-Orders nach Zeit 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert das Verhalten des ursprünglichen MetaTrader-Experten "Pending orders by time 2", indem Ausbruchs-Einstiegsorders rund um eine konfigurierbare Öffnungsstunde geplant werden. Zu Beginn der Handelssitzung platziert der Algorithmus sowohl einen Buy-Stop über dem aktuellen Ask-Preis als auch einen Sell-Stop unter dem aktuellen Bid-Preis. Jeder ausstehende Einstieg trägt seine eigenen Stop-Loss- und Take-Profit-Levels, die in Instrument-Preisschritten ausgedrückt sind, und sobald ein Einstieg ausgeführt wird, verwaltet die Strategie die offene Position mit Trailing-Stop-Logik und sich gegenseitig ausschließenden Exit-Orders. Der Code ist für die High-Level-API von StockSharp konzipiert und verwendet Tabulatoren zur Einrückung gemäß den Projektrichtlinien.

## Handelssitzungsablauf
1. **Täglicher Reset** – Bei der ersten abgeschlossenen Kerze eines neuen Handelstages löscht die Strategie interne Flags, damit später in der Sitzung ein neues Paar ausstehender Orders ausgegeben werden kann.
2. **Platzierung zur Öffnungsstunde** – Wenn die Stunde der Kerze der konfigurierten Öffnungsstunde entspricht und für den aktuellen Tag noch keine Orders platziert wurden, berechnet die Strategie Ausbruchspreise relativ zum letzten besten Bid/Ask-Snapshot (fällt auf den Kerzenschluss zurück, wenn keine Kurse verfügbar sind) und gibt sowohl Buy-Stop als auch Sell-Stop-Orders auf.
3. **Intraday-Management** – Während die Sitzung aktiv ist, zieht die Logik den Schutz-Stop für jede offene Position nach, hält den entgegengesetzten ausstehenden Einstieg aktiv (ermöglicht eine potenzielle Umkehr) und wartet darauf, dass entweder der Trailing-Stop, der feste Take-Profit oder die entgegengesetzte Ausbruchs-Order die Position schließt.
4. **Bereinigung zur Schlussstunde** – Sobald die Kerzenstunde der konfigurierten Schlussstunde entspricht, storniert die Strategie alle noch aktiven ausstehenden Einstiegs-Orders und schließt die Nettoposition zum Marktpreis, um sicherzustellen, dass keine Trades über Nacht gehalten werden.

## Auftragsplatzierungsdetails
- **Distanz, Stop-Loss, Take-Profit** – Die Parameter `DistanceTicks`, `StopLossTicks` und `TakeProfitTicks` werden in Instrument-Preisschritten (`Security.PriceStep`) interpretiert. Der Buy-Stop-Preis ist `bestAsk + DistanceTicks * step`, sein Stop-Loss wird `StopLossTicks` unterhalb des Einstiegspreises platziert, und der Take-Profit liegt dieselbe Distanz darüber. Der Sell-Stop spiegelt diese Logik auf der Short-Seite.
- **Bid/Ask-Verarbeitung** – Die Strategie abonniert das Orderbuch und zeichnet kontinuierlich das beste Bid und Ask auf. Wenn das Orderbuch noch keinen Kurs geliefert hat, wird der Schlusskurs der fertigen Kerze als sicherer Fallback verwendet.
- **Auftragsreferenzen** – Referenzen zu den gesendeten ausstehenden Orders werden gespeichert, damit der Algorithmus diese stornieren oder neu registrieren kann, wenn sich die Sitzung ändert oder wenn die Schlussstunde ausgelöst wird.

## Positions- und Risikomanagement
- **Schutzorders** – Wenn eine ausstehende Einstiegs-Order ausgeführt wird (erkannt in `OnOwnTradeReceived`), registriert die Strategie sofort eine Schutz-Stop-Order und eine Take-Profit-Order mit dem ursprünglichen Positionsvolumen. Long-Positionen erhalten einen `SellStop` und `SellLimit`, während Short-Positionen einen `BuyStop` und `BuyLimit` erhalten. Nur ein Stop und ein Take-Profit bleiben zu einem bestimmten Zeitpunkt aktiv; das Ausgeben neuer Schutzorders storniert automatisch das vorherige Paar.
- **Trailing-Stop** – Trailing wird durch `TrailingStopTicks` (die tatsächliche Stop-Distanz) und `TrailingStepTicks` (minimaler Gewinn, der vor einer Anpassung erforderlich ist) gesteuert. Die Trailing-Logik wird ausgelöst, sobald der nicht realisierte Gewinn `TrailingStop + TrailingStep` übersteigt. Sie berechnet einen besseren Stop-Preis (lockert den aktuellen Stop nie), storniert die vorherige Schutz-Stop-Order und gibt eine neue auf dem engeren Level auf.
- **Schlussstunden-Ausstieg** – Wenn die Schlussstunde eintrifft, storniert die Strategie beide Schutzorders und sendet eine Marktorder in Höhe der absoluten Position, damit keine Exposition offen bleibt.

## Parameter
- `OpeningHour` – Stunde (0–23), wenn die ausstehenden Orders erstellt werden.
- `ClosingHour` – Stunde (0–23), wenn ausstehende Orders entfernt und Positionen geschlossen werden.
- `DistanceTicks` – Ausbruchsdistanz vom aktuellen Bid/Ask in Preisschritten ausgedrückt.
- `StopLossTicks` – Feste Schutzdistanz für den anfänglichen Stop.
- `TakeProfitTicks` – Feste Distanz für das Gewinnziel.
- `TrailingStopTicks` – Vom Trailing-Stop einmal aktiviert gehaltene Distanz.
- `TrailingStepTicks` – Minimaler zusätzlicher Gewinn, der erforderlich ist, bevor der Trailing-Stop wieder bewegt wird.
- `Volume` – Größe beider ausstehender Orders.
- `CandleType` – Zeitrahmen für die Sitzungsverfolgung und Signalauswertung (standardmäßig 15-Minuten-Zeitrahmen).

## Implementierungshinweise
- Verwendet die High-Level-`Strategy`-API von StockSharp mit `SubscribeCandles`- und `SubscribeOrderBook`-Bindungen; kein Low-Level-Indikatorzugriff ist erforderlich.
- `OnOwnTradeReceived` wird genutzt, um Schutzorders mit der ausgeführten Einstiegs-Order synchron zu halten und aufzuräumen, wenn Stop-Loss oder Take-Profit ausgeführt wird.
- Die Trailing-Logik vermeidet es absichtlich, `GetValue` des Indikators aufzurufen und verlässt sich nur auf die eingehende Kerze und den gespeicherten Zustand, was den Konvertierungsrichtlinien entspricht.
- Distanzen basieren auf Preisschritten, was die ursprüngliche pip-basierte Arithmetik aus der MQL-Implementierung widerspiegelt und instrumentunabhängig bleibt.
- Die Python-Implementierung wird gemäß den Aufgabenanforderungen absichtlich weggelassen; nur die C#-Version ist in diesem Ordner bereitgestellt.
