# Jpalonso Modoki-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Jpalonso Modoki-Strategie handelt einen Preiskanal, der auf einem einfachen gleitenden Durchschnitt basiert.
Obere und untere Envelopes werden durch Anwendung einer prozentualen Abweichung auf den gleitenden Durchschnitt berechnet.
Das System geht Long, wenn der Preis das untere Band berührt oder in der oberen Hälfte des Kanals bleibt.
In den umgekehrten Situationen geht es Short. Feste Take-Profit- und Stop-Loss-Niveaus schützen die Position.

## Details

- **Einstiegskriterien**: Preis unterhalb der unteren Envelope oder zwischen mittlerer und oberer Band für Longs; Preis oberhalb der oberen Envelope oder zwischen mittlerer und unterer Band für Shorts.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop-Niveaus.
- **Stops**: Ja, Take-Profit und Stop-Loss in Punkten.
- **Standardwerte**:
  - `CandleType` = 1 Minute
  - `SmaPeriod` = 200
  - `Deviation` = 0.35%
  - `TakeProfit` = 127 Punkte
  - `StopLoss` = 77 Punkte
- **Filter**:
  - Kategorie: Kanal
  - Richtung: Beide
  - Indikatoren: SMA, Envelopes
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
