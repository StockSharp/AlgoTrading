# MicuRobert EMA Cross Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet zwei Zero Lag Exponential Moving Averages (ZLEMA), um Crossovers zu handeln. Der Handel kann auf eine bestimmte Session eingeschränkt werden; optional wird ein Trailing-Stop verwendet.

## Details

- **Einstiegskriterien:**
  - **Long:** schnelle ZLEMA kreuzt über langsame ZLEMA, oder Preis kreuzt über schnelle ZLEMA, während die schnelle über der langsamen liegt.
  - **Short:** schnelle ZLEMA kreuzt unter langsame ZLEMA, oder Preis kreuzt unter schnelle ZLEMA, während die schnelle unter der langsamen liegt.
- **Ausstiegskriterien:** Positionen schließen bei Trailing-Stop oder festen Stop-Loss- und Take-Profit-Niveaus.
- **Stops:** optionaler Trailing-Stop mit festem Take-Profit und Stop-Loss.
- **Filter:** Sitzungszeitfilter.
