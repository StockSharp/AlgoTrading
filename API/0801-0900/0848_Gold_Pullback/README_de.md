# Gold-Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Gold-Pullback-Strategie kombiniert die EMA-Trendrichtung mit MACD- und TDI-Filtern. Long-Trades werden ausgelöst, wenn der Preis die EMA mit 21 Perioden in einem Aufwärtstrend berührt und sowohl MACD als auch TDI bullisch sind. Short-Trades erfolgen bei Pullbacks zur EMA21 in Abwärtstrends mit bärischem MACD und TDI. Jede Position verwendet einen 1:1 Take-Profit und Stop-Loss basierend auf der Signalkerze zuzüglich eines Versatzes.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: EMA14 über EMA60, Kerze berührt EMA21, MACD-Linie über Signallinie, TDI MA über TDI-Signal und RSI über 50.
  - **Short**: EMA14 unter EMA60, Kerze berührt EMA21, MACD-Linie unter Signallinie, TDI MA unter TDI-Signal und RSI unter 50.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit bei gleichem Abstand vom Einstieg mit einem zusätzlichen Versatz.
- **Stops**: `Offset` = 0.1 angewendet auf das Tief/Hoch der Kerze.
- **Standardwerte**:
  - `EmaFastLength` = 14
  - `EmaSlowLength` = 60
  - `EmaPullbackLength` = 21
  - `SlOffset` = 0.1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: EMA, MACD, RSI, SMA
  - Komplexität: Mittel
  - Risikolevel: Mittel
