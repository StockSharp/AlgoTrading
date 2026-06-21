# Strategie für höherwertige Pivot-Punkte
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erkennt Pivot-Hochs und -Tiefs erster, zweiter und dritter Ordnung mithilfe von 3-Bar- oder 5-Bar-Pivot-Definitionen. Die Strategie ist analytisch und platziert keine Aufträge.

## Details

- **Einstiegskriterien**:
  - Keine (nur Analyse).
- **Ausstiegskriterien**:
  - Keine.
- **Indikatoren**:
  - 3-Bar- oder 5-Bar-Pivot-Detektor.
- **Stops**: Keine.
- **Standardwerte**:
  - `CandleType` = 5m
  - `UseThreeBar` = true
  - `DisplayFirstOrder` = true
  - `DisplaySecondOrder` = true
  - `DisplayThirdOrder` = true
- **Filter**:
  - Einzelner Zeitrahmen
  - Indikatoren: Pivot-Detektor
  - Stops: keine
  - Komplexität: Niedrig
