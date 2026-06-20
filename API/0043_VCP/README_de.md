# Volatility Contraction Pattern
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die VCP-Strategie sucht nach einer Abfolge sich verengender Preisspannen. Mit jeder Kontraktion baut sich Energie für einen Ausbruch auf. Das System misst die Spannenbreite und wartet auf einen Ausbruch über das höchste Hoch oder unter das niedrigste Tief.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 166%. Am besten funktioniert es im Aktienmarkt.

Sobald eine Kontraktion beobachtet wird, löst ein Ausbruch über die jüngsten Extrempunkte hinaus einen Trade in dieser Richtung aus. Das Kreuzen des Preises mit dem gleitenden Durchschnitt wird zur Steuerung von Ausstiegen verwendet.

Dieser Ansatz zielt darauf ab, explosive Bewegungen nach einem Volatilitätssqueeze zu erfassen.

## Details

- **Einstiegskriterien**: Kontraktion der Spanne und anschließender Ausbruch über das jüngste Hoch/Tief.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt MA oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `LookbackPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Range, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

