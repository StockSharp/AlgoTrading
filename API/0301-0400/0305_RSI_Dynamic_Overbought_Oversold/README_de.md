# RSI-Strategie mit dynamischen Überkauft-/Überverkauft-Niveaus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **RSI Dynamic Overbought Oversold**-Strategie basiert auf dem RSI mit dynamischen Überkauft-/Überverkauft-Niveaus.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 178%. Sie funktioniert am besten am Aktienmarkt.

### Tatsächlich ausgeführte Handelsregeln (C# und Python)

Auf **abgeschlossenen 5-Minuten-Kerzen** werden RSI(14), Preis-SMA(50), SMA(50) und Standardabweichung(50) **des RSI** ermittelt. Dynamische Bänder: `upper = min(RSI_MA + 2 * RSI_STD, 85)`, `lower = max(RSI_MA - 2 * RSI_STD, 15)`. Aus Flat nur bei `RSI <= lower` und `Close >= Price_SMA * 0.995` kaufen, nur bei `RSI >= upper` und `Close <= Price_SMA * 1.005` verkaufen. Long schließen bei `RSI >= RSI_MA` oder `Close < Price_SMA * 0.995`; Short schließen bei `RSI <= RSI_MA` oder `Close > Price_SMA * 1.005`. Nach einer Order **48 abgeschlossene Kerzen** Pause. Ausgeführte Positionen erhalten einen **nativen Stop-Loss von 2%**, keinen ATR-Stop. Die oben genannte Rendite ist durch BTC/TON nicht belegt.



## Details
- **Einstiegskriterien**: Implementierung für Indikatorbedingungen prüfen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop-Logik.
- **Stops**: Ja, mit indikatorbasierten Berechnungen.
- **Standardwerte**:
  - `RsiPeriod = 14`
  - `MovingAvgPeriod = 50`
  - `StdDevMultiplier = 2.0m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Overbought, Oversold
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
