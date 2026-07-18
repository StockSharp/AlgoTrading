# Donchian-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Ausbruchssystem mit Donchian-Kanälen und Volatilitäts- und Volumenfiltern.

Die Strategie kauft, wenn der Preis oberhalb des oberen Donchian-Kanals schließt und der Trend durch eine EMA und einen RSI über 50 bestätigt wird. Short-Positionen werden bei Brüchen unterhalb des unteren Kanals eröffnet. Positionen werden bei einem entgegengesetzten Donchian-Signal oder beim Auslösen eines ATR-basierten Stops geschlossen.

## Details

- **Einstiegskriterien**: Donchian-Kanalausbruch mit EMA-, RSI-, Volatilitäts- und Volumenfiltern.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Ausbruch oder ATR-Stop.
- **Stops**: ATR-basiert.
- **Standardwerte**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `EmaLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Donchian, ATR, EMA, RSI, Volumen
  - Stops: ATR-Stop
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
