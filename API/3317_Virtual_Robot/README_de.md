# Virtual-Robot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Virtual-Robot-Strategie stellt den gridbasierten Averaging-Ansatz des ursprünglichen MetaTrader-Expert-Advisors nach. Der Algorithmus führt zwei unabhängige virtuelle Grids (Long und Short) auf einem konfigurierbaren Kerzenzeitrahmen. Erst wenn die Anzahl virtueller Levels die definierte Schwelle erreicht, werden echte Marktorders gesendet. So kann die Strategie das MT4-Verhalten simulieren, bei dem virtuelle Levels die tatsächliche Positionsverwaltung steuern.

## Handelslogik

1. **Erstellung der virtuellen Leiter:** Auf jeder abgeschlossenen Kerze vergleicht die Strategie den Schlusskurs mit dem Eröffnungskurs.
   - Wenn die Kerze höher schließt als sie eröffnet hat, wird ein neues virtuelles Long-Level angehängt, sobald die Entfernung vom vorherigen Long-Level den Pip-Schritt überschreitet.
   - Wenn die Kerze tiefer schließt, wird dieselbe Logik auf die virtuelle Short-Leiter angewendet.
   - Die ersten `VirtualStepper` virtuellen Trades verwenden das Basislot, spätere Levels skalieren die Größe mit `Multiplier`.
2. **Beförderung zu echten Orders:** Nachdem mindestens `StartingRealOrders` virtuelle Levels für eine Seite bestehen (oder ein bestehender Basket um mindestens einen Pip-Schritt ins Minus läuft), eröffnet die Strategie eine echte Marktorder mit Volumen, das über den Martingal-Multiplikator berechnet wird (`Multiplier * distance / PipStep`).
3. **Basket-Verwaltung:** Die Strategie verfolgt:
   - Den letzten Ausführungspreis und das Volumen jeder Seite.
   - Den gewichteten Durchschnitt des offenen Baskets (real oder virtuell, abhängig von `RealAverageThreshold`).
4. **Take-Profit-Logik:** Positionen werden geschlossen, wenn eine der folgenden Bedingungen erfüllt ist:
   - Der Preis bewegt sich um `MinTakeProfitPips` von der allerersten virtuellen Order weg (Take-Profit für ein einzelnes Level).
   - Der Preis kehrt zum gewichteten virtuellen Durchschnitt plus/minus `AverageTakeProfitPips` für mehrstufige Grids zurück.
   - Das berechnete Einzelorder- oder Durchschnitts-Take-Profit-Niveau (abgeleitet aus `TakeProfitPips` / `AverageTakeProfitPips`) wird erreicht.
5. **Stop-Loss-Logik:** Ein weicher Stop wird aus der letzten gefüllten Order über `StopLossPips` abgeleitet. Wenn der Preis das Schutzniveau kreuzt, wird der Basket liquidiert.
6. **Volumensicherheit:** Lotgrößen werden gegen die Security-Metadaten (`VolumeStep`, `MinVolume`, `MaxVolume`) normalisiert und durch `MaxVolume` gedeckelt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzenserie zur Bildung der virtuellen Leiter (Standard: 60-Minuten-Kerzen). |
| `StopLossPips` | Stop-Distanz in Pips von der letzten gefüllten Order. |
| `TakeProfitPips` | Take-Profit-Distanz für Einzelorder-Baskets. |
| `MinTakeProfitPips` | Mindestgewinn zum Schließen eines einzelnen virtuellen Levels. |
| `AverageTakeProfitPips` | Gewinnziel auf den gewichteten Durchschnitt des Baskets. |
| `BaseVolume` | Basislotgröße für die ersten Grid-Orders. |
| `MaxVolume` | Maximal erlaubte Lotgröße. |
| `Multiplier` | Lot-Multiplikator für gemittelte Einstiege. |
| `RealStepper` | Anzahl gefüllter realer Orders, bevor der Multiplikator greift. |
| `VirtualStepper` | Virtuelle Orders zum Basislot vor Skalierung. |
| `PipStepPips` | Minimale adverse Bewegung (in Pips) zwischen aufeinanderfolgenden Grid-Levels. |
| `MaxTrades` | Harte Obergrenze für reale Orders pro Seite. |
| `StartingRealOrders` | Anzahl virtueller Orders, die vor der ersten realen Order erforderlich ist. |
| `RealAverageThreshold` | Schaltet den Durchschnittspreis von virtuell auf real um, sobald so viele Orders gefüllt sind. |
| `VisualMode` | Für Parität mit der MT4-Eingabe beibehalten (keine Wirkung in StockSharp). |

## Implementierungshinweise

- Die Strategie verwendet Nettopositionen (StockSharp-Portfoliomodell) und kann daher keine gleichzeitig unabhängigen Long- und Short-Baskets wie im MT4-Hedging-Modus halten. Wenn beide virtuellen Leitern auslösen, dreht das jüngste Signal die Nettoposition.
- Chartzeichnung aus dem ursprünglichen EA wird absichtlich ausgelassen; alle virtuellen Levels werden intern gehalten.
- Preisschritte werden aus `Security.PriceStep` abgeleitet (mit 10x-Anpassung für Forex-Instrumente mit drei/fünf Stellen), um die MT4-Pip-Konvertierungslogik zu spiegeln.
- Schutzorders werden modelliert, indem Preise im Kerzenhandler überwacht und Marktausstiege gesendet werden, statt brokerseitige Stop-/Limit-Orders anzuhängen.

## Nutzungstipps

1. Stellen Sie sicher, dass Instrument-Metadaten (`PriceStep`, `VolumeStep`, `MinVolume`, `MaxVolume`) gefüllt sind, damit Pip-Konvertierung und Lot-Normalisierung den Brokerregeln entsprechen.
2. Starten Sie in Simulation oder mit kleinem Volumen, um zu prüfen, ob Grid-Abstände und Multiplikatoren zum geplanten Broker passen.
3. Passen Sie `StartingRealOrders` und `RealStepper` an, um die Aggressivität der Martingal-Skalierung zu steuern.
