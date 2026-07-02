# MACD Divergenz RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Portierung des MetaTrader Expert Advisors **"Macd diver rsi mt4"** zum StockSharp High-Level API.
- Handelt ein einzelnes Symbol mit RSI-Filtern kombiniert mit MACD-Divergenzerkennung für Zeitumkehrungen.
- Es kann jeweils nur eine Marktposition offen sein; Die Strategie wartet auf den flachen Zustand, bevor sie ein neues Signal ausgibt.

## Signallogik
1. Jede fertige Kerze aus dem ausgewählten Zeitrahmen speist vier an die Strategie gebundene Indikatoren:
   - Zwei unabhängige `RelativeStrengthIndex`-Instanzen (für überverkaufte und überkaufte Filter) haben einen Balken zurück abgetastet.
   - Zwei `MovingAverageConvergenceDivergence`-Indikatoren mit konfigurierbaren schnellen/langsamen EMA- und Signallängen.
2. **Bulles Setup**
   - Der vorherige Balken RSI muss unter dem konfigurierbaren Überverkaufsschwellenwert liegen.
   - Die aktuellsten MACD-Werte müssen einen lokalen Rückgang unterhalb eines dynamischen Schwellenwerts bilden (entspricht 3 Pips im aktuellen Instrument).
   - Historische Daten werden gescannt, um einen früheren MACD-Rückgang und das damit verbundene Preisschwankungstief zu lokalisieren. Divergenz wird bestätigt, wenn
Das MACD-Tief steigt, während der Preis ein niedrigeres Tief erreicht (regelmäßige Divergenz), oder das MACD-Tief fällt, während der Preis ein höheres Niveau erreicht
niedrig (versteckte Divergenz), passend zur ursprünglichen MQL-Logik.
   - Wenn die Strategie bestätigt ist und keine offene Position aufweist, wird ein Marktkauf mit richtungsspezifischen Volumen- und Risikoeinstellungen gesendet.
3. **Bearisches Setup** spiegelt die bullischen Regeln mit dem RSI-Überkauft-Filter und MACD-Spitzen wider. Divergenz wird validiert durch
Vergleich früherer Swing-Hochs mit dem aktuellen.
4. Unmittelbar nach einem Einstieg rechnet die Strategie die konfigurierten Stop-Loss- und Take-Profit-Abstände von Pips in Preiseinheiten um
(unter Berücksichtigung der ursprünglichen Punktformatregeln) und wendet sie über `SetStopLoss` / `SetTakeProfit` an.

## Parameter
- `LowerRsiPeriod`, `LowerRsiThreshold` – Zuordnung zu `inp1_Lo_RSIperiod` / `inp1_Ro_Value`.
- `BullishFastEma`, `BullishSlowEma`, `BullishSignalSma` – Zuordnung zu `inp2_fastEMA` / `inp2_slowEMA` / `inp2_signalSMA`.
- `BullishVolume`, `BullishStopLossPips`, `BullishTakeProfitPips` – Zuordnung zu `inp3_VolumeSize`, `inp3_StopLossPips`, `inp3_TakeProfitPips`.
- `UpperRsiPeriod`, `UpperRsiThreshold` – Zuordnung zu `inp4_Lo_RSIperiod` / `inp4_Ro_Value`.
- `BearishFastEma`, `BearishSlowEma`, `BearishSignalSma` – Zuordnung zu `inp5_fastEMA` / `inp5_slowEMA` / `inp5_signalSMA`.
- `BearishVolume`, `BearishStopLossPips`, `BearishTakeProfitPips` – Zuordnung zu `inp6_VolumeSize`, `inp6_StopLossPips`, `inp6_TakeProfitPips`.
- `CandleType` – Zeitrahmenquelle für alle Berechnungen.

## Implementierungshinweise
- Der Divergenzschwellenwert MACD wird von der aktuellen Punktgröße des Instruments abgeleitet und beträgt 3 Pips, was dem Standardwert von 0,0003 entspricht
Wird von der MQL-Version verwendet.
- Kerze, MACD und Preisverlauf werden in begrenzten Listen (600 Elemente) gespeichert, um die Divergenz-Scanfenster ohne zu reproduzieren
Zuweisung großer Arrays.
- Die Strategie verwendet `SubscribeCandles(...).Bind(...)`, um alle Indikatoren in einem einzigen Durchgang zu aktualisieren und nur abgeschlossene Prozesse durchzuführen
Kerzen, genau wie die ursprüngliche Blockausführung einmal pro Balken.
- Pip-Abstände werden vor dem Aufruf von `SetStopLoss` und `SetTakeProfit` in absolute Preisversätze umgewandelt und reproduziert
Punktformatregeln, die oben in der MQL-Quelle deklariert sind.
