# I4 DRF v2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die I4 DRF v2-Strategie verwendet den benutzerdefinierten Indikator i4_DRF_v2, der die Anzahl der Auf- und Abwärtsschlusskurse über ein gleitendes Fenster zählt.
Abhängig vom TrendModes-Parameter kann sie im Gegentrendmodus (Direct) oder im trendfolgen Modus (NotDirect) arbeiten.
Die Strategie öffnet und schließt Positionen, wenn der Indikator sein Vorzeichen wechselt, und unterstützt optionalen Stop-Loss und Take-Profit in Preisschritten.

## Details

- **Einstiegskriterien**: Vorzeichenwechsel des Indikators gemäß TrendModes
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop-Loss/Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `Period` = 11
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `TrendModes` = Direct
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Benutzerdefiniert
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
