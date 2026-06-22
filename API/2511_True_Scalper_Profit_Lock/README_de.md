# True Scalper Gewinnabsicherungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **True Scalper Gewinnabsicherungs-Strategie** ist ein StockSharp-Port des MetaTrader 5 Expert Advisors "True Scalper Profit Lock". Die Strategie konzentriert sich auf ultrakunfristigen Handel mit schnellen exponentiellen gleitenden Durchschnitten, einem Zwei-Perioden-RSI-Filter und einer Gewinnschutzroutine, die Stops auf Break-Even verschiebt. Zusätzliche "Abandon"-Logik zwingt die Strategie, Trades zu schließen, die das Ziel innerhalb einer vordefinierten Anzahl von Kerzen nicht erreichen.

Die Implementierung abonniert einen einzelnen Kerzenstrom und bewertet nur abgeschlossene Kerzen. Sie ist für Intraday-Scalping konzipiert, aber alle Parameter sind vollständig anpassbar, sodass sie auf andere Zeitrahmen oder Instrumente angepasst werden kann.

## Indikatoren und Daten
- **EMA (schnell)** – Standardlänge 3, dient als bullischer Trigger beim Kreuzen über die langsame EMA.
- **EMA (langsam)** – Standardlänge 7, definiert die kurzfristige Trendrichtung.
- **RSI** – Standardlänge 2 mit auswählbarem Entscheidungsmodus:
  - *Methode A* (standardmäßig deaktiviert) reagiert auf den RSI, der den Schwellenwert von der vorherigen Kerze kreuzt.
  - *Methode B* (standardmäßig aktiviert) verfolgt die RSI-Polarität relativ zum Schwellenwert.
- **Kerzen** – Standard-Zeitrahmen ist 1 Minute, konfigurierbar über den `CandleType`-Parameter.

## Einstiegslogik
1. Berechnen Sie die schnelle EMA, langsame EMA und RSI auf der neuesten abgeschlossenen Kerze.
2. Bewerten Sie den RSI-Zustand:
   - Methode A: RSI-Polarität nur setzen, wenn der Schwellenwert zwischen zwei aufeinanderfolgenden Kerzen gekreuzt wird.
   - Methode B: RSI-Polarität setzen, je nachdem ob der Wert über oder unter dem Schwellenwert liegt.
3. **Kauf-Setup** – ausgelöst, wenn die schnelle EMA mindestens einen Preisschritt über der langsamen EMA liegt *und* der RSI negative Polarität anzeigt. Wenn die Abandon-Logik eine Umkehr zu Long erzwungen hat, wird der Trade auch unabhängig von den aktuellen Signalen eröffnet.
4. **Verkauf-Setup** – ausgelöst, wenn die schnelle EMA mindestens einen Preisschritt unter der langsamen EMA liegt *und* der RSI positive Polarität anzeigt, oder wenn eine Abandon-Umkehr einen Short-Einstieg erzwingt.
5. Positionsumkehrungen werden durch Senden der Differenz, die benötigt wird, um die Nettoposition in einer einzelnen Marktorder zu wenden, behandelt.

## Ausstiegslogik
- **Stop Loss / Take Profit** – in Preisschritten konfiguriert (`StopLossPoints`, `TakeProfitPoints`) und sofort nach dem Einstieg angewendet.
- **Gewinnabsicherung** – wenn aktiviert, wird der Stop auf Break-Even plus einem Offset (`BreakEvenPoints`) verschoben, sobald der offene Trade den angegebenen Gewinn (`BreakEvenTriggerPoints`) angesammelt hat. Die Routine funktioniert sowohl für Long- als auch Short-Positionen und läuft nur einmal pro Trade.
- **Abandon-Logik** – verfolgt die Anzahl der abgeschlossenen Kerzen seit dem Einstieg:
  - *Methode A*: schließt den Trade nach `AbandonBars` Kerzen und setzt ein Flag, um bei der nächsten Gelegenheit eine Position in entgegengesetzter Richtung zu eröffnen.
  - *Methode B*: schließt die Position nach dem Timeout, lässt aber signalbasierte Richtungsauswahl unberührt.
  - Methode A hat Priorität, wenn beide Methoden aktiviert sind.
- Manuelle Ausstiege werden mit Marktorders (via `ClosePosition`) erteilt und setzen den Trade-Zustand automatisch zurück.

## Geldverwaltung
- Wenn `UseMoneyManagement` aktiviert ist, wird die Positionsgröße aus dem Portfolio-Saldo abgeleitet: `Ceiling(Balance * RiskPercent / 10000) / 10`.
- Das verwaltete Volumen ist auf die ursprünglichen MT5-Regeln begrenzt: Mindest-Fallback auf `InitialVolume`, Werte über 1 Lot aufgerundet, optionaler Mini-Konto-Multiplikator, Hartkappung bei 100 Lots.
- Wenn deaktiviert, verwendet die Strategie das feste `InitialVolume` für jede Order.

## Parameter
- `InitialVolume` – Basis-Lot-Größe, wenn Geldverwaltung deaktiviert ist.
- `TakeProfitPoints` / `StopLossPoints` – Distanz in `Security.PriceStep`-Einheiten.
- `FastPeriod`, `SlowPeriod`, `RsiLength`, `RsiThreshold` – Indikatorkonfiguration.
- `UseRsiMethodA`, `UseRsiMethodB` – RSI-Entscheidungslogik umschalten.
- `UseAbandonMethodA`, `UseAbandonMethodB`, `AbandonBars` – Timeout-Management konfigurieren.
- `UseMoneyManagement`, `RiskPercent`, `LiveTrading`, `IsMiniAccount` – Risikogrößenoptionen, die mit dem MT5-Expert-Advisor abgestimmt sind.
- `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenPoints` – Break-Even-Parameter.
- `MaxPositions` – für Kompatibilität mit der MQL-Version beibehalten (der StockSharp-Port verwaltet eine einzelne Nettoposition pro Instrument).
- `CandleType` – Zeitrahmen oder benutzerdefinierter Kerzentyp für die Signalerzeugung.

## Verwendungshinweise
- Hängen Sie die Strategie an ein einzelnes Instrument; der `GetWorkingSecurities`-Override abonniert automatisch den ausgewählten Kerzentyp.
- Gewinnabsicherungs- und Abandon-Funktionen hängen von abgeschlossenen Kerzen ab; intrabar Preisausschläge, die innerhalb derselben Kerze zurückkehren, werden ignoriert.
- Der ursprüngliche MT5-Parameter `Slippage` wurde im Quellcode nicht verwendet und ist daher nicht vorhanden.
- Passen Sie `Security.PriceStep` oder die schrittbasierten Parameter entsprechend dem gehandelten Instrument an, um die beabsichtigten Pip-Distanzen zu erhalten.

## Konvertierungsunterschiede
- StockSharp operiert auf Nettopositionen, sodass keine gleichzeitigen mehrfachen Positionen eröffnet werden, auch wenn `MaxPositions` größer als eins ist. Dies spiegelt das typische Netting-Verhalten des ursprünglichen Experts wider, wenn `maxTradesPerPair` gleich 1 ist.
- Das Ordermanagement verwendet `BuyMarket`-, `SellMarket`- und `ClosePosition`-Helper anstelle direkter Ticket-Manipulation.
- Indikatordaten werden über `Bind`-Callbacks geliefert, um manuellen Pufferzugriff zu vermeiden.

## Testempfehlungen
- Validieren Sie das Verhalten auf historischen Daten mit demselben Zeitrahmen, der im ursprünglichen EA verwendet wurde (1-Minuten-Kerzen).
- Optimieren Sie `TakeProfitPoints`, `StopLossPoints` und `BreakEvenTriggerPoints` für das Zielinstrument, da diese für Forex-Kurse abgestimmt wurden.
