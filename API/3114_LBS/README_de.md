# LBS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **LBS-Strategie** ist eine direkte Konvertierung des MetaTrader 5-Expertenberaters "LBS (barabashkakvn's edition)". Das Originalsystem beobachtet Ausbrüche der vorherigen Kerze während eines konfigurierbaren Handelsfensters und platziert Stop-Orders an beiden Extrema. Der StockSharp-Port behält dieselben Trade-Management-Regeln bei und verwendet die High-Level-API (`SubscribeCandles`, `SubscribeLevel1`, `BuyStop`/`SellStop`) für Klarheit und Zuverlässigkeit.

## Handelslogik

1. Die Strategie überwacht abgeschlossene Kerzen des ausgewählten Zeitrahmens (`CandleType`).
2. Wenn die Schlusszeit der Kerze mit einer der aktivierten Handelsstunden (`Hour1`, `Hour2`, `Hour3`) übereinstimmt, berechnet der Algorithmus Ausbruchniveaus:
   - Der Buy-Stop wird beim Höchstwert aus Kerzenhoch und aktuellem Ask plus Freeze-Puffer platziert.
   - Der Sell-Stop wird beim Mindestwert aus Kerzentief und aktuellem Bid minus demselben Puffer platziert.
   - Der Puffer reproduziert den MetaTrader-`SYMBOL_TRADE_FREEZE_LEVEL`-Fallback (drei Spreads, aber nie weniger als zehn Pips).
3. Wenn eine Position eröffnet wird, wird die entgegengesetzte ausstehende Order sofort storniert, genau wie die `DeleteAllPendingOrders`-Routine des MQL-Experten.
4. Anfängliche Stop-Loss-Preise werden gemäß `StopLossPips` angehängt. Optionale Trailing-Logik (`TrailingStopPips` und `TrailingStepPips`) verschiebt den Stop, sobald der schwebende Gewinn die konfigurierten Schwellenwerte übersteigt.
5. Orders werden nur gesendet, wenn die Strategie online ist, keine Position offen ist und gültige Level1-Kurse verfügbar sind.

## Geldmanagement

`MoneyMode` spiegelt den `Lot/Risk`-Schalter des ursprünglichen Experten:

- **FixedLot** – der Parameter `VolumeOrRisk` wird als absolutes Handelsvolumen interpretiert.
- **RiskPercent** – die Strategie konvertiert `VolumeOrRisk` in einen Anteil des Portfoliowerts. Der Risikobetrag wird durch den Abstand zwischen dem Einstiegspreis und dem Schutz-Stop (in Preisschritten) geteilt, um das Ordervolumen zu erhalten. Wenn dieser Modus verwendet wird, muss der Stop-Loss aktiviert sein; andernfalls wird die Order übersprungen.

Alle Volumina werden auf die Mindest-, Maximal- und Schrittbeschränkungen des Instruments normalisiert, um Broker-Ablehnungen zu vermeiden.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `StopLossPips` | 50 | Abstand zum festen Stop in Pips. Null deaktiviert sowohl den anfänglichen Stop als auch das Trailing-Modul. |
| `TrailingStopPips` | 5 | Trailing-Stop-Abstand in Pips. Null deaktiviert Trailing. |
| `TrailingStepPips` | 15 | Zusätzlicher Gewinn (in Pips) erforderlich bevor der Trailing-Stop bewegt wird. Muss positiv sein wenn Trailing aktiviert ist. |
| `MoneyMode` | `FixedLot` | Wählt zwischen festem Volumen und risikoprozentualem Sizing. |
| `VolumeOrRisk` | 1.0 | Lotgröße im `FixedLot`-Modus oder Risikoprozentsatz im `RiskPercent`-Modus. |
| `Hour1` | 10 | Erste Handelsstunde. Auf `0` setzen zum Deaktivieren. |
| `Hour2` | 11 | Zweite Handelsstunde. Auf `0` setzen zum Deaktivieren. |
| `Hour3` | 12 | Dritte Handelsstunde. Auf `0` setzen zum Deaktivieren. |
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzenserie für die Ausbrucherkennung; anpassen um den Chartrahmen aus MetaTrader zu spiegeln. |

## Hinweise

- Stundenvergleiche verwenden die Kerzenschlusszeit, die dem Moment entspricht, wenn MetaTrader's `TimeCurrent()` dem Beginn der nächsten Bar entspricht.
- Die Freeze/Stop-Level-Approximation garantiert, dass Stop-Orders nie näher als zehn Pips am aktuellen Bid/Ask liegen, wodurch die häufigsten MetaTrader-Fehler vermieden werden.
- Trailing-Stops werden bei jedem Level1-Tick aktualisiert, was ein Verhalten nahe dem tick-gesteuerten `OnTick`-Handler des ursprünglichen Experten sicherstellt.
- Risikobasiertes Sizing verwendet `Portfolio.CurrentValue` wenn verfügbar und fällt sonst auf `Portfolio.BeginValue` zurück.

## Verwendungshinweise

1. Hängen Sie die Strategie an ein Instrument und wählen Sie denselben Zeitrahmen wie in MetaTrader.
2. Konfigurieren Sie die Handelsstunden entsprechend der Session, die Sie handeln möchten (Setzen auf `0` deaktiviert diesen Slot).
3. Wählen Sie den `RiskPercent`-Modus für automatisches Scaling; stellen Sie sicher, dass `StopLossPips` positiv ist.
4. Für Fixed-Lot-Trading behalten Sie `MoneyMode` bei `FixedLot` und setzen Sie `VolumeOrRisk` auf die gewünschte Größe.
5. Starten Sie die Strategie. Sie platziert zwei ausstehende Orders bei der nächsten konfigurierten Stunde und pflegt den Schutz-Stop automatisch.
