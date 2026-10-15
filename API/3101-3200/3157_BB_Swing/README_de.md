# BB Swing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **BB Swing-Strategie** ist ein getreuer Port des MetaTrader-Expert Advisors "BB SWING". Sie handelt Bollinger-Band-Pullbacks, die mit dem vorherrschenden Trend übereinstimmen, der durch zwei linear gewichtete gleitende Durchschnitte (LWMAs) definiert wird. Ein höherer Zeitrahmen-Momentum-Filter und ein sehr langsamer MACD helfen, die Stärke der Umkehr zu bestätigen, bevor eine Position eröffnet wird.

## Handelslogik

1. Nur mit abgeschlossenen Kerzen des `CandleType`-Zeitrahmens arbeiten.
2. Die letzten vier abgeschlossenen Kerzen verfolgen, um aktuelle Extreme und Kerzenkörper zu inspizieren.
3. Warten, dass die schnelle LWMA über (für Longs) oder unter (für Shorts) der langsamen LWMA bleibt.
4. Prüfen, ob eines der letzten drei Tiefs das untere Bollinger-Band berührt (Long-Setup) oder eines der Hochs das obere Band berührt (Short-Setup).
5. Erfordern, dass die vorherige Kerze einen stärkeren Körper als ihre Vorgängerin hat, was Momentum weg vom Band signalisiert.
6. Die Trendstärke mit dem auf `MomentumCandleType` berechneten Momentum bestätigen. Die Strategie misst den absoluten Abstand zwischen der Momentum-Lesung und 100; der Abstand muss die konfigurierten Kauf-/Verkaufsschwellen bei einem der letzten drei Momentum-Werte überschreiten.
7. Die langfristige Richtung mit einem auf dem `MacdCandleType`-Zeitrahmen berechneten MACD validieren. Long-Einträge sind erlaubt, während die MACD-Hauptlinie über der Signallinie bleibt; Shorts erfordern das entgegengesetzte Verhältnis.
8. Wenn alle Bedingungen erfüllt sind, eine Marktposition mit dem aktuellen Martingal-Volumenschritt eingehen.

## Positionsgrößenbestimmung und Skalierung

- `InitialVolume` definiert das erste Einstiegsvolumen.
- Jedes zusätzliche Add-on multipliziert das Basisvolumen mit `LotExponent` (`volume = InitialVolume * LotExponent^n`).
- `MaxTrades` begrenzt die Anzahl der sequenziellen Add-ons, sodass die Gesamtpositionsgröße niemals `InitialVolume * MaxTrades` überschreitet.

## Ausstiegs- und Schutzregeln

- Feste `StopLoss`- und `TakeProfit`-Werte in Kursschritten ausgedrückt.
- Optionale Break-Even-Logik (`EnableBreakEven`), die den Stop auf `BreakEvenOffset` verschiebt, sobald der Preis `BreakEvenTrigger` Schritte vorschreitet.
- Klassischer Trailing Stop (`EnableTrailingStop`), der dem Extrempreis um `TrailingStop` Schritte folgt.
- Geldverwaltungstools:
  - `UseMoneyTakeProfit` schließt Positionen, sobald der unrealisierte Gewinn in Kontowährung `MoneyTakeProfit` erreicht.
  - `UsePercentTakeProfit` schließt Positionen, sobald der Gewinn `PercentTakeProfit` Prozent des Startkapitals entspricht.
  - `UseMoneyTrailing` aktiviert einen Gewinn-Trail: sobald der Gewinn `MoneyTrailTarget` überschreitet, löst ein Rückgang von `MoneyTrailStop` einen Ausstieg aus.
- `UseEquityStop` überwacht den Eigenkapital-Drawdown relativ zum während der Sitzung aufgezeichneten Eigenkapitalhöchststand. Ein Drawdown größer als `EquityRiskPercent` schließt alle Positionen.
- Optionales `CloseOnMacdCross` steigt aus, wann immer die MACD-Hauptlinie die Signallinie gegen die aktuelle Positionsrichtung kreuzt.

Alle Schutzaktionen basieren auf Marktorders (`BuyMarket` / `SellMarket`), um die gesamte Position zu neutralisieren.

## Parameter

| Name | Beschreibung |
|------|-------------|
| `InitialVolume` | Basis-Trade-Volumen für den ersten Einstieg. |
| `LotExponent` | Multiplikator für das Volumen jedes zusätzlichen Einstiegs beim Skalieren. |
| `MaxTrades` | Maximale Anzahl sequenzieller Add-ons jederzeit erlaubt. |
| `TakeProfit` | Take-Profit in Kursschritten ausgedrückt. |
| `StopLoss` | Stop-Loss in Kursschritten ausgedrückt. |
| `FastMaPeriod` | Periode der schnellen LWMA auf typischen Preisen berechnet. |
| `SlowMaPeriod` | Periode der langsamen LWMA auf typischen Preisen berechnet. |
| `MomentumLength` | Anzahl der Bars in der Momentum-Berechnung. |
| `MomentumBuyThreshold` | Mindestabstand von 100 damit das höhere Zeitrahmen-Momentum Long-Trades validiert. |
| `MomentumSellThreshold` | Mindestabstand von 100 damit das höhere Zeitrahmen-Momentum Short-Trades validiert. |
| `EnableBreakEven` | Aktiviert Break-Even-Stop-Verschiebung. |
| `BreakEvenTrigger` | Kursschritte erforderlich um die Break-Even-Verschiebung auszulösen. |
| `BreakEvenOffset` | Offset am Stop nach Break-Even-Aktivierung. |
| `EnableTrailingStop` | Aktiviert den klassischen Trailing Stop in Kursschritten. |
| `TrailingStop` | Größe des Trailing Stops in Schritten ausgedrückt. |
| `UseMoneyTakeProfit` | Aktiviert feste Gewinnmitnahme in Kontowährung. |
| `MoneyTakeProfit` | Gewinn in Währung der die Position schließt wenn `UseMoneyTakeProfit` aktiv ist. |
| `UsePercentTakeProfit` | Aktiviert eigenkapitalprozentbasierte Gewinnmitnahme. |
| `PercentTakeProfit` | Prozentsatz des Startkapitals der einen Ausstieg auslöst wenn `UsePercentTakeProfit` aktiv ist. |
| `UseMoneyTrailing` | Aktiviert geldbasiertes Trailing nach Erreichen eines Gewinnziels. |
| `MoneyTrailTarget` | Gewinnniveau das die Money-Trailing-Logik aktiviert. |
| `MoneyTrailStop` | Maximaler erlaubter Rückgang in Währung nach Aktivierung. |
| `UseEquityStop` | Aktiviert das Schließen von Positionen wenn der schwebende Drawdown einen Schwellenwert überschreitet. |
| `EquityRiskPercent` | Maximal erlaubter Eigenkapital-Drawdown in Prozent. |
| `CloseOnMacdCross` | Aktiviert MACD-basierte Ausstiegsfilterung. |
| `CandleType` | Primärer Zeitrahmen für Signalberechnungen. |
| `MomentumCandleType` | Höherer Zeitrahmen für den Momentum-Filter. |
| `MacdCandleType` | Sehr langsamer Zeitrahmen für den MACD-Ausstiegsfilter. |

## Hinweise

- Die Strategie verarbeitet nur abgeschlossene Kerzen; sie reagiert nicht intrabar.
- Alle Stop- und Zielberechnungen verwenden den vom angeschlossenen Exchange gemeldeten Instrument-Kursschritt. Stelle sicher, dass `PriceStep` korrekt konfiguriert ist für präzise Risikokontrolle.
- Geld- und eigenkapitalbasierte Schutzmaßnahmen basieren auf den Strategie-Portfolio-Statistiken in StockSharp. Im Tester-Modus stelle sicher, dass der Portfolio-Feed aktiviert ist.
- Im Gegensatz zum ursprünglichen MQL-Experten verwaltet diese C#-Implementierung eine einzelne aggregierte Position pro Richtung. Beim Skalieren wird die aggregierte Position erhöht statt mehrere diskrete Tickets zu öffnen.
- Bollinger-Bänder verwenden eine feste Länge von 20 und eine Breite von 2 Standardabweichungen auf typischen Preisen, passend zum ursprünglichen Code.
