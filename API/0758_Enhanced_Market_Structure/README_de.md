# Verbesserte Marktstruktur-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die verbesserte Marktstruktur kombiniert Swing-Hoch-/Tief-Analyse mit ATR-, RSI-, Volumen-, MACD- und EMA-Filtern. Die Strategie tritt bei Ausbrüchen oder Sweep-Umkehrungen ein, wenn mehrere Filter den Impuls bestätigen.

## Details

- **Einstiegskriterien**: Ausbruch oder Sweep eines aktuellen Swings mit Filtern
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 1 Stunde
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ATR, RSI, MACD, EMA, Volumen
  - Stops: Nein
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel

