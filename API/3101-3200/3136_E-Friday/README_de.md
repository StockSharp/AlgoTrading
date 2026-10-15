# E-Friday-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertiert den ursprünglichen MetaTrader-Expertenberater `E-Friday.mq5` in die StockSharp High-Level-API.
- Handelt nur, wenn der Chart-Zeitrahmen **H1 oder kürzer** ist; andernfalls protokolliert die Strategie eine Warnung und bleibt flat.
- Einstieg in Positionen auf kontrarische Weise: eine bärische Kerze eröffnet eine Long-Position und eine bullische Kerze eröffnet eine Short-Position.
- Deaktiviert das Trading jeden Freitag vollständig, um das ursprüngliche Wochenendschutzverhalten zu übernehmen.
- Beschränkt das Trading auf ein konfigurierbares Zeitfenster und kann Positionen nach Sitzungsende zwangsschließen.

## Handelslogik
1. Bei jeder abgeschlossenen Kerze prüft die Strategie die aktuelle Börsenzeit:
   - wenn der Tag Freitag ist, wird jede Aktion übersprungen;
   - wenn die Stunde vor der konfigurierten Startstunde liegt, wird gewartet;
   - wenn das Schließfenster aktiviert ist und die Stunde über der Endstunde liegt, werden alle Positionen geschlossen und neue Eintritte übersprungen.
2. Wenn Handel erlaubt ist, steuert die letzte abgeschlossene Kerze das Signal:
   - wenn `Open > Close` (bärischer Körper) bereitet die Strategie einen Long-Einstieg vor;
   - wenn `Open < Close` (bullischer Körper) bereitet die Strategie einen Short-Einstieg vor;
   - gleiche Eröffnungs- und Schlusskurse heben alle ausstehenden Aktionen auf.
3. Vor dem Einstieg in eine neue Position wird die aktuelle Exposition geschlossen, sodass niemals mehr als eine Nettoposition besteht.

## Positionsmanagement
- **Lotgröße** – aus `TradeVolume` übernommen und an `BuyMarket` / `SellMarket`-Orders gesendet.
- **Stop-Loss und Take-Profit** – in Pips gemessen. Pips werden aus `Security.PriceStep` berechnet und mit `10` multipliziert, wenn das Instrument 3 oder 5 Dezimalstellen hat, genau wie in der MQL-Version.
- **Trailing-Stop** – aktiviert sich, sobald der Preis `TrailingStopPips + TrailingStepPips` zugunsten der Position wechselt. Der Stop wird auf `aktueller Preis - Trailing-Stop` (Long) oder `aktueller Preis + Trailing-Stop` (Short) nachgezogen.
- Ausstiege werden anhand von Kerzenextrema bewertet:
  - eine Long-Position schließt, wenn das Kerzentief den Stop berührt oder das Hoch das Take-Profit erreicht;
  - eine Short-Position schließt, wenn das Kerzenhoch den Stop berührt oder das Tief das Take-Profit erreicht.
- Nach der Sitzungsendezeit (wenn `UseCloseHour = true`) werden alle offenen Positionen über Marktorders geschlossen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen. Muss einen positiven `TimeSpan` definieren und sollte eine Stunde nicht überschreiten. |
| `TradeVolume` | Ordervolumen in Lots. Muss positiv sein. |
| `StopLossPips` | Abstand vom Eintrittspreis zum Schutz-Stop in Pips. Null zum Deaktivieren des anfänglichen Stops. |
| `TakeProfitPips` | Abstand vom Eintrittspreis zum Gewinnziel in Pips. Null zum Deaktivieren des Ziels. |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Funktioniert zusammen mit `TrailingStepPips`. |
| `TrailingStepPips` | Minimaler zusätzlicher Fortschritt (in Pips) bevor der Trailing-Stop nachgezogen wird. Muss positiv sein, wenn der Trailing-Stop aktiviert ist. |
| `StartHour` | Stunde (Börsenzeit), ab der die Strategie mit der Positionseröffnung beginnen darf. |
| `UseCloseHour` | Aktiviert oder deaktiviert das Zwangsschließen nach der Endstunde. |
| `EndHour` | Stunde (Börsenzeit), nach der die Strategie aufhört zu handeln und bestehende Positionen schließt. |

## Implementierungshinweise
- Verwendet `SubscribeCandles` und die High-Level-`Bind`-API, damit Indikatoren später bei Bedarf hinzugefügt werden können.
- Validiert die Trailing-Konfiguration beim Start: wenn ein Trailing-Stop angefordert wird, muss der Trailing-Schritt strikt positiv sein.
- Die Pip-Konvertierung spiegelt die ursprüngliche EA-Logik (`PriceStep * 10` für 3/5-stellige Symbole) wider, um Stop-Loss-Abstände konsistent zu halten.
- Die StockSharp-Version bewertet Stops und Ziele einmal pro abgeschlossener Kerze. Der ursprüngliche EA lief auf jedem Tick, daher kann der StockSharp-Port einige Ticks später aussteigen, aber die Logik bleibt gleichwertig.
- Die Strategie ruft `CloseActivePosition` explizit auf, wenn das Sitzungsfenster endet. Das MQL-Skript enthielt dieselbe Idee, kehrte jedoch zurück, bevor die Schließroutine erreicht wurde; die C#-Version implementiert das beabsichtigte Verhalten.
- Informative Protokolle (`AddInfoLog` / `AddWarningLog`) werden verwendet, um übersprungene Handelszeiträume in der Benutzeroberfläche anzuzeigen.
