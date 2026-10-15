# ForexLine-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ForexLine-Strategie ist ein Trendfolge-System, das vom MetaTrader-Indikator "ForexLine" abgeleitet ist. Es wendet zwei Stufen gewichteter gleitender Durchschnitte auf den Kurs an, um schnelle und langsame Linien zu erstellen. Kreuzungen zwischen diesen doppelt geglätteten Linien werden zur Bestimmung von Einstiegssignalen verwendet.

Die Strategie kauft, wenn die schnelle Linie über die langsame Linie kreuzt, und verkauft, wenn die schnelle Linie unter die langsame Linie kreuzt. Jeder gleitende Durchschnitt verwendet einen zweistufigen Glättungsprozess, der hilft, Marktlärm herauszufiltern.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle doppelt geglättete WMA kreuzt über die langsame doppelt geglättete WMA.
  - **Short**: Schnelle doppelt geglättete WMA kreuzt unter die langsame doppelt geglättete WMA.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung schließt bestehende Position.
- **Stops**: Nicht enthalten; können extern hinzugefügt werden.
- **Standardwerte**:
  - `FastLength1` = 5
  - `FastLength2` = 10
  - `SlowLength1` = 20
  - `SlowLength2` = 20
  - `CandleType` = 8-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gewichtete gleitende Durchschnitte
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
