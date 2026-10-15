# MA-nach-MA-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert einen doppelt geglätteten gleitenden Durchschnitt-Crossover.
Die Preisreihe wird durch einen schnellen exponentiellen gleitenden Durchschnitt (EMA) geglättet.
Das Ergebnis des schnellen EMA wird dann erneut durch einen langsameren EMA geglättet.
Die beiden Reihen werden verglichen, um Signale zu generieren:
- Eine Long-Position wird eröffnet, wenn der schnelle EMA den langsamen EMA nach oben kreuzt.
- Eine Short-Position wird eröffnet, wenn der schnelle EMA den langsamen EMA nach unten kreuzt.
Jede bestehende entgegengesetzte Position wird beim Crossover geschlossen.

Die Strategie funktioniert auf jedem Zeitrahmen von Kerzen.

## Parameter
- `FastLength` – Periode des schnellen EMA.
- `SlowLength` – Periode des langsamen EMA, der auf den Ausgang des schnellen EMA angewendet wird.
- `EnableLong` – Long-Positionen erlauben.
- `EnableShort` – Short-Positionen erlauben.
- `CandleType` – Kerzentyp für Berechnungen.

## Details
- **Einstiegskriterien**:
  - **Long**: schneller EMA kreuzt langsamen EMA nach oben.
  - **Short**: schneller EMA kreuzt langsamen EMA nach unten.
- **Long/Short**: Beide Richtungen unterstützt.
- **Ausstiegskriterien**:
  - Entgegengesetzter Crossover schließt eine bestehende Position.
- **Stops**: Kein expliziter Stop-Loss oder Take-Profit wird verwendet.
- **Standardwerte**:
  - `FastLength` = 7
  - `SlowLength` = 7
  - `EnableLong` = true
  - `EnableShort` = true
  - `CandleType` = 12-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Moving averages
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
