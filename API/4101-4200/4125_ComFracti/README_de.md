# ComFracti-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

ComFracti ist eine direktionale Strategie, die aus dem MT4-Expertenberater „ComFracti“ übersetzt wurde. Die Logik kombiniert die fraktale Bestätigung mehrerer Zeitrahmen mit RSI und stochastischen Filtern, während optionale gleitende Durchschnitte, parabolische SAR, Kanal- und Perzeptronfilter die Trendausrichtung steuern. Die C#-Implementierung handelt jeweils eine einzelne Position und wertet Signale für abgeschlossene Kerzen mithilfe von StockSharp-APIs auf hoher Ebene aus.

## Handelslogik

- **Primärsignal**
  - Bestätigt ein bullisches Setup, wenn sowohl der aktuelle Zeitrahmen als auch der höhere Zeitrahmen ein bullisches Fraktalsignal erzeugen.
  - Bestätigt ein bärisches Setup, wenn beide Zeitrahmen ein bärisches Fraktalsignal erzeugen.
  - RSI (Standard 3-Perioden im höheren Zeitrahmen) muss für Long-Positionen unter `50 - RsiLevelBuy` und für Short-Positionen über `50 + RsiLevelSell` liegen, wenn der RSI-Filter aktiviert ist.
  - Der stochastische Oszillator (Standard %K-Periode 5 mit %D-Glättung 3/3) muss für Long-Positionen unter `50 - StochasticLevelBuy` und für Short-Positionen über `50 + StochasticLevelSell` liegen, wenn der stochastische Filter aktiviert ist.
- **Optionale Filter**
  - **EMA-Steigung**: Der EMA im Filterzeitrahmen muss für Long-Positionen steigen und für Short-Positionen fallen.
  - **Parabolic SAR**: Der Wert von SAR muss für Long-Positionen unter der offenen Leiste und für Short-Positionen darüber bleiben.
  - **Kanalausbruch**: vergleicht den vorherigen Balken mit einem adaptiven Kanal im Donchian-Stil; Frühere Tiefststände müssen bei Long-Positionen über der Kanaluntergrenze bleiben, während frühere Höchststände bei Short-Positionen unter der Obergrenze bleiben müssen.
  - **Perceptron**: Eine gewichtete Summe der jüngsten Höchst-/Tiefstdifferenzen muss für Long-Positionen positiv und für Short-Positionen negativ sein.
- **Positionsverwaltung**
  - Es ist immer nur eine Position aktiv; Die Strategie schließt das bestehende Engagement, bevor sie einen neuen Trade in die entgegengesetzte Richtung eröffnet.
  - Feste Stop-Loss- und Take-Profit-Abstände werden in Instrumentenpunkten ausgedrückt.
  - Ein optionaler Trailing Stop bewegt sich in Richtung Gewinn, sobald der Trailing Buffer erreicht ist (wenn `ProfitTrailing` wahr ist).
  - Wenn `CloseOnOppositeSignal` aktiviert ist, wird die Strategie vorzeitig beendet, wenn das entgegengesetzte Primärsignal auftritt.

## Risikomanagement

- Die Basispositionsgröße entspricht dem Parameter `BaseVolume` (Standard 0,1 Lots). Wenn `AccountMicro` aktiviert ist, wird die Lautstärke durch zehn geteilt.
- Wenn `UseMoneyManagement` aktiviert ist, riskiert die Strategie `RiskPercent` des Kontowerts pro Trade, indem sie die konfigurierte Stop-Loss-Distanz und den Instrumentenschrittwert verwendet, um die Positionsgröße anzunähern. Das berechnete Volumen wird durch `MinimumVolume` begrenzt.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `TakeProfitPoints`, `StopLossPoints` | Take-Profit- und Stop-Loss-Abstände in Instrumentenpunkten. |
| `UseTrailingStop`, `TrailingStopPoints`, `ProfitTrailing` | Trailing-Stop-Kontrollen (Abstand und ob Trailing einen offenen Gewinn erfordert). |
| `BaseVolume`, `UseMoneyManagement`, `RiskPercent`, `AccountMicro`, `MinimumVolume` | Konfiguration der Positionsgröße. |
| `UseFractals`, `FractalShift*` | Aktiviert die fraktale Bestätigung und definiert die Balkenversätze, die im aktuellen und höheren Zeitrahmen überprüft werden sollen. |
| `UseRsi`, `RsiLevelBuy`, `RsiLevelSell`, `RsiType` | RSI Filter-Offsets und Zeitrahmen. |
| `UseStochastic`, `StochasticPeriod*`, `StochasticLevel*` | Stochastic Oszillatorperioden und Schwellenwerte. |
| `UseMaFilter`, `MaPeriod` | EMA Filterkonfiguration im Filterzeitraum. |
| `UsePsarFilter`, `PsarStep` | Parabolic SAR Filterkonfiguration. |
| `UseChannelFilter`, `ChannelLookback`, `ChannelK` | Parameter des Kanal-Breakout-Filters. |
| `UsePerceptronFilter`, `PerceptronV1`–`PerceptronV4` | Perceptron-Filtergewichte (0–100, zentriert um 50). |
| `CandleType`, `HigherFractalType`, `FilterType` | Von der Strategie verwendete Datenzeitrahmen. |

## Notizen

- Die Strategie verarbeitet nur fertige Kerzen, daher kann das Verhalten geringfügig vom ursprünglichen Tick-gesteuerten Expertenberater abweichen.
- Der Fraktal-Tracker reproduziert die MT4-Fraktallogik mit fünf Balken und ermöglicht es dem Benutzer, zu verschieben, welcher historische Balken ausgewertet wird, passend zu den MT4-`sh1/ sh2`-Parametern.
- Das Geldmanagement stützt sich auf die verfügbare Portfoliobewertung innerhalb von StockSharp; Liegt keine Bewertung vor, greift die Strategie auf das feste Basisvolumen zurück.
