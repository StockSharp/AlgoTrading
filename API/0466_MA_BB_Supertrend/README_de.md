# MA + BB + SuperTrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert einen gleitenden Durchschnitt-Crossover mit der SuperTrend-Bestätigung
und verwendet Bollinger Bands für Ausstiege. Eine Long-Position wird eröffnet, wenn die Signal-MA
die Basis-MA von unten kreuzt und der Preis über der SuperTrend-Linie liegt. Short-Positionen
werden beim umgekehrten Kreuzung unter einem bärischen SuperTrend eröffnet. Positionen werden
geschlossen, wenn der Preis das entfernte Bollinger Band berührt oder wenn der Preis den
SuperTrend in die entgegengesetzte Richtung kreuzt.

## Details

- **Einstiegskriterien**:
  - Signal-MA kreuzt die Basis-MA in Richtung des SuperTrend.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Berührung des gegenüberliegenden Bollinger Bands oder SuperTrend-Wechsel.
- **Stops**: SuperTrend fungiert als Trailing-Stop.
- **Standardwerte**:
  - MA-Signallänge = 89, MA-Verhältnis = 1.08.
  - BB-Länge = 30, BB-Breite = 3.
  - SuperTrend-Periode = 20, SuperTrend-Faktor = 4.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MA, Bollinger Bands, SuperTrend
  - Stops: SuperTrend
  - Komplexität: Moderat
  - Zeitrahmen: Kurz/mittel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
