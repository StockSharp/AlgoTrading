# BTCUSD Adjustable SLTP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt BTCUSD mit einem Crossover zwischen SMA(10) und SMA(25) sowie einem EMA(150)-Filter. Long-Einstiege warten auf einen Rücksetzer: Nach dem Crossover wird ein Retracement-Prozentsatz verfolgt, und eine Long-Position wird eröffnet, wenn der Preis wieder über dieses Niveau steigt. Short-Einstiege werden sofort bei einem bärischen Crossover ausgelöst, solange der Preis unter dem EMA liegt.

Ausstiege verwenden einstellbare Take-Profit-, Stop-Loss- und Break-Even-Abstände. Eine Long-Position wird auch geschlossen, wenn SMA(10) unter SMA(25) kreuzt, während der Preis unter EMA(150) liegt.

## Details

- **Einstiegskriterien**:
  - Long: SMA(10) kreuzt über SMA(25), dann zieht der Preis um einen festgelegten Prozentsatz zurück und kreuzt über das Retracement-Niveau.
  - Short: SMA(10) kreuzt unter SMA(25), während der Preis unter EMA(150) liegt.
- **Long/Short**: Long und Short.
- **Ausstiegskriterien**:
  - Konfigurierbare Take-Profit-, Stop-Loss- und Break-Even-Abstände.
  - Long-Ausstieg, wenn SMA(10) unter SMA(25) unter EMA(150) kreuzt.
- **Stops**: Ja, einstellbar in Punkten.
- **Standardwerte**:
  - `FastSmaLength` = 10
  - `SlowSmaLength` = 25
  - `EmaFilterLength` = 150
  - `TakeProfitDistance` = 1000
  - `StopLossDistance` = 250
  - `BreakEvenTrigger` = 500
  - `RetracementPercentage` = 0.01
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: SMA, EMA
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
