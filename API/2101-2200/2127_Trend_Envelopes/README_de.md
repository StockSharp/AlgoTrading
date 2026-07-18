# Trendhüllen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die auf dem TrendEnvelopes-Indikator basiert. Sie kombiniert eine EMA mit ATR-basierten Bändern, um Ausbrüche zu erkennen.
Long-Positionen werden eröffnet, wenn der Kurs das obere Band nach oben durchbricht und ein Kaufsignal erscheint. Short-Positionen werden bei Durchbrüchen unter das untere Band mit einem Verkaufssignal eröffnet. Entgegengesetzte Bänder lösen Positionsschließungen aus.

## Details

- **Einstiegskriterien**:
  - Long: Kurs schließt über dem oberen Hüllenband und erzeugt ein Kaufsignal
  - Short: Kurs schließt unter dem unteren Hüllenband und erzeugt ein Verkaufssignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegenteiliges Trendsignal
- **Stops**: Ja (Take-Profit und Stop-Loss)
- **Standardwerte**:
  - `MaPeriod` = 14
  - `Deviation` = 0.2m
  - `AtrPeriod` = 15
  - `AtrSensitivity` = 0.5m
  - `TakeProfit` = 2000 Punkte
  - `StopLoss` = 1000 Punkte
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
