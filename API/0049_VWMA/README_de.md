# VWMA Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der volumengewichtete gleitende Durchschnitt (VWMA) betont Preisniveaus mit höherem Handelsvolumen. Diese Strategie handelt Kreuzungen zwischen dem Preis und dem VWMA.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 184%. Am besten funktioniert es im Kryptomarkt.

Ein Schlusskurs über dem VWMA nach einer Phase darunter erzeugt ein Long-Signal, während ein Rückfall unter den VWMA einen Short-Trade auslöst. Positionen enden, wenn der Preis wieder in die entgegengesetzte Richtung kreuzt.

Die Verwendung eines volumengewichteten Durchschnitts reduziert das Rauschen aus Perioden mit niedrigem Volumen.

## Details

- **Einstiegskriterien**: Preis kreuzt VWMA von unten oder oben.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umgekehrtes Kreuz oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `VWMAPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: VWMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

