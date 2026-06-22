# Trend Alexcud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Trend Alexcud Strategie sucht nach starker Richtungsbewegung, indem mehrere einfache gleitende Durchschnitte und der Accelerator Oscillator über drei Zeitrahmen ausgerichtet werden. Sie wurde aus dem originalen MQL5-Expert "TREND_alexcud v_2" konvertiert.

Das System beobachtet drei Zeitrahmen (Standard 15 Minuten, 1 Stunde, 4 Stunden). Auf jedem Zeitrahmen berechnet es fünf einfache gleitende Durchschnitte (Perioden 5, 8, 13, 21, 34) und den Accelerator Oscillator. Ein Zeitrahmen gilt als bullisch, wenn der Schlusskurs über allen gleitenden Durchschnitten liegt und der Accelerator positiv ist. Ein Zeitrahmen ist bearisch, wenn der Schlusskurs unter allen gleitenden Durchschnitten liegt und der Accelerator negativ ist.

Ein Trade wird nur eröffnet, wenn alle drei Zeitrahmen übereinstimmen. Sind sie gleichzeitig bullisch, kauft die Strategie; eine gemeinsame bearische Lesung löst einen Verkauf aus. Die Position wird umgekehrt, wenn das entgegengesetzte Signal erscheint. Schutzorders werden über das integrierte Risikomanagementsystem von StockSharp verwaltet.

## Details

- **Einstiegskriterien**
  - **Long**: Preis über allen MAs und Accelerator > 0 auf jedem Zeitrahmen.
  - **Short**: Preis unter allen MAs und Accelerator < 0 auf jedem Zeitrahmen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Position wird umgekehrt, wenn das entgegengesetzte Signal entsteht.
- **Stops**: Verwendet integrierten Schutz (keine Standardwerte).
- **Standardwerte**:
  - Timeframe1 = 15m, Timeframe2 = 1h, Timeframe3 = 4h
  - MA-Perioden = 5, 8, 13, 21, 34
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
