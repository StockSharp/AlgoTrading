# Kauf- und Verkaufsvolumen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet die Verteilung von Kauf- und Verkaufsvolumen zur Erkennung von Druck.
Eine Long-Position wird eröffnet, wenn das Kaufvolumen dominiert und die Volumenmetrik
eine Volatilitätsband überschreitet, während der Preis oberhalb des wöchentlichen VWAP liegt. Eine Short-Position
verwendet die entgegengesetzten Bedingungen.

## Details

- **Einstiegskriterien**:
  - **Long**: Bereinigtes Kaufvolumen > bereinigtes Verkaufsvolumen, Volumenmetrik über oberem Band, close über wöchentlichem VWAP.
  - **Short**: Bereinigtes Verkaufsvolumen > bereinigtes Kaufvolumen, Volumenmetrik über oberem Band, close unter wöchentlichem VWAP.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenteiliges Signal oder ATR-basierter Take-Profit/Stop-Loss.
- **Stops**: ATR-Prozentmultiplikatoren über `ProfitTargetLong`, `StopLossLong`, `ProfitTargetShort`, `StopLossShort`.
- **Standardwerte**:
  - Length 20, StdDev 2.
  - ProfitTargetLong 100, StopLossLong 1.
  - ProfitTargetShort 100, StopLossShort 5.
- **Filter**:
  - Kategorie: Volumenbasiert
  - Richtung: Beide
  - Indikatoren: Benutzerdefiniert
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
