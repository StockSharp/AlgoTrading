# Monte Carlo Simulation - Zufallspfad-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Beispielstrategie führt eine Monte Carlo-Simulation zukünftiger Kursverläufe anhand historischer Log-Renditen durch. Sie platziert keine Trades, sondern demonstriert, wie Zufallspfade generiert und zukünftige maximale und minimale Kursniveaus geschätzt werden können.

## Details

- **Einstiegskriterien**: keine, diese Strategie handelt nicht.
- **Long/Short**: keine.
- **Ausstiegskriterien**: nicht anwendbar.
- **Stops**: keine.
- **Standardwerte**:
  - `NumberOfBarsToPredict` = 50.
  - `NumberOfSimulations` = 500.
  - `DataLength` = 2000.
  - `KeepPastMinMaxLevels` = false.
- **Filter**: nicht anwendbar.
- **Komplexität**: moderat.
- **Zeitrahmen**: konfigurierbar.

