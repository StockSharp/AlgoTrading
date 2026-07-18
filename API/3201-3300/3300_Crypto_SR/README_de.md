# Crypto-SR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Crypto-SR-Strategie portiert den MetaTrader-4-Expert-Advisor "Crypto S&R" auf die StockSharp-High-Level-API. Die Implementierung behält die gestaffelte Bestätigungslogik des ursprünglichen Systems bei: einen Trendfilter auf Basis linear gewichteter gleitender Durchschnitte (LWMA), eine Momentum-Prüfung auf höherem Zeitrahmen, einen langfristigen MACD-Trendfilter und aus Fraktalen abgeleitete Unterstützungs-/Widerstandsniveaus. Orders werden per Marktausführung gesendet, und die Position wird über feste Stop-Loss-/Take-Profit-Niveaus, Break-even-Anpassungen und einen in Pips gemessenen Trailing Stop verwaltet.

## Handelslogik

1. **Analyse des primären Zeitrahmens:** Die Strategie abonniert die konfigurierte Kerzenserie und speist zwei LWMAs mit dem typischen Kerzenpreis `(high + low + close) / 3`. Die schnelle LWMA muss über (unter) der langsamen LWMA liegen, um Longs (Shorts) zu erlauben.
2. **Momentum auf höherem Zeitrahmen:** Ein `Momentum`-Indikator wird auf einer zweiten Kerzenserie ausgewertet. Die absolute Entfernung der letzten drei Momentum-Werte vom neutralen Wert (100) muss die Kauf-/Verkaufsschwellen überschreiten.
3. **Langfristiger MACD-Filter:** Die Strategie hört auf einen weiteren Kerzenstrom, in dem ein MACD (12, 26, 9) berechnet wird. Long-Positionen erfordern, dass die MACD-Linie über ihrem Signal bleibt; Short-Positionen benötigen sie darunter. Der langfristige Standardzeitrahmen ist täglich, um die vom EA verwendete Monatsserie anzunähern; er kann angepasst werden, wenn echte Monatskerzen verfügbar sind.
4. **Fraktale Unterstützung/Widerstand:** Fertige Kerzen werden in einem rollierenden Puffer gespeichert. Wenn das klassische Bill-Williams-Fraktalmuster (zwei Nachbarn auf jeder Seite) erscheint, wird das entsprechende Hoch/Tief zum aktiven Widerstands- oder Unterstützungsniveau. Um das Niveau wird ein konfigurierbarer Pip-Puffer gelegt, um die horizontalen Linien des ursprünglichen Experten nachzubilden.
5. **Einstiegsregeln**:
   - *Kauf*: keine offene Long-Position, schnelle LWMA über langsamer LWMA, Momentum-Abweichung >= Kaufschwelle, bullischer MACD, die aktuelle Kerze testet die gepufferte Unterstützung und schließt über dem vorherigen Schlusskurs.
   - *Verkauf*: gespiegelte Bedingungen mit Widerstandsniveau, Momentum-Verkaufsschwelle und bärischer MACD-Bestätigung.
6. **Risikomanagement:** Jede neue Position erhält einen anfänglichen Stop-Loss und Take-Profit in Pips. Break-even-Logik kann den Stop verschieben, sobald die Triggerdistanz erreicht ist, während ein optionaler Trailing Stop dem Preis über Kerzenhochs/-tiefs folgt. Long-/Short-Exposure wird geschlossen, wenn der MACD-Filter gegen den Trade dreht.

## Implementierungshinweise

- Der monatliche MACD-Filter der MetaTrader-Version wird standardmäßig mit einer Tagesserie angenähert, weil StockSharp keine Kalendermonatskerzen direkt bereitstellt. Benutzer können zu einem eigenen Monatsaggregator wechseln, wenn die Datenquelle dies unterstützt.
- Orders werden mit Marktanfragen geschlossen, wenn Schutzlevel verletzt werden. Das spiegelt die `OrderClose`-Aufrufe in MQL wider und vermeidet die Abhängigkeit von börsenseitigen Stop-Orders.
- Alle Indikatorbindungen erfolgen über die High-Level-Abonnement-API; direkte Aufrufe von `GetValue` sind nicht erforderlich.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `FastMaPeriod` | Länge der schnellen LWMA im primären Zeitrahmen. | `6` |
| `SlowMaPeriod` | Länge der langsamen LWMA im primären Zeitrahmen. | `85` |
| `MomentumPeriod` | Momentum-Periode im höheren Zeitrahmen. | `14` |
| `MomentumBuyThreshold` | Minimale absolute Momentum-Abweichung von 100, um Long-Einstiege zu erlauben. | `0.3` |
| `MomentumSellThreshold` | Minimale absolute Momentum-Abweichung von 100, um Short-Einstiege zu erlauben. | `0.3` |
| `MacdFastPeriod` | Länge der schnellen EMA für den langfristigen MACD-Filter. | `12` |
| `MacdSlowPeriod` | Länge der langsamen EMA für den langfristigen MACD-Filter. | `26` |
| `MacdSignalPeriod` | Länge der Signal-EMA für den langfristigen MACD-Filter. | `9` |
| `StopLossPips` | Harte Stop-Loss-Distanz in Pips. | `20` |
| `TakeProfitPips` | Feste Take-Profit-Distanz in Pips. | `50` |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips (0 deaktiviert den Trail). | `40` |
| `UseBreakEven` | Ob der Stop nach einem Gewinntrigger auf Break-even verschoben wird. | `true` |
| `BreakEvenTriggerPips` | Gewinn in Pips, der vor Break-even-Anpassungen erforderlich ist. | `30` |
| `BreakEvenOffsetPips` | Offset beim Verschieben des Stops auf Break-even. | `30` |
| `FractalWindowLength` | Anzahl fertiger Kerzen, die zur Bestätigung fraktaler Hochs und Tiefs gehalten werden. | `7` |
| `FractalBufferPips` | Zusätzlicher Puffer um Fraktalniveaus in Pips. | `10` |
| `TradeVolume` | Volumen jeder Marktorder. | `1` |
| `CandleType` | Primäre Kerzenserie für LWMA- und Fraktallogik. | Zeitrahmen `15m` |
| `HigherCandleType` | Höherer Zeitrahmen für den Momentum-Filter. | Zeitrahmen `1h` |
| `LongTermCandleType` | Zeitrahmen für den MACD-Trendfilter. | Zeitrahmen `1d` |
