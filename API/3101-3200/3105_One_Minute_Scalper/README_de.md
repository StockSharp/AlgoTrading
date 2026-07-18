# Ein-Minuten-Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **1 MINUTE SCALPER** MetaTrader 4 Expert Advisor in die StockSharp High-Level-API. Sie behält die
mehrschichtige Trendbestätigung, das Multi-Timeframe-Momentum und den Langzeit-MACD-Filter des Original-Roboters bei, während
die Risikokontrollen an StockSharp's positionszentriertes Modell angepasst werden.

## Kernlogik

1. **Trend-Stack** – dreizehn linear gewichtete gleitende Durchschnitte (LWMA 3/5/8/10/12/15/30/35/40/45/50/55/200) müssen in
   strenger Reihenfolge ausgerichtet sein. Long-Trades erfordern, dass jeder kürzere Durchschnitt über dem nächsten liegt,
   während Shorts die Bedingung umkehren.
2. **Primäres Trend-Gate** – ein zusätzlicher schneller LWMA (Standard 6) muss für Longs über dem langsamen LWMA (Standard 85)
   bleiben und für Shorts darunter, was die schnell-vs-langsam-Prüfung des EA widerspiegelt.
3. **Kerzenstruktur** – Einträge werden nur ausgelöst, wenn die Überlappungsmuster aus dem Skript vorhanden sind: Für Longs muss
   das Tief vor zwei Bars unter dem vorherigen Hoch liegen, für Shorts muss das vorherige Tief unter dem Hoch von vor zwei Bars
   fallen.
4. **Momentum-Filter** – ein 14-Perioden-Momentum-Indikator, berechnet auf einem höheren Zeitrahmen (Standard 15-Minuten-
   Kerzen), muss bei mindestens einem der letzten drei Werte um mindestens die konfigurierten Schwellenwerte von 100 abweichen.
   Dies reproduziert die `MomLevelB/MomLevelS`-Vergleiche.
5. **Monatlicher MACD-Bias** – ein MACD, der auf dem ausgewählten MACD-Zeitrahmen aufgebaut ist (standardmäßig 30-Tage-Kerzen
   als Proxy für monatliche Daten), muss die Hauptlinie über der Signallinie für Longs oder darunter für Shorts zeigen.

## Trade-Management

- **Anfangsschutz** – Stop-Loss- und Take-Profit-Abstände werden in Instrument-Schritten (Punkten) ausgedrückt. Wenn eine
  Position öffnet, wandelt die Strategie diese Schrittzählungen in absolute Preise um, indem sie `Security.PriceStep` verwendet.
- **Break-Even-Bewegung** – nachdem sich der Preis um `BreakEvenTriggerSteps` zugunsten bewegt hat, wird der Stop auf den
  Einstieg plus `BreakEvenOffsetSteps` verschoben (für Shorts gilt die gespiegelte Logik). Das Flag wird einmal pro Position
  ausgelöst.
- **Schritt-Trailing** – wenn `TrailingStopSteps` positiv ist, folgt der Stop dem höchsten (oder niedrigsten) Preis seit dem
  Einstieg um die angegebene Anzahl von Schritten.
- **Geld-Trailing** – sobald der schwebende Gewinn `MoneyTrailTarget` (Währung) überschreitet, verfolgt die Strategie den
  Spitzen-PnL und schließt die Position, wenn der Rückgang `MoneyTrailStop` entspricht.
- **Geld-/Prozentziele** – optionale absolute oder prozentuale Take-Profit-Ziele schließen alle Positionen, wenn der schwebende
  PnL die konfigurierten Schwellenwerte überschreitet. Das Prozentziel verwendet den beim Start der Strategie erfassten
  Anfangs-Portfoliowert.
- **Eigenkapital-Stop** – die Strategie überwacht das maximale Eigenkapital (Portfoliowert plus offener PnL). Wenn der Drawdown
  von diesem Höchststand `EquityRiskPercent` überschreitet, werden alle Positionen abgebaut und die `AccountEquityHigh()`-
  Schutzmaßnahme des EA repliziert.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `Volume` | Ordervolumen für neue Einträge. Wird zur absoluten aktuellen Position hinzugefügt, sodass Umkehrungen die Exposition sofort wechseln. |
| `FastMaPeriod` / `SlowMaPeriod` | LWMA-Längen für den primären Trendfilter. |
| `MomentumPeriod` | Länge des Momentum-Indikators auf höherem Zeitrahmen. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Minimale absolute Abweichung von 100, die für Long-/Short-Momentum-Bestätigung erforderlich ist. |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | MACD-Konfiguration für `MacdCandleType`. |
| `StopLossSteps` / `TakeProfitSteps` | Schutz-Stop- und Zielabstände in Preisschritten. Auf null setzen zum Deaktivieren. |
| `TrailingStopSteps` | Schrittbasierter Trailing-Stop-Abstand (0 deaktiviert). |
| `BreakEvenTriggerSteps` / `BreakEvenOffsetSteps` | Abstand zum Auslösen der Break-Even-Bewegung und der Offset beim Verschieben des Stops. |
| `UseMoneyTakeProfit`, `MoneyTakeProfit` | Währungsbasiertes schwebendes Gewinnziel aktivieren und dimensionieren. |
| `UsePercentTakeProfit`, `PercentTakeProfit` | Schwebendes Gewinnziel als Prozentsatz des Anfangskapitals aktivieren und dimensionieren. |
| `EnableMoneyTrailing`, `MoneyTrailTarget`, `MoneyTrailStop` | Trailing-Logik für den schwebenden Gewinn konfigurieren. |
| `UseEquityStop`, `EquityRiskPercent` | Drawdown-Stop aktivieren und den maximalen Drawdown-Prozentsatz definieren. |
| `CandleType` | Primäre Arbeitskerzen (Standard 1 Minute). |
| `MomentumCandleType` | Kerzen auf höherem Zeitrahmen für den Momentum-Indikator (Standard 15 Minuten). |
| `MacdCandleType` | Kerzen für den MACD-Trendfilter (Standard 30 Tage ≈ monatlich). |

## Unterschiede zum MT4-Expert

- StockSharp verwendet Nettopositionen, daher pflegt die Strategie immer eine einzelne aggregierte Position anstelle mehrerer
  Tickets bis zu `Max_Trades`. Umkehrungen schließen das bestehende Exposure, bevor sie in die entgegengesetzte Richtung öffnen.
- `PercentTakeProfit` bezieht sich auf den beim Start erfassten Portfoliowert statt des sich ständig ändernden
  `AccountBalance()`, das MetaTrader verwendet, was geräuschvolle Ziele vermeidet, wenn externe Trades das Guthaben ändern.
- Die geldbasierte Ausstiegslogik (`Take_Profit_In_Money` und `TRAIL_PROFIT_IN_MONEY2`) arbeitet mit dem Live-PnL, der aus dem
  durchschnittlichen Eintrittspreis der Strategie berechnet wird. Dies entspricht dem EA-Verhalten, aber innerhalb des
  StockSharp-Schutzrahmens.
- Die Plattform muss Kerzen-Feeds für die ausgewählten Zeitrahmen liefern (`CandleType`, `MomentumCandleType`,
  `MacdCandleType`). Stellen Sie sicher, dass die verwendeten Adapter die angeforderten Auflösungen unterstützen.

Stimmen Sie die Schwellenwerte auf Ihr Instrument und Ihre Session ab. Enge Spreads oder hochvolatile Paare können breitere
Schrittabstände oder größere Momentum-Schwellenwerte erfordern, um Rauschen zu reduzieren.
