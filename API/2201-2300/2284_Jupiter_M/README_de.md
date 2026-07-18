# Jupiter M-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grid-basierte Strategie, übersetzt vom MetaTrader-Experten "Jupiter M. 4.1.1".
Der Algorithmus baut einen Auftragskorb mit einem konfigurierbaren Schritt auf und passt
sowohl Take-Profit als auch Volumen an, wenn neue Ebenen geöffnet werden.

## Details

- **Einstiegskriterien**:
  - Long: Kurs fällt um die Schrittgröße und (optional) CCI < -100
  - Short: Kurs steigt um die Schrittgröße und (optional) CCI > 100
- **Long/Short**: Beide
- **Ausstiegskriterien**: Korb erreicht den berechneten Take-Profit
- **Stops**: Breakeven nach einer bestimmten Anzahl von Schritten
- **Standardwerte**:
  - `TakeProfit` = 10
  - `FirstStep` = 20
  - `FirstVolume` = 0.01
  - `VolumeMultiplier` = 2
  - `CciPeriod` = 50
  - `CandleType` = 5-Minuten-Kerzen
- **Filter**:
  - Kategorie: Grid, Mean Reversion
  - Richtung: Beide
  - Indikatoren: CCI (optional)
  - Stops: Breakeven
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch

## Parameter

- `TakeProfit` – Gewinnziel in Preiseinheiten für den Korb.
- `UseAverageTakeProfit` – Take-Profit vom Durchschnittspreis offener Orders berechnen.
- `DynamicTakeProfit` – Take-Profit nach `TpDynamicStep` mit `TpDecreaseFactor` reduzieren, mit einem Minimum bei `MinTakeProfit`.
- `BreakevenClose` / `BreakevenStep` – Ziel nach einer Anzahl von Schritten auf Breakeven verschieben.
- `FirstStep` – anfänglicher Abstand zwischen Grid-Ebenen.
- `DynamicStep`, `StepIncreaseStep`, `StepIncreaseFactor` – Schritt für jede weitere Order erhöhen.
- `MaxStepsBuy` / `MaxStepsSell` – maximale Anzahl von Orders pro Richtung.
- `FirstVolume`, `VolumeMultiplier`, `MultiplyUseStep` – steuern das Volumenwachstum im Grid.
- `CciFilter` / `CciPeriod` – optionaler CCI-Filter für die erste Order.
- `AllowBuy` / `AllowSell` – Handelsrichtungen aktivieren.
- `CandleType` – Kerzenzeitrahmen für Berechnungen.

Die Strategie zielt darauf ab, die Preismittelwertrückkehr durch Positionsmittelung zu erfassen
und den Korb bei dynamischen Gewinnzielen zu schließen.
