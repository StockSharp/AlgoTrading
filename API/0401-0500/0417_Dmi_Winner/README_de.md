# DMI Winner-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

DMI Winner ist eine Trendfolge-Strategie, die auf dem Directional Movement Index
(DMI) basiert. Sie eröffnet Trades, wenn sich die `+DI`- und `-DI`-Linien kreuzen
und der Average Directional Index (ADX) über einen Schlüsselschwellenwert steigt,
was einen starken Trend signalisiert.

Ein optionaler Moving-Average-Filter hält Trades in Richtung des übergeordneten
Trends. Ein Stop-Loss kann ebenfalls aktiviert werden, um das Abwärtsrisiko zu
begrenzen, obwohl das System standardmäßig auf Signalumkehrungen für Ausstiege
setzt.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: `+DI` kreuzt über `-DI` UND `ADX` > `KeyLevel` (mit optionalem MA-Filter).
  - **Short**: `-DI` kreuzt über `+DI` UND `ADX` > `KeyLevel` (mit optionalem MA-Filter).
- **Ausstiegskriterien**: Entgegengesetzter DI-Kreuzung oder Stop-Loss wenn aktiviert.
- **Stops**: Optionaler Stop-Loss (`UseSL`).
- **Standardwerte**:
  - `DILength` = 14
  - `KeyLevel` = 23
  - `UseMA` = True
  - `UseSL` = False
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: DMI, Moving Average
  - Komplexität: Moderat
  - Risikolevel: Mittel
