# Beta-gewichtete MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Beta Weighted MA (BWMA)-Strategie verwendet eine Beta-Verteilung, um aktuelle Preise zu gewichten, und erzeugt einen gleitenden Durchschnitt, dessen Verzögerung und Glättung mit den Alpha- und Beta-Parametern angepasst werden können. Die Strategie eröffnet eine Long-Position, wenn der Kurs die BWMA nach oben kreuzt, und eine Short-Position, wenn er sie nach unten kreuzt.

## Details

- **Einstiegskriterien**:
  - Kurs kreuzt die Beta Weighted Moving Average nach oben → Long eröffnen.
  - Kurs kreuzt die Beta Weighted Moving Average nach unten → Short eröffnen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Kreuzen schließt die aktuelle Position und eröffnet die umgekehrte.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 50
  - `Alpha` = 3
  - `Beta` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: Beta Weighted Moving Average
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
