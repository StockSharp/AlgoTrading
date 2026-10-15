# MA-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MA-Kanal-Strategie handelt Ausbrüche aus einem gleitenden Durchschnittskanal, der aus den Hoch- und Tiefpreisen aufgebaut ist. Eine Position wird eröffnet, wenn der Preis den Kanal in der entsprechenden Richtung verlässt, und umgekehrt, wenn sich der Trend dreht. Die Kanalgrenzen werden aus exponentiellen gleitenden Durchschnitten mit einem festen Versatz berechnet.

Das System ist für Long- und Short-Handel ausgelegt und reagiert nur auf abgeschlossene Kerzen. Ziel ist es, Trendwenden frühzeitig zu erfassen und dabei Rauschen innerhalb des Kanals zu vermeiden.

## Details

- **Einstiegskriterien**:
  - **Long**: Preis bricht über den oberen Kanal aus.
  - **Short**: Preis bricht unter den unteren Kanal aus.
- **Ausstiegskriterien**:
  - Ein entgegengesetzter Ausbruch löst eine Umkehr der Position aus.
- **Indikatoren**: Exponentielle gleitende Durchschnitte von Hoch- und Tiefpreisen mit konfigurierbarer Länge und Preisversatz.
- **Stops**: Standardmäßig nicht verwendet; Trades werden nur bei entgegengesetzten Signalen geschlossen.
- **Standardwerte**:
  - `Length` = 8
  - `Offset` = 10
  - `CandleType` = 1-Stunden-Kerzen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
