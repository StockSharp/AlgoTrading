# ADX DMI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet den Directional Movement Index (DMI), um Kreuzungen zwischen den +DI- und -DI-Linien zu handeln. Wenn -DI über +DI steigt und dann darunter fällt, eröffnet die Strategie eine Long-Position. Wenn +DI über -DI steigt und dann darunter fällt, wird eine Short-Position eröffnet. Umgekehrte Signale können optional bestehende Positionen schließen.

## Details

- **Einstiegskriterien**:
  - **Long**: -DI war auf dem vorherigen Balken über +DI und kreuzt auf dem neuesten Balken darunter.
  - **Short**: +DI war auf dem vorherigen Balken über -DI und fällt auf dem neuesten Balken darunter.
- **Ausstiegskriterien**:
  - Umgekehrte Kreuzung, wenn die entsprechende Schließoption aktiviert ist.
- **Indikatoren**:
  - Directional Index (Periode standardmäßig 14)
- **Stops**: standardmäßig keine.
- **Standardwerte**:
  - `DmiPeriod` = 14
  - `AllowLong` = true
  - `AllowShort` = true
  - `CloseLong` = true
  - `CloseShort` = true
- **Filter**:
  - Funktioniert auf jedem Zeitrahmen
  - Indikatoren: DMI
  - Stops: optional über externes Risikomanagement
  - Komplexität: Grundlegend
