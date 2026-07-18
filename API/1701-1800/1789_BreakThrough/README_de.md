# Durchbruch-Strategie (BreakThrough)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die BreakThrough-Strategie führt Trades aus, wenn der Preis benutzerdefinierte Trendlinienlevel kreuzt.
Zwei Hauptlevel werden verwendet:
- **Buy Line** – Preisniveau zum Auslösen einer Long-Position.
- **Sell Line** – Preisniveau zum Auslösen einer Short-Position.

Sobald eine Linie von der gegenüberliegenden Seite gekreuzt wird, tritt die Strategie in dieser Richtung in den Markt ein.
Optionale zusätzliche Linien ermöglichen das Schließen einer Position, wenn der Preis ein bestimmtes Niveau berührt.
Schutz-Stop-Loss, Take-Profit und Trailing-Stop-Abstände werden in Pips vom Einstiegspreis gemessen.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Preis kreuzt die Buy Line je nach Ausgangsposition nach oben oder unten.
  - **Short**: Der Preis kreuzt die Sell Line je nach Ausgangsposition nach oben oder unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Preis trifft eine optionale Take-Profit- oder Stop-Loss-Linie.
  - Preis erreicht die Take-Profit- oder Stop-Loss-Distanz in Pips.
  - Trailing Stop wird ausgelöst.
- **Stops**: Ja, mittels `StopLossPips`, `TakeProfitPips` und `TrailingStopPips`.
- **Standardwerte**:
  - `BuyLinePrice` = 0 (deaktiviert)
  - `SellLinePrice` = 0 (deaktiviert)
  - `TakeProfitPips` = 100
  - `StopLossPips` = 30
  - `TrailingStopPips` = 20
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig (Standard 1 Minute)
  - Risikolevel: Mittel
