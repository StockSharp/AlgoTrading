# LANZ-Strategie 4.0 Backtest
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der LANZ-Strategie 4.0 Backtest ist eine Ausbruchsstrategie, die Swing-Pivots zur Erkennung von Trendwechseln verwendet. Wenn der Preis über das letzte Pivot-Hoch steigt, wird long gegangen; wenn der Preis unter das letzte Pivot-Tief fällt, wird short gegangen. Die Positionsgröße wird aus dem Risikoprozentsatz und dem Pip-Wert berechnet, mit Stop-Loss unterhalb/oberhalb des letzten Swings plus Puffer und Take-Profit nach dem Risiko-Rendite-Verhältnis.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Preis überschreitet letztes Pivot-Hoch.
  - **Short**: Preis unterschreitet letztes Pivot-Tief.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Jüngstes Swing-Hoch/-Tief mit Puffer.
- **Standardwerte**:
  - `SwingLength` = 180
  - `SlBufferPoints` = 50
  - `RiskReward` = 1
  - `RiskPercent` = 1
  - `PipValueUsd` = 10
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Long & Short
  - Indikatoren: Highest, Lowest
  - Komplexität: Moderat
  - Risikolevel: Mittel
