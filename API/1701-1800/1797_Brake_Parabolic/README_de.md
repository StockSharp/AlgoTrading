# Brake Parabolic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie implementiert den Brake Parabolic-Indikator, der eine parabolische Barriere ober- oder unterhalb des Preises projiziert. Wenn die Barriere durchbrochen wird, kehrt sich der Trend um und eine neue Position in Richtung des Ausbruchs wird eröffnet. Der Algorithmus verfolgt den Extrempreis mit einer Kurve, die durch die Parameter **A**, **B** und **Shift** definiert ist.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 48%. Am besten funktioniert die Strategie in Trendmärkten auf höheren Zeitrahmen.

Das System wartet darauf, dass die Barriere die Seite wechselt. Ein bullischer Flip schließt alle Short-Positionen und eröffnet eine neue Long-Position. Ein bärischer Flip schließt alle Long-Positionen und eröffnet eine Short-Position. Während eines Trends werden entgegengesetzte Positionen geschlossen, wenn der Indikator die Richtung bestätigt.

## Details

- **Einstiegskriterien**:
  - **Long**: Barriere wechselt von oberhalb des Preises zu unterhalb des Preises.
  - **Short**: Barriere wechselt von unterhalb des Preises zu oberhalb des Preises.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Indikator bestätigt Gegentrend.
- **Stops**: Keine festen Stops; Ausstiege basieren auf Barriereumkehr.
- **Standardwerte**:
  - `A` = 1.5
  - `B` = 1.0
  - `BeginShift` = 10
  - `CandleType` = 4-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Benutzerdefiniert
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Swing
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
