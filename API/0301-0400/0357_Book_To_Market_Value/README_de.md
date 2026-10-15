# Strategie des Buchwert-zu-Marktwert-Verhältnisses
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Book-to-Market Value**-Strategie demonstriert die Einrichtung von Universum-Parametern und die Tageskerzen-Subscription für den Book-to-Market-Faktor.
Dieses Beispiel ist ein Platzhalter und enthält derzeit keine Handelslogik.

## Details
- **Einstiegskriterien**: Faktorlogik nicht implementiert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Keine.
- **Stops**: Nein.
- **Standardwerte**:
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Fundamental
  - Richtung: Beide
  - Indikatoren: Fundamentals
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
