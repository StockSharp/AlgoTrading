# 1H Bollinger Bands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **1H Bollinger Bands-Strategie** adaptiert den MetaTrader-Experten "1H Bolinger Bands" an die High-Level-API von StockSharp. Die Idee ist, Bounces von den täglichen Bollinger Bands zu handeln, während der stündliche Trend ausgerichtet ist und der langfristige monatliche MACD die Richtung bestätigt. Die Strategie arbeitet standardmäßig auf dem H1-Zeitrahmen und stützt sich auf zusätzliche höhere Zeitrahmen-Datenströme zur Bestätigung.

## Trading-Logik
- **Trendfilter:** Zwei lineare gewichtete gleitende Durchschnitte (LWMA 250 und 500) auf dem Basiszeitrahmen stellen sicher, dass nur Trades zugelassen werden, die mit der dominierenden Richtung übereinstimmen.
- **Auslösemuster:** Auf dem höheren Zeitrahmen (standardmäßig täglich) beobachtet die Strategie eine Kerze, deren Tief unter das untere Bollinger Band fällt und die nächste Kerze darüber öffnet (umgekehrt für Shorts mit dem oberen Band). Dies repliziert die ursprüngliche Bounce-Bedingung.
- **Momentum-Bestätigung:** Momentum (Periode 14) wird auf dem höheren Zeitrahmen berechnet. Mindestens eine der drei jüngsten Momentum-Abweichungen von 100 muss den konfigurierten Schwellenwert (Standard 0.3) überschreiten.
- **MACD-Filter:** Ein monatlicher MACD (12/26/9) muss dem Signal zustimmen. Bei Long-Trades muss die MACD-Linie über der Signallinie liegen, bei Shorts darunter.
- **Einstieg:** Wenn alle Filter übereinstimmen, öffnet die Strategie eine Marktorder. Wenn eine entgegengesetzte Position offen ist, neutralisiert das angeforderte Volumen die bestehende Exposition und dreht die Richtung um.

## Positionsverwaltung
Das Risikomanagement wird direkt in der Strategie mit Pip-Abständen implementiert, die über `Security.PriceStep` konvertiert werden:
- **Stop Loss:** Schließt die Position, sobald der Preis um die konfigurierte Pip-Anzahl gegen den Einstieg läuft.
- **Take Profit:** Sichert Gewinne, wenn der Preis das konfigurierte Pip-Ziel erreicht.
- **Trailing Stop (optional):** Wenn aktiviert und der Kurs die Trailing-Distanz überschreitet, folgt ein internes Trailing-Niveau dem Preis. Eine Kerze, die dieses Niveau durchbricht, schließt den Trade.
- **Break-Even (optional):** Nachdem der Preis um die Auslösedistanz vorgerückt ist, wird das Stop-Niveau auf den Einstiegspreis plus dem konfigurierten Offset verschoben (minus für Shorts). Ein Rückzug auf dieses Niveau beendet die Position.

Die geldbasierte Gewinnverwaltung des ursprünglichen Experten wird nicht nachgebildet; die StockSharp-Version konzentriert sich auf preisbasierte Kontrollen, um börsenunabhängig zu bleiben.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Basiszeitrahmen für die Signalauswertung. | 1-Stunden-Kerzen |
| `HigherTimeFrame` | Zeitrahmen für Bollinger Bands und Momentum. | 1-Tages-Kerzen |
| `MacdTimeFrame` | Zeitrahmen für den bestätigenden MACD. | 30-Tages-Kerzen |
| `FastMaPeriod` / `SlowMaPeriod` | Schnelle/langsame LWMA-Längen auf dem Basiszeitrahmen. | 6 / 85 |
| `TrendFastPeriod` / `TrendSlowPeriod` | Langfristige LWMA-Trendfilter. | 250 / 500 |
| `MomentumPeriod` | Momentum-Lookback auf dem höheren Zeitrahmen. | 14 |
| `MomentumThreshold` | Minimale absolute Abweichung von 100 für Momentum. | 0.3 |
| `BollingerPeriod` / `BollingerWidth` | Tägliche Bollinger-Band-Einstellungen. | 20 / 2.0 |
| `TradeVolume` | Basisvolumen für jede neue Position. | 1 |
| `StopLossPips` / `TakeProfitPips` | Schutz-Stop und Ziel in Pips. | 20 / 50 |
| `EnableTrailing` / `TrailingStopPips` | Trailing-Stop-Schalter und Abstand. | true / 40 |
| `EnableBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Break-Even-Schalter, Auslösedistanz und Offset. | true / 30 / 30 |

Alle numerischen Parameter werden über `StrategyParam<T>` bereitgestellt und können in Designer/Runner optimiert werden.

## Implementierungshinweise
- Die Strategie abonniert gleichzeitig drei Kerzenströme: Basiszeitrahmen, höherer Zeitrahmen für Bollinger/Momentum und MACD-Zeitrahmen.
- Momentum verwendet den Standard-StockSharp-`Momentum`-Indikator und speichert die letzten drei Abweichungen, um die MQL-Logik nachzubilden.
- Tradingvolumen und Pip-Abstände gehen davon aus, dass `Security.PriceStep` korrekt gefüllt ist; andernfalls wird die Schutzlogik nicht ausgelöst.
- StockSharp pflegt eine einzelne Nettoposition. Das "Max_Trades"-Skalierungsverhalten des ursprünglichen Skripts wird auf eine einzelne aggregierte Position in diesem Port vereinfacht.
- Equity-basierte Stop-outs und Geld-Trailing-Funktionen der MQL-Version werden absichtlich weggelassen, um die Implementierung börsenunabhängig zu halten.

## Verwendung
1. Hängen Sie die Strategie an ein Instrument an, das stündliche, tägliche und monatliche Kerzen bereitstellt (oder passen Sie die Parameter entsprechend an).
2. Stellen Sie sicher, dass das Instrument `PriceStep` bereitstellt, damit Pip-Abstände in Preis-Offsets übersetzt werden.
3. Konfigurieren Sie das gewünschte Volumen und die Risikoparameter in der UI oder im Code, bevor Sie die Strategie starten.
4. Starten Sie die Strategie; sie abonniert automatisch die erforderlichen Daten, wertet Signale auf geschlossenen Kerzen aus und verwaltet die Position mit den konfigurierten Schutzregeln.

## Bekannte Unterschiede zum MQL-Experten
- Geldbasiertes Trailing und Gesamt-Equity-Stop sind nicht implementiert; nur preisbasierte Kontrollen bleiben erhalten.
- Alerts, E-Mails und Push-Benachrichtigungen aus dem MQL-Code werden weggelassen.
- Order-Stacking wird durch das Single-Net-Position-Modell von StockSharp ersetzt.

Diese Anpassungen halten die Strategie idiomatisch für StockSharp, während die Kern-Trading-Idee des ursprünglichen Experten erhalten bleibt.
