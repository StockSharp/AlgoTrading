# Fisher Transform X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Fisher Transform-Indikator auf zwei verschiedenen Zeitrahmen. Der höhere Zeitrahmen definiert den Gesamttrend, während der niedrigere Zeitrahmen Einstiege generiert, wenn Fisher seinen vorherigen Wert gegen diesen Trend kreuzt. Optionale Parameter erlauben das Schließen von Positionen bei Trendwechsel oder bei Kreuzsignalen.

## Details

- **Einstiegskriterien**:
  - **Long**: `Trend Fisher steigt` && `Signal Fisher kreuzt seinen vorherigen Wert nach unten`
  - **Short**: `Trend Fisher fällt` && `Signal Fisher kreuzt seinen vorherigen Wert nach oben`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Optionales Schließen bei Trendumkehr
  - Optionales Schließen bei entgegengesetztem Fisher-Kreuz auf dem Signal-Zeitrahmen
- **Stops**: Take Profit und Stop Loss in Punkten
- **Standardwerte**:
  - `Trend Length` = 10
  - `Signal Length` = 10
  - `Trend Timeframe` = 6 Stunden
  - `Signal Timeframe` = 30 Minuten
  - `Take Profit` = 2000 Punkte
  - `Stop Loss` = 1000 Punkte
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Fisher Transform
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
