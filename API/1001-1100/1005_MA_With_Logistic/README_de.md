# MA mit logistischer Funktion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

MA mit logistischer Funktion ist eine Strategie auf Basis gleitender Durchschnitte, die einen schnellen und einen langsamen gleitenden Durchschnitt für Einstiege verwendet und prozentuale oder logistikbasierte Ausstiege unterstützt.

## Details
- **Daten**: Preiskerzen.
- **Einstiegskriterien**:
  - **Long**: Schlusskurs > schnelle MA und schnelle MA > langsame MA.
  - **Short**: Schlusskurs < schnelle MA und schnelle MA < langsame MA.
- **Ausstiegskriterien**: Prozentziele oder logistische Wahrscheinlichkeitsschwellen.
- **Stops**: Prozentuale oder logistik-wahrscheinlichkeitsbasierte Ausstiege.
- **Standardwerte**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `MaType` = MaTypeEnum.EMA
  - `ExitType` = ExitTypeEnum.Percent
  - `TakeProfitPercent` = 20
  - `StopLossPercent` = 5
  - `LogisticSlope` = 10
  - `LogisticMidpoint` = 0
  - `TakeProfitProbability` = 0.8
  - `StopLossProbability` = 0.2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: MA
  - Komplexität: Niedrig
  - Risikolevel: Mittel
