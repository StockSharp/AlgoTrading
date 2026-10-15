# Timer-Handel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Timer-Handel wechselt in festen Zeitintervallen zwischen Long- und Short-Positionen. Ein Timer löst Marktorders aus, und jede Position wird automatisch mit Stop-Loss und Take-Profit geschützt.

## Details

- **Einstiegskriterien**: Timer-Ereignis.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: Ja, über StartProtection.
- **Standardwerte**:
  - `TimerInterval` = TimeSpan.FromSeconds(30)
  - `Volume` = 1
  - `StopLossLevel` = 10 Punkte
  - `TakeProfitLevel` = 50 Punkte
- **Filter**:
  - Kategorie: Timer
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
