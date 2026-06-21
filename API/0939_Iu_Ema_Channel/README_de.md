# IU EMA-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem TradingView-Skript "IU EMA Channel Strategy". Die Strategie handelt, wenn der Preis EMA-Kanäle kreuzt, die aus Hochs und Tiefs gebildet werden. Der Stop-Loss wird am Extrempunkt der vorherigen Kerze gesetzt und der Take Profit wird anhand eines Risiko-Ertrags-Verhältnisses berechnet.

## Details

- **Einstiegskriterien**: Schluss kreuzt über den Hoch-EMA für Long, unter den Tief-EMA für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Stop-Loss am vorherigen Kerzenextremum oder Take Profit nach Risiko-Ertrags-Verhältnis.
- **Stops**: Ja, fester Stop und Ziel.
- **Standardwerte**:
  - `EmaLength` = 100
  - `RiskToReward` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Variabel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
