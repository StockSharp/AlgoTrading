# Testinator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie ist eine C#-Portierung des MetaTrader Expert Advisors **Testinator v1.30a**. Es eröffnet nur Long-Positionen und verwaltet diese als Korb. Jeder neue Kauf ist nur zulässig, wenn ein konfigurierbarer Satz technischer Filter „wahr“ zurückgibt und der Preis um eine Mindestanzahl von Pips gestiegen ist. Die Ausgangslogik spiegelt die Eingangslogik wider, indem sie eine andere Filtermaske verwendet. Der ursprüngliche EA stützte sich auch auf tägliche ATR-Messungen für das Risikomanagement, daher abonniert der Hafen zusätzlich zum primären Zeitrahmen auch tägliche Kerzen.

## Handelslogik

### Eingabefiltermaske (Parameter `BuySequence`)

Die Maske verwendet die unteren neun Bits. Ein gesetztes Bit muss den entsprechenden Test der zuvor fertigen Kerze bestehen.

| Etwas | Zustand |
| --- | --------- |
| 1 | EMA(12) liegt über SMA(14). |
| 2 | EMA(50) bleibt unter den Tiefstständen der letzten drei Kerzen. |
| 4 | Das vorherige Tief liegt unter dem unteren Bollinger-Band (20, 2). |
| 8 | ADX(14) liegt über dem -DI und +DI ist stärker als -DI. |
| 16 | Stochastic (16, 4, 8) hat %K über %D und %D über 80. |
| 32 | Williams %R(14) ist größer als -20. |
| 64 | Die Leitung MACD(12, 26, 9) liegt über der Signalleitung. |
| 128 | Ichimoku zeigt Senkou Span A über Span B, Tenkan über Kijun und das vorherige Tief über Span A. |
| 256 | RSI (Zeitraum `RsiEntryPeriod`) liegt über `RsiEntryLevel` und steigt relativ zum vorherigen Wert. |

### Filtermaske verlassen (Parameter `CloseBuySequence`)

| Etwas | Zustand |
| --- | --------- |
| 1 | SMA(14) liegt über EMA(12). |
| 2 | EMA(50) liegt über den Höchstständen der letzten drei Kerzen. |
| 4 | Das vorherige Hoch liegt über dem oberen Ausstiegsband Bollinger (`BollingerCloseLength`, `BollingerCloseDeviation`). |
| 8 | -DI liegt über +DI. |
| 16 | Stochastic %D liegt unter 80. |
| 32 | Williams %R(14) ist kleiner als -80. |
| 64 | Die Linie MACD liegt unterhalb der Signallinie. |
| 128 | Ichimoku Senkou Span B liegt über Senkou Span A. |
| 256 | RSI (Zeitraum `RsiClosePeriod`) liegt unter `RsiCloseLevel`. |

Ein Korb wird nur dann erweitert, wenn alle aktiven Eintragsbits „true“ zurückgeben, die Anzahl der Käufe unter `MaxBuys` liegt und der letzte Füllpreis mindestens `StepPips` entfernt ist. Der Korb wird immer dann abgeflacht, wenn die Austrittsmaske passiert oder wenn Schutzstufen ausgelöst werden.

### Sitzungskontrolle und Risikomanagement

* Der Handel findet nur zwischen `TradeStartHour` und `TradeStartHour + TradeDurationHours - 1` (Osteuropäische Zeit) statt. Wenn das Fenster geschlossen ist und der Korb im Gewinn ist, werden alle Käufe geschlossen.
* Die Schutzstopp- und Take-Profit-Distanzen werden in Pips ausgedrückt. Wenn Sie einen Wert auf `-1` setzen, wird dieser deaktiviert, während `0` den ATR-Multiplikator aktiviert (`StopRatio`, `TakeRatio`).
* Der Trailing Stop verwendet die gleiche ATR-Logik bis `StartTrailPips`, `TrailStepPips`, `StartTrailRatio` und `TrailStepRatio`.
* Die Strategie berechnet tägliche ATR(15)-Werte für D1-Kerzen, um das Verhalten mit dem von EA identisch zu halten.

## Parameter

* `TradeVolume` – Losgröße (Volumen) für jeden Marktkauf.
* `BuySequence` / `CloseBuySequence` – Bitmasken, die individuelle Indikatorfilter ermöglichen.
* `MaxBuys` – maximale Anzahl offener Käufe, die als Warenkorb behandelt werden.
* `StepPips` – Mindestpreisfortschritt (Pips) vor dem Hinzufügen zum Warenkorb.
* `TradeStartHour`, `TradeDurationHours` – definiert das tägliche Handelsfenster.
* `TakeProfitPips`, `StopLossPips` – feste Schutzstufen (Negativ deaktiviert, Null schaltet zu ATR-Verhältnissen).
* `StartTrailPips`, `TrailStepPips` – Trailing-Startdistanz und Schritt (negativ deaktiviert, Null verwendet ATR-Verhältnisse).
* `TakeRatio`, `StopRatio`, `StartTrailRatio`, `TrailStepRatio` – ATR Multiplikatoren, die verwendet werden, wenn der feste Wert gleich Null ist.
* `RsiEntryLevel`, `RsiEntryPeriod` – RSI Schwellenwert und Zeitraum für die Eingabemaske.
* `RsiCloseLevel`, `RsiClosePeriod` – RSI Schwellenwert und Zeitraum für die Exit-Maske.
* `BollingerCloseLength`, `BollingerCloseDeviation` – Parameter der Ausgangsbänder Bollinger.
* `CandleType` – Zeitrahmen der Arbeitskerzen (Tageskerzen werden automatisch für ATR abonniert).

## Notizen

* Der Port behält das Korbabrechnungsmodell des ursprünglichen EA bei: Alle Aufträge sind Käufe und es werden nur Marktaufträge verwendet.
* Die Logik speichert absichtlich frühere Indikatorwerte, um die „bar[1]“-Prüfungen von MetaTrader nachzuahmen.
* Die Strategie ignoriert die nicht verwendeten Eingaben von EA (`TakeAsBasket`, `StopAsBasket` usw.), da sie keinen Einfluss auf die Logik von MQL hatten.
