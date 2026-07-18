# UP3x1 Premium-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die UP3x1 Premium-Strategie ist ein C#-Port des MetaTrader-Expert-Advisors *up3x1_premium_v2M*. Sie kombiniert schnelle/langsame EMA-Crossovers mit Weitbereichs-Kerzenfiltern und einem täglichen Kontextfilter, um Momentum-Ausbrüche zu erfassen, während das Risiko durch feste Ziele und Trailing Stops verwaltet wird.

## Funktionsweise

1. **Trenddetektion**
   - Berechnet zwei EMAs auf dem Arbeitszeitrahmen (Standard 12 und 26 Perioden).
   - Verfolgt die vorherigen zwei EMA-Werte, um bullische oder bearische Crossovers ähnlich der MQL-Logik zu identifizieren.
   - Führt eine tägliche EMA, um den übergeordneten Bias zu verstehen.

2. **Einstiegslogik**
   - **Long-Setups** werden ausgelöst, wenn eines der Folgenden eintritt:
     - Die schnelle EMA kreuzt über die langsame EMA und die vorherigen zwei Kerzeneröffnungen zeigen Aufwärtsprogression.
     - Die vorherige Kerze bildet eine bullische Weitbereichsbar, deren Körper den konfigurierten Körperschwellenwert überschreitet.
     - Um Mitternacht, wenn die vorherige Tageskerze deutlich tiefer als sie eröffnete schloss (Kapitulation), wird ein Bounce-Signal erlaubt.
     - Der Preis handelt über der aktuellen Tages-EMA und begünstigt die Long-Seite.
   - **Short-Setups** werden ausgelöst, wenn die Spiegelbedingungen gelten (bearischer EMA-Crossover, breite bearische Bar oder Mitternacht-Umkehr in entgegengesetzter Richtung).
   - Wenn sowohl Long- als auch Short-Trigger gleichzeitig auslösen, folgt die Strategie der vorherrschenden EMA-Beziehung, um die Entscheidung zu treffen.

3. **Ausstiegsverwaltung**
   - Eine offene Position wird geschlossen, wenn:
     - Die EMAs innerhalb von ±0.1% konvergieren, was den Verlust der Richtungsüberzeugung signalisiert.
     - Der Preis die in absoluten Preiseinheiten definierten Take-Profit- oder Stop-Loss-Offsets berührt.
     - Der Trailing Stop (falls aktiviert) hinter dem Preis gezogen wird und anschließend getroffen wird.

4. **Positionshandhabung**
   - Trades werden nur eröffnet, wenn die Strategie flach ist, was dem ursprünglichen EA-Verhalten entspricht.
   - Das Volumen wird über den `OrderVolume`-Parameter gesteuert und auf jede Marktorder angewendet.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `OrderVolume` | Ordergröße in Lots/Kontrakten für jeden Trade. |
| `FastEmaLength` / `SlowEmaLength` | Perioden für die schnellen und langsamen EMAs auf dem Arbeitszeitrahmen. |
| `DailyEmaLength` | Periode für die auf den Tageskerzen berechnete EMA. |
| `TakeProfit` | Absolutes Gewinnziel in Preiseinheiten (auf null setzen zum Deaktivieren). |
| `StopLoss` | Absoluter Stop-Abstand in Preiseinheiten (auf null setzen zum Deaktivieren). |
| `TrailingStop` | Trailing-Distanz, die dem Preis folgt, sobald die Bewegung den Schwellenwert überschreitet. |
| `RangeThreshold` | Mindest-Gesamtbereich, den die vorherige Kerze überschreiten muss, um als Weitbereichsbar zu qualifizieren. |
| `BodyThreshold` | Mindest-Kerzenkörpergröße, die bullische/bearische Thrust-Bars definiert. |
| `DailyReversalThreshold` | Größe der vorherigen täglichen Umkehr, die beim Mitternachtsfilter erforderlich ist. |
| `CandleType` | Arbeitszeitrahmen für die Haupt-EMA und Preislogik. |
| `DailyCandleType` | Höherer Zeitrahmen für den täglichen EMA-Kontext. |

## Verwendungshinweise

- Standardwerte imitieren die numerischen Konstanten aus dem ursprünglichen EA (von Punktwerten zu dezimalen Preisoffsets konvertiert).
- Passen Sie die preisbasierten Schwellenwerte (`TakeProfit`, `StopLoss`, `TrailingStop`, Bereichs-/Körperschwellenwerte) an die Tick-Größe des gehandelten Instruments an.
- Der tägliche EMA-Filter ersetzt den unbedingten Long-Bias im MQL-Skript und hält Trades im Einklang mit dem vorherrschenden übergeordneten Zeitrahmentrend.
- Immer auf historischen Daten backtesten und in einer Demo-Umgebung vorwärts testen, bevor Live-Handel aktiviert wird.
