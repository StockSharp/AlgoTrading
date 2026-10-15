# TenPips Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **TenPips-Strategie** ist ein StockSharp-Port des MetaTrader-Expert Advisors "10PIPS". Sie kombiniert schnelle/langsame linear gewichtete gleitende Durchschnitte, die auf dem Handelszeitrahmen berechnet werden, mit einer Multi-Zeitrahmen-Momentum-Bestätigung und einem makro (monatlichen) MACD-Filter. Die Konvertierung spiegelt das ursprüngliche Geldverwaltungsmodul wider, einschließlich Break-Even-Schutz, pip-basiertem Trailing und Eigenkapital-/absoluten Gewinnzielen.

## Signallogik

1. **Primärer Zeitrahmen** (Parameter `CandleType`, Standard 15 Minuten) liefert den Preisstrom, der für die schnellen und langsamen LWMAs verwendet wird, berechnet auf dem typischen Preis `(H + L + C) / 3`.
2. **Höherer Zeitrahmen-Momentum** (`MomentumCandleType`, Standard 1 Stunde) konvertiert den StockSharp-Momentum-Unterschied in die MetaTrader-Verhältnis. Der absolute Abstand von `100` über die letzten drei abgeschlossenen Bars muss `MomentumThreshold` überschreiten, damit ein Trade ausgelöst wird.
3. **Makro-MACD-Filter** (`MacdCandleType`, Standard 30-Tages-Kerzen, die den monatlichen MetaTrader-Zeitraum approximieren) erfordert, dass die MACD-Hauptlinie über der Signallinie liegt für Käufe und darunter für Verkäufe.

Eine Long-Position wird eröffnet, wenn die vorherige Kerze:
- über der schnellen LWMA schloss, nachdem sie darunter getaucht war,
- die schnelle LWMA über der langsamen LWMA liegt,
- eine der letzten drei Momentum-Lesungen `MomentumThreshold` erfüllt,
- der Makro-MACD bullisch ist.

Eine Short-Position verwendet die symmetrischen Bedingungen (vorheriger Schlusskurs unter der schnellen LWMA, schnell unter langsam, Momentum über dem Schwellenwert, MACD bearisch).

Da StockSharp mit einem Netto-Positionsmodell arbeitet, öffnet der Port höchstens eine aggregierte Position pro Seite. Das Senden eines Kaufs bei Short-Position schließt automatisch den Short-Anteil und hinterlässt das angeforderte Long-Volumen.

## Risiko- und Geldverwaltung

- **Schutzabstände** – `StopLossPips` und `TakeProfitPips` übersetzen MetaTrader-Pips in Preisoffsets unter Verwendung des `PriceStep` des Instruments. Wenn eine Grenze getroffen wird, schließt die Strategie die gesamte Position mit einer Marktorder.
- **Trailing Stop** – `TrailingStopPips` folgt dem höchsten (Long) oder niedrigsten (Short) Preis seit dem Einstieg.
- **Break-Even** – wenn aktiviert, bewaffnet `BreakEvenTriggerPips` den Stop und verschiebt ihn auf den Einstieg plus den optionalen `BreakEvenOffsetPips`.
- **Geldziele** – das Trio `UseMoneyTakeProfit`, `UsePercentTakeProfit` und `EnableMoneyTrailing` repliziert das `TP_In_Money`, `TP_In_Percent` des EAs und die Balance-basierte Trailing-Sperre. Unrealisierter PnL wird pro Kerzenschluss gemessen.
- **Eigenkapital-Stop** – `UseEquityStop` mit `EquityRiskPercent` implementiert den ursprünglichen `UseEquityStop`/`TotalEquityRisk`-Schutz, indem Positionen geschlossen werden, sobald der Drawdown vom Eigenkapitalhöchststand den Schwellenwert überschreitet.
- **MACD-Exit-Flag** – `UseMacdExit` spiegelt den `Exit`-Schalter des EAs wider und schließt Positionen frühzeitig, wenn der Makro-MACD gegen den Trade dreht.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Netto-Positionsvolumen für Marktorders (MetaTrader-Lotgrößen-Äquivalent). |
| `CandleType` | `15m` Zeitrahmen | Primärer Zeitrahmen für die schnellen/langsamen LWMAs und Trade-Ausführung. |
| `MomentumCandleType` | `1h` Zeitrahmen | Höherer Zeitrahmen-Kerzen für die Momentum-Bestätigung. |
| `MacdCandleType` | `30d` Zeitrahmen | Makro-Zeitrahmen (monatliche Approximation) für MACD-Bestätigung. |
| `FastMaPeriod` | `8` | Periode des schnellen linear gewichteten gleitenden Durchschnitts. |
| `SlowMaPeriod` | `50` | Periode des langsamen linear gewichteten gleitenden Durchschnitts. |
| `MomentumPeriod` | `14` | Lookback für das Momentum-Verhältnis. |
| `MomentumThreshold` | `0.3` | Mindestabsolutabstand von `100` (MetaTrader-Momentum) erforderlich über die letzten drei Bars des höheren Zeitrahmens. |
| `StopLossPips` | `20` | Schutz-Stop-Loss in MetaTrader-Pips. Auf null setzen zum Deaktivieren. |
| `TakeProfitPips` | `50` | Schutz-Take-Profit in MetaTrader-Pips. Auf null setzen zum Deaktivieren. |
| `TrailingStopPips` | `40` | Trailing-Stop-Abstand in Pips (null deaktiviert das Trailing). |
| `UseBreakEven` | `true` | Aktiviert das Break-Even-Verhalten. |
| `BreakEvenTriggerPips` | `30` | Gewinn (Pips) erforderlich, bevor Break-Even aktiviert wird. |
| `BreakEvenOffsetPips` | `30` | Zusätzliche Pips, die zum Break-Even-Stop nach Aktivierung hinzugefügt werden. |
| `UseMoneyTakeProfit` | `false` | Positionen schließen nach Erreichen des absoluten Gewinnziels `MoneyTakeProfit`. |
| `MoneyTakeProfit` | `10` | Gewinnziel in Kontowährung ausgedrückt. |
| `UsePercentTakeProfit` | `false` | Positionen schließen nach Verdienen von `PercentTakeProfit` Prozent des Anfangskapitals. |
| `PercentTakeProfit` | `10` | Prozentziel basierend auf Startkapital. |
| `EnableMoneyTrailing` | `true` | Balance-basierter Trailing-Stop aktivieren mit `MoneyTrailTarget` / `MoneyTrailStop`. |
| `MoneyTrailTarget` | `40` | Gewinn (Währung) erforderlich, bevor der Money-Trail bewaffnet wird. |
| `MoneyTrailStop` | `10` | Erlaubter Rückgang nach Bewaffnen des Money-Trails. |
| `UseEquityStop` | `true` | Eigenkapital-Drawdown-Schutz aktivieren. |
| `EquityRiskPercent` | `1` | Maximaler Drawdown vom Eigenkapitalhöchststand vor erzwungener Flat-Position. |
| `UseMacdExit` | `false` | Positionen bei entgegengesetztem MACD-Signal des Makro-Zeitrahmens schließen. |

## Implementierungshinweise

- Die Pip-Konvertierung folgt der EA-Logik: wenn der Broker-Tick-Size `0.00001` oder `0.001` ist, entspricht ein Pip zehn Ticks; andernfalls wird der rohe `PriceStep` verwendet.
- StockSharp's Momentum-Indikator gibt eine Preisdifferenz aus. Die Strategie konvertiert diese in das MetaTrader-Verhältnis `(Close / Close(period) * 100)` vor der Anwendung von `MomentumThreshold`.
- Der Port arbeitet in einer Netting-Umgebung und repliziert daher nicht das Multi-Ticket-Martingal des EAs (`IncreaseFactor`, `LotExponent`, `Max_Trades`). Stattdessen passt er das Order-Volumen automatisch an, wenn zwischen Long- und Short-Positionen gewechselt wird.
- Schutzende Exits und Gewinnverwaltung senden Marktorders, entsprechend dem ursprünglichen Advisor-Verhalten beim Modifizieren offener Tickets.
- Diagramme zeigen die verarbeiteten Indikatoren (schnelle LWMA, langsame LWMA, Momentum, MACD) wenn Visualisierung verfügbar ist.

## Verwendung

1. Konfiguriere die Kerzen-Zeitrahmen so, dass sie zum MetaTrader-Chart und dem höheren Zeitrahmen des EAs passen.
2. Passe die pip-basierten Risikoparameter an die Instrument-Punktgröße an. Null deaktiviert die entsprechende Komponente.
3. Aktiviere oder deaktiviere Geld-/Prozentziele, Eigenkapital-Stop und MACD-Exit entsprechend deinen Risikopräferenzen.
4. Starte die Strategie; sie abonniert die drei erforderlichen Zeitrahmen, verwaltet Positionen nach den ursprünglichen Regeln und protokolliert jegliche durch Balance- oder Eigenkapitalschutz ausgelöste Schutzausstiege.
