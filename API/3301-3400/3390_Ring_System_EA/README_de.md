# Ringsystem EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den Multi-Währungs-Grid-Hedging-Experten „RingSystemEA“ von MetaTrader 4 auf die StockSharp hohe Ebene API. Es ordnet eine konfigurierbare Liste von Währungen in dreieckigen Ringen an (drei Währungen erzeugen drei korrelierte Paare) und verwaltet zwei abgesicherte Körbe pro Ring: einen **Plus**-Korb (Long/Short/Long) und einen **Minus**-Korb (Short/Long/Short). Die Strategie überwacht kontinuierlich den gleitenden Gewinn in jedem Ring, wendet eine stufenbasierte Verstärkung im Martingal-Stil an, wenn Verluste konfigurierte Schwellenwerte überschreiten, und koordiniert globale oder pro-seitige Ausstiege, wenn Gewinn- oder Verlustziele erreicht werden.

## Handelslogik

* Erstellen Sie alle eindeutigen Kombinationen von drei Währungen aus der geordneten `CurrenciesTrade`-Liste (z. B. EUR/GBP/AUD ergibt EURGBP, EURAUD und GBPAUD).
* Jeder Ring verwaltet zwei synchronisierte Körbe:
  * **Plus-Korb** öffnet KAUF für das erste Paar, VERKAUF für das zweite Paar, KAUF für das dritte Paar.
  * **Minus-Korb** öffnet die gespiegelte SELL/BUY/SELL-Sequenz.
* Körbe werden automatisch geöffnet, sobald der Ring über Preisdaten verfügt und der Sitzungsfilter den Handel zulässt. Abhängig von `SideOpenOrders` können beide Seiten gleichzeitig oder nur eine Seite laufen.
* Wenn ein aktiver Korb über den Schwellenwert von `StepOpenNextOrders` hinausgeht (optional geometrisch oder exponentiell skaliert), wird eine neue Ebene von Aufträgen unter Verwendung von Volumenprogressionsregeln (`LotOrdersProgress`) hinzugefügt.
* Körbe werden geschlossen, wenn ihr schwebender PnL den gewählten Ausstiegsmodus erfüllt:
  * `SingleTicket` schließt die Plus- und Minus-Körbe unabhängig voneinander.
  * `BasketTicket` schließt beide Körbe zusammen, sobald ihr kombinierter Gewinn das Ziel erreicht.
  * `PairByPair` schließt einzelne Paare, wenn ihr PnL das Ziel überschreitet.
* Schutzausgänge spiegeln die MT4-Logik wider. Abhängig von `TypeCloseInLoss` schließt die Strategie entweder ganze Körbe, halbiert das Risiko oder lässt die Körbe sich ohne erzwungene Ausstiege erholen.
* Der optionale Sitzungswächter repliziert das Verhalten „Warten nach Öffnung am Montag“ und „Stoppen vor Schließung am Freitag“.
* Die Parameter stimmen weitgehend mit dem Original EA überein. Bei der automatischen Lotgrößenbestimmung werden der aktuelle Portfoliowert und `RiskFactor` verwendet, während die Option „Fair Lot“ Tickwertunterschiede innerhalb eines Rings ausgleicht.

## Schlüsselparameter

| Parameter | Beschreibung |
| --- | --- |
| `CurrenciesTrade` | Geordnete Währungsliste, die definiert, wie Ringe generiert werden. |
| `NoOfGroupToSkip` | Durch Kommas getrennte Rufnummern, die ignoriert werden sollen. |
| `SideOpenOrders` | Wählen Sie Plusseite, Minusseite oder beides. |
| `OpenOrdersInLoss` + `StepOpenNextOrders` | Steuert, wann zusätzliche Bestellungen hinzugefügt werden, während Körbe verloren gehen. |
| `StepOrdersProgress` | Multiplikator, der für jede zusätzliche Schicht auf den Verlustschwellenwert angewendet wird. |
| `LotOrdersProgress` | Staffelungsregel für Volumina von Folgeaufträgen. |
| `TypeCloseInProfit` / `TargetCloseProfit` | Gewinnmitnahmelogik und Schwellenwerte. |
| `TypeCloseInLoss` / `TargetCloseLoss` | Schutzausgänge bei Verlust. |
| `AutoLotSize`, `RiskFactor`, `ManualLotSize`, `UseFairLotSize` | Optionen zur Geldverwaltung. |
| `ControlSession`, `WaitAfterOpen`, `StopBeforeClose` | Fensterschutz für den wöchentlichen Handel. |
| `MaxSpread`, `MaximumOrders`, `MaxSlippage` | Risikobeschränkungen. |

## Verhaltenshinweise

* Der StockSharp-Port behält den Status in verwalteten Strukturen und nicht in Roharrays bei, aber der Handelsfluss spiegelt den MT4-Experten wider: ausgeglichene Körbe öffnen, Korb-PnL überwachen, bei Drawdown-Schritten verstärken und bei Gewinn- oder Risikoereignissen schließen.
* Alle Indikatoren sind implizit; Die Strategie stützt sich bei der Entscheidungsfindung ausschließlich auf Preisabonnements und Konto-PnL.
* Bestellungen sind mit `StringOrdersEA` gekennzeichnet, damit externe Nachbearbeitungstools sie identifizieren können.
* Marktaufträge nutzen das Strategieportfolio; Schließen Sie die gewünschten Instrumente an, bevor Sie beginnen.

## Unterschiede zum Original EA

* Die Spread-Filterung wird vereinfacht: Der Port StockSharp validiert den konfigurierten `MaxSpread` anhand von Kerzendaten und nicht anhand von Tick-Snapshots.
* Im Auto-Step-Modus wird der manuelle Schrittwert wiederverwendet, da MetaTrader-spezifische Margenberechnungen in StockSharp nicht verfügbar sind.
* Die UI-Zeichnungs- und Dateiprotokollierungsfunktionen der MT4-Version entfallen. `SaveInformations` schreibt jetzt detaillierte Diagnosen in das Protokoll statt in das Diagramm.
* Bei der Positionsgröße wird der aktuelle Portfoliowert verwendet. Passen Sie `RiskFactor` an, um die Lautstärke zu kalibrieren.

## Nutzungstipps

1. Alle von `CurrenciesTrade` referenzierten Währungspaare verbinden und zuordnen. Präfix-/Suffix-Helfer unterstützen Broker-spezifische Symbole.
2. Legen Sie `SideOpenOrders` fest, um zu steuern, ob die Strategie beide Körbe beibehalten oder in eine einzige Richtung agieren soll.
3. Stimmen Sie `StepOpenNextOrders`, `StepOrdersProgress` und `LotOrdersProgress` sorgfältig ab; Diese Parameter prägen den Martingalverlauf und die Risikoexposition.
4. Sehen Sie sich die Protokollmeldungen an, wenn `SaveInformations` aktiviert ist, um zu verstehen, wie sich Ringe entwickeln und wann Körbe hinzugefügt oder geschlossen werden.

Dieser Port behält das Kernverhalten des abgesicherten Rasters des MT4-Experten bei und passt es gleichzeitig an die ereignisgesteuerte Architektur und das Parametersystem von StockSharp an.
