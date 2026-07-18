# Fly-System-Scalp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Fly-System-Scalp-Strategie ist ein hochfrequentes Ausbruchssystem, das das Kernverhalten des ursprünglichen MQL4-Expert-Advisors *FlySystemEA* reproduziert. Die Strategie überwacht ständig die besten Bid/Ask-Quotes und platziert zwei symmetrische Stop-Orders um den Marktpreis. Ziel ist es, schnelle Mikrotrends nach kurzfristigen Konsolidierungen zu erfassen und Spread, Kommissionen sowie Handelssitzungsgrenzen streng zu kontrollieren.

Die Konvertierung konzentriert sich auf folgende Mechaniken:

* Automatische Platzierung von Buy-Stop- und Sell-Stop-Orders in konfigurierbarer Entfernung vom Markt.
* Automatische Stornierung von Pending Orders, wenn der Spread (inklusive Kommission) die zulässige Schwelle überschreitet oder der Handel außerhalb der erlaubten Sitzung liegt.
* Optionales Take-Profit- und obligatorisches Stop-Loss-Management direkt an neuen Pending Orders.
* Unterstützung für manuelles Festvolumen und automatische risikobasierte Positionsgröße anhand von Broker-Kontraktspezifikationen (Preisschritt, Schrittwert, Lotschritt, Min-/Max-Volumen).
* Selbstzurücksetzender Handelszyklus, der auf Positionsschließungen wartet, bevor ein neues Paar Stop-Orders scharfgestellt wird.

Die StockSharp-Implementierung nutzt die High-Level-API (Level-1-Abonnement mit Bind) und folgt den erforderlichen Projektkonventionen: Strategieparameter werden über `StrategyParam` bereitgestellt, Kommentare sind auf Englisch, und der Namespace verwendet die file-scoped-Deklaration.

## Handelslogik
1. **Level-1-Feed:** Die Strategie abonniert Level-1-Daten für die zugewiesene Security. Jede Aktualisierung speichert das jüngste Bid/Ask-Paar.
2. **Validierungsschicht:** Vor jeder Handelsaktion prüft die Engine:
   * Die Strategie ist online und darf handeln.
   * Die aktuelle Zeit liegt innerhalb des optionalen Handelsfensters.
   * Der Spread plus Kommission überschreitet `MaxSpread` Pips nicht.
3. **Pending-Order-Platzierung:** Wenn die Bedingungen erfüllt sind, keine Position offen ist und die Strategie für einen neuen Zyklus bereit ist, werden zwei Orders vorbereitet:
   * Buy Stop bei `Ask + PendingDistance * pip` mit schützendem Stop Loss und optionalem Take Profit.
   * Sell Stop bei `Bid - PendingDistance * pip` mit gespiegelten Schutzwerten.
   Orders werden neu registriert, wenn die Differenz zwischen gewünschtem und tatsächlichem Preis `ModifyThreshold` Pips erreicht.
4. **Orderverwaltung:** Öffnet sich eine Position, wird die entgegengesetzte Pending Order sofort storniert. Wird ein Handelszyklus durch Spread-/Zeitverletzungen unterbrochen, werden alle Pending Orders entfernt und die Strategie wartet auf gültige Bedingungen.
5. **Positionsgröße:** Wenn `AutoLotSize` aktiviert ist, wird das Volumen aus `RiskFactor` Prozent der Equity geteilt durch den Verlust pro Kontrakt bei der konfigurierten Stop-Distanz abgeleitet. Das Volumen wird auf den Broker-Lotschritt gerundet und an Min-/Max-Grenzen angepasst.
6. **Schutz:** `StartProtection()` wird aufgerufen, damit StockSharp die Position für Notfallliquidation überwacht, falls die Infrastruktur dies erfordert.

## Parameter
| Name | Beschreibung | Standard |
|------|-------------|---------|
| `PendingDistance` | Distanz in Pips zwischen Marktpreis und beiden Stop-Orders. | 4 |
| `StopLossDistance` | Stop-Loss-Distanz in Pips für neue Positionen. | 0.4 |
| `TakeProfitDistance` | Take-Profit-Distanz in Pips, wenn aktiviert. | 10 |
| `UseTakeProfit` | Aktiviert Take-Profit-Platzierung. | `false` |
| `MaxSpread` | Maximal erlaubter Spread (Pips); 0 deaktiviert den Filter. | 1 |
| `CommissionInPips` | Kommission (in Pips), die dem Spreadfilter hinzugefügt wird. | 0 |
| `AutoLotSize` | Aktiviert risikobasierte Positionsgröße. | `false` |
| `RiskFactor` | Equity-Prozentsatz für Positionsgröße bei aktivem Auto-Sizing. | 10 |
| `ManualVolume` | Festes Volumen, wenn Auto-Sizing deaktiviert ist. | 0.1 |
| `UseTimeFilter` | Aktiviert den Handelssitzungsfilter. | `false` |
| `TradeStartTime` | Sitzungsstartzeit (inklusive). | 00:00:00 |
| `TradeStopTime` | Sitzungsende (exklusiv). | 00:00:00 |
| `ModifyThreshold` | Preisdelta (Pips), das vor erneuter Registrierung einer Pending Order erforderlich ist. | 1 |

## Nutzungshinweise
* Stellen Sie sicher, dass das Zielinstrument `Step`, `PriceStep`, `StepPrice`, `LotStep`, `MinVolume` und `MaxVolume` bereitstellt, da automatisches Sizing auf diesen Werten beruht. Fehlen Daten, fällt die Strategie sauber auf `ManualVolume` zurück.
* Der Pip-Wert wird aus Dezimalpräzision und Preisschritt der Security geschätzt, passend zur ursprünglichen MQL-Logik (inklusive Sonderbehandlung für 3-/5-stellige Forex-Quotes).
* Wenn `TradeStartTime` gleich `TradeStopTime` ist und `UseTimeFilter` aktiviert ist, gilt die Sitzung als immer offen. Ist die Startzeit größer als die Stoppzeit, läuft die Sitzung über Mitternacht.
* Spread-Validierung addiert `CommissionInPips` zum aktuellen Spread und repliziert damit das Verhalten, bei dem die MQL-Version Spread und Kommission in einem Filter kombinierte.
* Die Strategie erstellt oder verwaltet keine Chartobjekte. Visualisierung kann extern ergänzt werden, indem Level-1-Daten an Charts gebunden werden.

## Unterschiede zum ursprünglichen EA
* Der Low-Level-Tick-Timer und GUI-Elemente der MQL-Version werden bewusst ausgelassen. Die StockSharp-Variante nutzt Level-1-Ereignisse und integriertes Logging.
* Die Orderänderungslogik ist vereinfacht: Wenn der Zielpreis um mehr als `ModifyThreshold` Pips abweicht, wird die Order neu registriert statt der mehrzweigigen Anpassungslogik des EA.
* Automatische Kommissionserkennung aus der Tradehistorie wird durch den statischen Parameter `CommissionInPips` ersetzt; der Risikofilter addiert diesen Wert dennoch vor dem Handel zum Spread.
* Die StockSharp-Version nutzt `StartProtection()` anstelle eigener Stop-Level-Überwachungsschleifen.

## Historische Tests
Die Strategie benötigt Level-1-Quote-Daten, um die Stop-Order-Auslöse-Logik zu reproduzieren. Für historische Simulationen liefern Sie Bid/Ask-Serien oder bauen synthetische Level-1-Daten aus Tickhistorie. Reine Candle-Feeds reichen nicht aus, weil Pending Stop Orders auf Spread-Änderungen reagieren müssen.
