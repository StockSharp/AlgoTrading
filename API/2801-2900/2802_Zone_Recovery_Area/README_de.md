# Zonen-Wiederherstellungsbereich Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Zonen-Wiederherstellungsbereich Strategie** ist eine direkte Konvertierung des MetaTrader Expert Advisors "Zone Recovery Area" (Paket `MQL/20266`). Sie recreiert die ursprüngliche Absicherungslogik auf dem StockSharp High-Level API und fügt eine erschöpfende Parametrisierung hinzu, damit das Verhalten ohne Codeänderungen angepasst werden kann. Die Strategie kombiniert einen Trendfilter mit einem alternierenden Kauf/Verkauf-Wiederherstellungsgitter: Sobald ein primärer Trade eröffnet wird, werden zusätzliche Positionen gestapelt, wenn der Preis die vordefinierte Zone verlässt oder wieder eintritt, wodurch ein abgesicherter Korb entsteht, der darauf abzielt, schwebende Drawdowns zu erholen.

Kerneigenschaften:
- Verwendet einen schnellen/langsamen einfachen gleitenden Durchschnittskrossing zusammen mit einem monatlichen MACD-Filter, um den Handelsvoreingenommenheit zu definieren.
- Implementiert die Zonenwiederherstellungstechnik: Der erste Trade etabliert einen Basispreis, und alternierende Absicherungsorders werden ausgelöst, wenn der Markt die Zonengrenze überquert oder zum Basisniveau zurückkehrt.
- Bietet geldbasierte, prozentbasierte und Trailing-Gewinnkontrollen, um den Korb zu verlassen, sobald ausreichend Gewinn gesichert ist.
- Erlaubt sowohl multiplikative (Martingale-Stil) als auch additive Positionsgrößen für jeden Wiederherstellungsschritt.

## Marktdaten & Indikatoren
- **Primäre Kerzen:** Benutzerdefinierter Zeitrahmen (Standard 30 Minuten) für Einstiege und Wiederherstellungsmanagement.
- **Monatliche Kerzen:** Bei Bedarf aus niedrigeren Zeitrahmen konstruiert; zur Berechnung der MACD (12/26/9)-Werte verwendet.
- **Indikatoren:**
  - Einfacher gleitender Durchschnitt (schnell und langsam) auf dem primären Zeitrahmen.
  - Moving Average Convergence Divergence mit Signallinie auf dem monatlichen Zeitrahmen.

## Handelslogik
1. **Trendvalidierung**
   - Warten bis beide SMAs und der monatliche MACD vollständig geformt sind.
   - Ein bullisches Setup erfordert, dass der schnelle SMA unter dem langsamen SMA der vorherigen Bar liegt, während die monatliche MACD-Linie über ihrem Signal liegt.
   - Ein bärisches Setup erfordert, dass der schnelle SMA über dem langsamen SMA der vorherigen Bar liegt, während die monatliche MACD-Linie unter ihrem Signal liegt.
2. **Zyklusinitialisierung**
   - Wenn ein bullisches (bärisches) Setup erkannt wird, die anfängliche Long-(Short-)Position mit `InitialVolume` eröffnen und den Einstiegspreis als Zyklusbasis speichern.
   - Interne Zähler und Gewinnverfolgung für den neuen Zyklus zurücksetzen.
3. **Zonenwiederherstellungsmotor**
   - Zwei kritische Niveaus definieren: die **Zonengrenze** (`ZoneRecoveryPips`) vom Basispreis entfernt und das **Take-Profit-Niveau** (`TakeProfitPips`) in der günstigen Richtung.
   - Während der Zyklus aktiv ist, jede abgeschlossene Kerze überwachen:
     - Wenn der Preis das Take-Profit-Niveau erreicht, alle Nettoexposition schließen und den Zyklus beenden.
     - Wenn geld- oder prozentbasierte Gewinnziele erfüllt sind oder die Trailing-Gewinnsperre ausgelöst wird, den Zyklus schließen.
     - Andernfalls prüfen, ob eine neue Absicherung benötigt wird:
       - Für Long-Zyklen: einen zusätzlichen Short eröffnen, wenn der Preis unter `base - zone` fällt, und einen zusätzlichen Long, wenn der Preis wieder über den Basispreis steigt.
       - Für Short-Zyklen: einen zusätzlichen Long eröffnen, wenn der Preis über `base + zone` steigt, und einen zusätzlichen Short, wenn der Preis unter den Basispreis zurückkehrt.
     - Die Absicherungsrichtung wechselt automatisch; die nächste Ordergröße wird durch Multiplikation des vorherigen Volumens oder durch Hinzufügen eines festen Inkrements bestimmt.
   - Die Anzahl der Trades pro Korb ist durch `MaxTrades` begrenzt.
4. **Gewinnmanagement**
   - `UseMoneyTakeProfit`: Den Korb schließen, sobald der nicht realisierte Gewinn den konfigurierten Währungsbetrag erreicht.
   - `UsePercentTakeProfit`: Den Korb schließen, sobald der nicht realisierte Gewinn dem angegebenen Prozentsatz des Portfoliowertes entspricht.
   - `EnableTrailing`: Sobald der Gewinn `TrailingStartProfit` übersteigt, den Peak verfolgen und den Zyklus verlassen, wenn der Gewinn um `TrailingDrawdown` fällt.

Alle Orders werden mit StockSharp High-Level-Hilfsprogrammen (`BuyMarket`/`SellMarket`) platziert, was die Implementierung konsistent mit den Framework-Best-Practices hält.

## Parameter
| Name | Standard | Beschreibung |
| ---- | -------- | ------------ |
| `CandleType` | 30-Minuten-Kerzen | Zeitrahmen für Einstiege und Wiederherstellungsüberwachung. |
| `MonthlyCandleType` | 30-Tages-Kerzen | Höherer Zeitrahmen zur Erstellung des MACD-Trendfilters. |
| `FastMaLength` | 20 | Periode des schnellen SMA. |
| `SlowMaLength` | 200 | Periode des langsamen SMA. |
| `TakeProfitPips` | 150 | Abstand vom Basispreis zum Schließen des gesamten Korbs im Gewinn. |
| `ZoneRecoveryPips` | 50 | Halbbreite der Absicherungszone um den Basispreis. |
| `InitialVolume` | 1 | Volumen des ersten Trades in jedem Zyklus. |
| `UseVolumeMultiplier` | true | Wenn aktiviert, multipliziert jede neue Absicherung das vorherige Volumen. |
| `VolumeMultiplier` | 2 | Faktor, der auf das vorherige Volumen angewendet wird, wenn `UseVolumeMultiplier` true ist. |
| `VolumeIncrement` | 0.5 | Additiver Volumenzuwachs, wenn `UseVolumeMultiplier` false ist. |
| `MaxTrades` | 6 | Maximale Anzahl von Trades pro Wiederherstellungszyklus (einschließlich des anfänglichen). |
| `UseMoneyTakeProfit` | false | Geldbasiertes Take-Profit aktivieren. |
| `MoneyTakeProfit` | 40 | Gewinnziel in Kontowährung. |
| `UsePercentTakeProfit` | false | Prozentbasiertes Take-Profit aktivieren. |
| `PercentTakeProfit` | 5 | Gewinnziel als Prozentsatz des Portfoliowertes. |
| `EnableTrailing` | true | Trailing-Gewinnschutz aktivieren. |
| `TrailingStartProfit` | 40 | Gewinnschwelle, bevor Trailing aktiv wird. |
| `TrailingDrawdown` | 10 | Erlaubter Gewinnrückgang, sobald Trailing aktiv ist. |

> **Pip-Konvertierung:** `TakeProfitPips` und `ZoneRecoveryPips` werden in Preisoffsets unter Verwendung des Preis-Steps des Instruments konvertiert. Stellen Sie sicher, dass das gehandelte Instrument korrekte `PriceStep` und `StepPrice`-Werte bereitstellt.

## Verwendungshinweise
1. Strategie zur StockSharp-Lösung hinzufügen (Designer, API, Runner usw.).
2. Gewünschtes Wertpapier und Portfolio vor dem Start zuweisen.
3. Parameter an die Volatilität des Instruments, akzeptablen Drawdown und Kontogröße anpassen.
4. Ausreichende historische Daten sicherstellen, damit sowohl SMAs als auch der monatliche MACD vor dem ersten Trade aufgewärmt werden können.
5. Margenbenutzung sorgfältig überwachen: Wiederherstellungsschritte können die Exposition schnell erhöhen, besonders wenn der Multiplikator aktiviert ist.

## Risikomanagement & Überlegungen
- Zonenwiederherstellungs-/Martingale-Techniken können in Trendmärkten sehr große Positionen ansammeln. Immer mit konservativen Einstellungen testen und den `MaxTrades`-Parameter zur Risikobegrenzung verwenden.
- Da StockSharp eine einzige Nettoposition pflegt, repliziert die interne Gewinnberechnung den Korb-PnL unter Verwendung von Instrumentspreise/-Schritt-Informationen. Die Zahlen mit dem Broker-Datenfeed validieren.
- Geld- und Prozentziele hängen von der Portfoliobewertung ab. Beim Backtesting oder Paper-Trading sicherstellen, dass das Portfoliomodell `BeginValue`/`CurrentValue` korrekt liefert.
- Kein automatischer harter Stop-Loss wird verwendet; Risiko wird über die Wiederherstellungsmechaniken verwaltet. Die Strategie mit externen Portfolio-Level-Stops kombinieren.

## Dateien
- `CS/ZoneRecoveryAreaStrategy.cs` — Implementierung der Strategie.
- `README.md` — Englische Dokumentation (diese Datei).
- `README_ru.md` — Russische Dokumentation.
- `README_zh.md` — Chinesische Dokumentation.
