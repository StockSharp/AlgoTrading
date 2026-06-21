# Outside-Bar-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche aus Outside Bars. Ein bullischer Outside Bar entsteht, wenn das Hoch der aktuellen Kerze über dem vorherigen Hoch und ihr Tief unter dem vorherigen Tief liegt. Orders werden innerhalb der Bar platziert, mit optionaler Teilgewinnmitnahme und Breakeven-Stop-Verschiebung.

## Details

- **Einstiegskriterien**: Outside Bar mit bullischer oder bärischer Klassifizierung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit aus der Bar-Range abgeleitet.
- **Stops**: Ja.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `EntryPercentage` = 0.5
  - `TpPercentage` = 1
  - `PartialRR` = 1
  - `PartialExitPercent` = 0.5
  - `StopLossOffset` = 10
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: Candlestick
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
