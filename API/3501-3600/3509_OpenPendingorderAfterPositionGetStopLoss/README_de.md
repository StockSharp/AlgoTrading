# Strategie OpenPendingorderAfterPositionGetStopLoss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **OpenPendingorderAfterPositionGetStopLoss**-Strategie portiert die MetaTrader 5 gleichnamigen Expertenberater in die StockSharp-Hochebene API. Es wertet kontinuierlich die Steigung der Stochastic %K-Linie im ausgewählten Zeitrahmen aus. Wenn %K nach unten geht, wird eine Verkaufsstopp-Order unter dem Markt platziert, und wenn %K steigt, wird eine Kauf-Stopp-Order über dem Markt platziert. Jeder ausgefüllte Eintrag erhält sofort eine schützende Stop-Loss- und Take-Profit-Order. Wenn ein Stop-Loss die Position schließt, installiert die Strategie automatisch die entsprechende ausstehende Order neu, sodass das Raster der Breakout-Trades wiederhergestellt wird, ohne auf die nächste Kerze warten zu müssen.

## Handelsregeln
- Abonnieren Sie fertige Kerzen des konfigurierten Zeitrahmens und berechnen Sie einen klassischen Stochastic-Oszillator (`KPeriod`, `DPeriod`, `Slowing`).
- Vergleichen Sie den aktuellen %K-Wert mit dem Wert vor zwei Balken:
  - `%K(current) < %K(two bars ago)` → Erteilen Sie einen Verkaufsstopp unterhalb des besten Gebots.
  - `%K(current) > %K(two bars ago)` → Setzen Sie einen Kaufstopp über dem besten Briefkurs.
- Ausstehende Aufträge werden vom Markt um den aktuellen Spread plus den benutzerdefinierten `MinStopDistancePoints`-Puffer ausgeglichen, der der ursprünglichen MQL-Logik entspricht.
- Sobald eine ausstehende Order ausgeführt wird, sendet die Strategie einen schützenden Stop-Loss (Stop-Order) und einen optionalen Take-Profit (Limit-Order).
- Wenn der schützende Stop-Loss ausgelöst wird, wird die entsprechende ausstehende Order sofort unter Verwendung der neuesten Marktpreise neu erstellt.
- Schutzaufträge werden automatisch gelöscht, wenn die Position durch den Take-Profit geschlossen wird oder wenn die Strategie stoppt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `OrderVolume` | Handelsvolumen in Lots für jede ausstehende Order. |
| `StopLossPoints` | Stop-Loss-Distanz in Symbolpunkten. Zum Deaktivieren auf 0 setzen. |
| `TakeProfitPoints` | Take-Profit-Distanz in Symbolpunkten. Zum Deaktivieren auf 0 setzen. |
| `MinStopDistancePoints` | Minimaler Preispuffer (in Punkten), der dem Spread hinzugefügt wird, bevor eine ausstehende Order platziert wird. |
| `MaxPositions` | Maximale Anzahl gleichzeitiger Positionen pro Richtung (Netting-Konten verwenden effektiv 0 oder 1). |
| `KPeriod` | Anzahl der Balken, die für die %K-Berechnung verwendet werden. |
| `DPeriod` | Länge der Glättungslinie %D. |
| `Slowing` | Zusätzlicher Glättungsfaktor, der vor dem Vergleich auf %K angewendet wird. |
| `PendingExpiry` | Optionale Lebensdauer ausstehender Stop-Orders. Abgelaufene Aufträge werden bei der nächsten Kerze storniert. |
| `CandleType` | Zeitrahmen für Kerzenabonnements und Indikatorberechnungen. |

## Hinweise zur Implementierung
- Die gesamte Auftragsverwaltung basiert auf hochrangigen Hilfsprogrammen wie `BuyStop`, `SellStop`, `SellLimit` und `BuyLimit`, wie von `AGENTS.md` gefordert.
- Indikatorwerte werden direkt im `SubscribeCandles().BindEx(...)`-Rückruf verbraucht, wodurch jegliche `GetValue`-Aufrufe vermieden werden.
- Die Strategie überwacht `MyTrade`-Ereignisse, um Schutzanordnungen zu installieren und zu entfernen, und emuliert dabei die `OnTradeTransaction`-Logik des ursprünglichen Expert Advisors.
- Kommentare im Code werden in Englisch verfasst und die Einrückung erfolgt mit Tabulatoren, entsprechend den Repository-Richtlinien.
