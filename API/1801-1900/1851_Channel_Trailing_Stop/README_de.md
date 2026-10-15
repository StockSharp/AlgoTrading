# Kanal-Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie mit Donchian-Kanal-Ausbruchseinstiegen und Trailing-Stop-Management.

Das System eröffnet Trades, wenn der Kurs außerhalb des Kanals schließt. Ein Trailing-Stop verfolgt die entgegengesetzte Seite des Kanals plus Offset. Optionales "Noose"-Trailing hält den Stop-Loss in gleichem Abstand zwischen aktuellem Kurs und Take-Profit. Ausstehende Orders können nach Ausführungen gelöscht werden.

## Details

- **Einstiegskriterien**: Schlusskurs außerhalb des Kanalbereichs.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Trailing-Stop oder entgegengesetztes Signal.
- **Stops**: Trailing-Stop, optionaler Noose.
- **Standardwerte**:
  - `TrailPeriod` = 5
  - `TrailStop` = 50
  - `UseNooseTrailing` = true
  - `UseChannelTrailing` = true
  - `DeletePendingOrders` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Donchian Channel
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
