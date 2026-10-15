# Universal Trailing Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **Universal Trailing Manager-Strategie** ist eine C#-Portierung des MetaTrader-Expertenberaters „Universal 1.64 (barabashkakvn's Edition)".
Sie automatisiert Handelsverwaltungsaufgaben für diskretionären oder halbautomatischen Handel: zeitgesteuerte Einstiege, gitterartige
ausstehende Orders, dynamisches Trailing für Market- und Pending-Orders, schnelles Gewinnscalping sowie Portfolio-Benachrichtigungen,
wenn das Kontokapital einen definierten Prozentsatz erreicht.

Die Strategie ist für jedes Instrument geeignet, das Kerzendaten liefert. Sie stützt sich nicht auf Indikatoren, sondern reagiert auf
Preisniveaus und Zeitfenster — ideal für manuelle Signalbestätigung oder die Integration in größere Handelsverwaltungsworkflows.

## Hauptfunktionen

- **Geplante Aktionen**: öffnet automatisch Marktpositionen oder platziert ausstehende Orders zu einer bestimmten Terminalzeit (Stunde/Minute).
- **Pending-Order-Grid**: verwaltet jeweils eine Buy-Limit-, Sell-Limit-, Buy-Stop- und Sell-Stop-Order mit unabhängigen Offsets, optionalem
  Trailing und automatischer Neuregistrierung, wenn sich der Preis zugunsten der ausstehenden Order bewegt.
- **Marktpositionsschutz**: wendet Stop-Loss-, Take-Profit- und Trailing-Logik auf die aktuelle aggregierte Position an, einschließlich der
  Option, auf unrealisierten Gewinn zu warten, bevor das Trailing beginnt.
- **Scalping-Ausstieg**: schließt bestehende Positionen, sobald der Preis eine feste Anzahl von Punkten vom durchschnittlichen Einstandspreis
  entfernt ist.
- **Portfolio-Alerts**: überwacht das Portfolio-Kapital und protokolliert Meldungen, wenn das Konto um den konfigurierten Prozentsatz
  wächst oder sinkt.
- **Positions-Gating**: unterstützt den Modus „Warten bis Position geschlossen" sowie ein konfigurierbares Limit für die Anzahl offener
  Positionen pro Richtung vor der Annahme neuer Einstiege oder Pending-Orders.

## Parameter

| Gruppe | Parameter | Beschreibung |
|--------|-----------|--------------|
| Allgemein | `TradeVolume` | Ordervolumen in Lots für Market- und Pending-Einstiege. |
| Allgemein | `WaitClose` | Wenn `true`, sind neue Orders nur zulässig, wenn die Anzahl offener Positionen in dieser Richtung unter `MaxMarketPositions` liegt. |
| Markt | `MaxMarketPositions` | Maximale Anzahl aktiver Positionen pro Richtung bei aktiviertem `WaitClose`. |
| Markt | `MarketTakeProfitPoints` | Take-Profit-Abstand (in Preispunkten) für offene Positionen. 0 zum Deaktivieren. |
| Markt | `MarketStopLossPoints` | Stop-Loss-Abstand (in Preispunkten) für offene Positionen. 0 zum Deaktivieren. |
| Markt | `MarketTrailingStopPoints` | Trailing-Stop-Abstand (in Preispunkten). 0 deaktiviert das Trailing. |
| Markt | `MarketTrailingStepPoints` | Mindestverbesserung (in Punkten), die erforderlich ist, bevor der Trailing-Stop verschoben wird. |
| Markt | `WaitForProfit` | Bei Aktivierung beginnt das Trailing erst, nachdem der Gewinn `MarketTrailingStopPoints` überschreitet. |
| Markt | `ScalpProfitPoints` | Gewinngrenzwert (in Punkten), der eine sofortige Positionsschließung auslöst. 0 deaktiviert das Scalping. |
| Pending | `AllowBuyLimit`, `AllowSellLimit`, `AllowBuyStop`, `AllowSellStop` | Hauptschalter für jeden Pending-Order-Typ. |
| Pending | `LimitOrderOffsetPoints`, `StopOrderOffsetPoints` | Abstand vom aktuellen Schlusskurs zum Platzieren der entsprechenden Limit-/Stop-Order. Muss über dem minimalen Stop-Abstand des Instruments liegen. |
| Pending | `LimitOrderTakeProfitPoints`, `StopOrderTakeProfitPoints` | Gewinnziel (Punkte) für neu eröffnete Positionen aus Pending-Orders. |
| Pending | `LimitOrderStopLossPoints`, `StopOrderStopLossPoints` | Schutz-Stop (Punkte) für neu eröffnete Positionen aus Pending-Orders. |
| Pending | `LimitOrderTrailingStopPoints`, `StopOrderTrailingStopPoints` | Trailing-Abstand für aktive Pending-Orders. Null deaktiviert das Trailing. |
| Pending | `LimitOrderTrailingStepPoints`, `StopOrderTrailingStepPoints` | Mindestverbesserung, bevor eine Pending-Order beim Trailing verschoben wird. |
| Zeit | `UseTime` | Aktiviert den geplanten Aktionsblock. |
| Zeit | `TimeHour`, `TimeMinute` | Terminalzeit, zu der der geplante Block ausgewertet wird. |
| Zeit | `TimeBuy`, `TimeSell` | Market-Kauf-/Verkaufspositionen zur geplanten Zeit öffnen. |
| Zeit | `TimeBuyLimit`, `TimeSellLimit`, `TimeBuyStop`, `TimeSellStop` | Die entsprechende Pending-Order zur geplanten Zeit unabhängig von den Hauptschaltern platzieren. |
| Global | `UseGlobalLevels` | Aktiviert das Portfolio-Monitoring. |
| Global | `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Kapital-Prozentwerte, die informative Protokollmeldungen auslösen. |
| Daten | `CandleType` | Kerzentyp für die periodische Verarbeitung (Standard: 1 Minute). |

## Ausführungsablauf

1. **Kerzeneingang**: Bei jeder abgeschlossenen Kerze aktualisiert die Strategie Order-Referenzen, synchronisiert geplante Signale und
   wertet die Handelslogik aus.
2. **Zeitfenster**: Wenn der Kerzenschluss mit dem konfigurierten Zeitfenster übereinstimmt, werden die entsprechenden Booleans (`TimeBuy`
   usw.) gesetzt und Market-/Pending-Orders sofort registriert.
3. **Pending-Orders**: Die Strategie platziert eine Pending-Order pro Typ. Wenn die Preisbewegung die Trailing-Regeln erfüllt, wird die
   Order storniert und mit erhaltenem Offset näher am Markt neu ausgegeben.
4. **Marktschutz**: Für offene Positionen pflegt die Strategie dedizierte Stop-Loss- und Take-Profit-Orders und passt sie gemäß der
   Trailing-Konfiguration an, um sicherzustellen, dass die Volumina mit der aggregierten Position übereinstimmen.
5. **Scalping-Prüfung**: Wenn `ScalpProfitPoints` positiv ist, wird die Position geschlossen, sobald der aktuelle Schlusskurs das Ziel-Delta
   vom durchschnittlichen Positionspreis erreicht.
6. **Globale Alerts**: Das Portfolio-Kapital wird in jedem Zyklus überprüft; informative Meldungen werden protokolliert, sobald Schwellen
   erreicht werden.

## Verwendungshinweise

- Platzieren Sie die Strategie in einem Handelschema, in dem Kerzen kontinuierlich geliefert werden (z. B. 1-Minuten-Kerzen). Die Logik ist
  kerzengesteuert, sodass ein feinerer Zeitrahmen ein reaktionsfähigeres Trailing erzeugt.
- Die Strategie verwendet die aggregierte `Position`-Eigenschaft. Bei der Umkehr von Short zu Long (oder umgekehrt) wird die ausgeführte
  Ordergröße automatisch erhöht, um die bestehende Position zu schließen, bevor die neue eröffnet wird.
- Pending-Order-Offsets und Trailing-Schritte werden in *Preispunkten* (Vielfache von `Security.PriceStep`) gemessen. Stellen Sie sicher,
  dass der Schrittwert des Instruments korrekt konfiguriert ist; andernfalls fällt die Strategie auf eine Schrittgröße von 1 zurück.
- Das globale Gewinn-/Verlust-Monitoring liefert nur informative Protokollmeldungen. Es schließt keine Positionen automatisch — dies spiegelt
  das Verhalten des originalen Expertenberaters wider.
- Wenn `WaitClose` aktiviert ist, wird die Anzahl der offenen Positionen pro Seite aus der aggregierten Position geteilt durch `TradeVolume`
  abgeleitet. Verwenden Sie konsistente Volumensgrößen für genaues Gating-Verhalten.

## Protokollierung

Jede bedeutende Aktion — Order-Platzierung, Trailing-Anpassungen und globale Level-Alerts — wird über `LogInfo` in das Strategieprotokoll
geschrieben. Überwachen Sie das Protokoll, um den Entscheidungsprozess zu verfolgen, insbesondere beim Abstimmen von Offsets und
Trailing-Parametern.
