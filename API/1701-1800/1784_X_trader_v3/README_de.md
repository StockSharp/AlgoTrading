# X Trader V3-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Kreuzungen zwischen zwei gleitenden Durchschnitten des Medianpreises. Der erste gleitende Durchschnitt ist länger und verschoben, der zweite ist kurz. Eine Long-Position wird eröffnet, wenn der erste gleitende Durchschnitt den zweiten nach unten kreuzt und für zwei Bars darunter bleibt, nachdem er vor zwei Bars darüber lag. Eine Short-Position wird beim umgekehrten Crossover eröffnet. Positionen können bei umgekehrten Signalen geschlossen werden. Der Handel ist auf ein bestimmtes Intraday-Zeitfenster begrenzt. Optionale Schutz-Stops sind verfügbar.

## Details

- **Einstiegskriterien**:
  - Medianpreis-SMA(`Ma1Period`) kreuzt Medianpreis-SMA(`Ma2Period`) nach unten und bleibt zwei Bars darunter ⇒ Kauf wenn `AllowBuy` wahr ist.
  - Medianpreis-SMA(`Ma1Period`) kreuzt Medianpreis-SMA(`Ma2Period`) nach oben und bleibt zwei Bars darüber ⇒ Verkauf wenn `AllowSell` wahr ist.
  - Kerzenzeit zwischen `StartTime` und `EndTime`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gegenteiliger Crossover wenn `CloseOnReverseSignal` wahr ist.
- **Stops**:
  - Optionaler Take-Profit und Stop-Loss in Ticks über `TakeProfitTicks` und `StopLossTicks`.
- **Standardwerte**:
  - `Ma1Period` = 16
  - `Ma2Period` = 1
  - `TakeProfitTicks` = 150
  - `StopLossTicks` = 100
- **Filter**:
  - Kategorie: Crossover
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Optional
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
