# Anands-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Ausbruchssystem legt die Handelsrichtung anhand der Kerze des Vortages fest.
Wenn der vorherige Schlusskurs über dem Tageshoch liegt, sucht die Strategie nach Longs; liegt der Schluss unter dem Tief, wird sie bärisch.
Im 15-Minuten-Zeitrahmen beobachtet sie die letzten zwei abgeschlossenen Kerzen.
Eine Long-Position wird eröffnet, wenn die vorherige Kerze über dem Hoch von zwei Bars zuvor schließt.
Eine Short-Position wird eröffnet, wenn der vorherige Schluss unter das Tief von zwei Bars zuvor fällt.

## Details

- **Einstiegskriterien**:
  - Vortagsschluss über/unter seiner Spanne legt bullischen/bärischen Bias fest.
  - **Long**: vorheriger 15m-Schluss > Hoch zwei Bars zuvor.
  - **Short**: vorheriger 15m-Schluss < Tief zwei Bars zuvor.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Nicht definiert, umgekehrtes Signal schließt.
- **Stops**: Empfohlen auf der gegenüberliegenden Seite der Ausbruchsbar.
- **Standardwerte**:
  - `CandleType` = 15 Minuten
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Kerzen
  - Stops: Optional
  - Komplexität: Niedrig
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
