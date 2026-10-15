# RSI Expert Trendfilter Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 5 Expert Advisors **RSI_Expert_v2.0** in StockSharp's High-Level-Strategie-API.
- Generiert Signale beim konfigurierten `CandleType` (Standard 1 Stunde) und führt Trades beim Kerzenschluss aus.
- Entwickelt für Nettopositionen: die Strategie hält eine einzige aggregierte Position anstatt mehrere Tickets abzusichern.

## Einstiegslogik
1. **RSI-Kreuzung** – ein Long-Setup erscheint, wenn der letzte RSI-Wert über `RsiLevelDown` steigt, während die vorherige abgeschlossene Kerze unter dem Level lag. Ein Short-Setup wird ausgelöst, wenn RSI nach oben über `RsiLevelUp` wieder unterschreitet.
2. **Gleitender-Durchschnitt-Filter** – der ursprüngliche Experte erlaubt das Handeln mit oder gegen eine Gleitender-Durchschnitt-Kreuzung. Der `MaMode`-Parameter reproduziert die Optionen:
   - `Off`: Gleitende Durchschnitte ignorieren und nur auf RSI-Trigger handeln.
   - `Forward`: Longs nur erlauben, wenn der schnelle MA über dem langsamen MA liegt, Shorts nur wenn er darunter liegt.
   - `Reverse`: Den Filter umkehren, sodass Longs den schnellen MA unter dem langsamen MA erfordern, passend zum "Reverse"-Modus des EA.

Beide Bedingungen müssen übereinstimmen, bevor die Strategie eine neue Marktorder eröffnet. Wenn bereits eine Position offen oder eine Order in der Warteschlange ist, werden neue Signale ignoriert, bis sie abgeschlossen sind.

## Trade-Management
- Initialer Stop-Loss und Take-Profit werden in Pips unter Verwendung des Instrumenten-`PriceStep` ausgedrückt. Beide sind optional; das Setzen eines Wertes von null deaktiviert den jeweiligen Ausstieg.
- Wenn `TrailingStopPips` größer als null ist, folgt der Stop dem Preis, sobald der Gewinn `TrailingStopPips + TrailingStepPips` überschreitet. Der Step-Wert muss strikt positiv sein, wenn Trailing aktiviert ist (die Strategie wirft andernfalls eine Ausnahme).
- Wenn `UseMartingale` aktiviert ist, verdoppelt sich das nächste Ordervolumen, nachdem die vorherige Position mit einem Verlust geschlossen wurde (erkannt über realisiertes PnL). Gewinnende Trades setzen den Multiplikator zurück.

## Geldmanagement
- `MoneyMode = FixedVolume` hält dasselbe `VolumeOrRiskValue` für jeden Einstieg.
- `MoneyMode = RiskPercent` behandelt `VolumeOrRiskValue` als Prozentsatz des Portfolio-Eigenkapitals und leitet die Menge aus der konfigurierten Stop-Loss-Distanz ab. Wenn kein Stop-Loss angegeben ist, fällt die Strategie auf den Rohwert zurück.
- Volumen werden an Börsenregeln unter Verwendung von `Security.MinVolume` und `Security.VolumeStep` normalisiert, um ungültige Ordergrößen zu vermeiden.

## Weitere Implementierungshinweise
- Trailing-Logik und Stop/Ziel-Prüfungen werden bei abgeschlossenen Kerzen ausgewertet, um das "neue Kerze"-Verhalten der MQL-Version zu replizieren.
- Das Martingale-Flag verwendet Änderungen des realisierten PnL, wenn eine Position extern geschlossen wird, sodass manuelle Schließungen ebenfalls verfolgt werden.
- Da StockSharp aggregierte Positionen verwendet, werden gleichzeitige Long- und Short-Trades (MT5-Hedging-Modus) nicht unterstützt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen für Indikatoraktualisierungen und Signalgenerierung. |
| `StopLossPips` | Initialer Stop-Loss-Abstand in Pips; null deaktiviert den Stop. |
| `TakeProfitPips` | Initialer Take-Profit-Abstand in Pips; null deaktiviert das Ziel. |
| `TrailingStopPips` | Trailing-Stop-Abstand. Erfordert ein positives `TrailingStepPips`. |
| `TrailingStepPips` | Zusätzliche Pips, die benötigt werden, bevor der Trailing Stop wieder bewegt wird. |
| `MoneyMode` | Wählt feste Lot-Größe oder Risikoprozent-Berechnung. |
| `VolumeOrRiskValue` | Lot-Größe im Festmodus oder Risikoprozent im Risikomodus. |
| `UseMartingale` | Verdoppelt das nächste Ordervolumen nach einem verlorenen Trade. |
| `FastMaPeriod` | Periode des schnellen gleitenden Durchschnitts für den Trendfilter. |
| `SlowMaPeriod` | Periode des langsamen gleitenden Durchschnitts für den Trendfilter. |
| `RsiPeriod` | Mittelungslänge für den RSI-Indikator. |
| `RsiLevelUp` | Oberer RSI-Schwellenwert, der Short-Setups auslöst. |
| `RsiLevelDown` | Unterer RSI-Schwellenwert, der Long-Setups auslöst. |
| `MaMode` | Aktiviert oder invertiert den Gleitender-Durchschnitt-Bestätigungsfilter. |
