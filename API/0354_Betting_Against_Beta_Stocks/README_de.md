# Wetten gegen Beta bei Aktien
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Betting Against Beta Stocks**-Strategie geht long beim untersten Beta-Dezil eines Aktienuniversums und short beim höchsten Beta-Dezil. Die Neugewichtung erfolgt am ersten Handelstag jedes Monats.

Der Ansatz zielt darauf ab, die Anomalie auszunutzen, dass Aktien mit niedrigem Beta risikobereinigt tendenziell besser abschneiden. Es wird angenommen, dass ein Referenzwert für Beta-Berechnungen verfügbar ist.

## Details
- **Einstiegskriterien**: Monatliche Auswahl von Aktien mit niedrigem/hohem Beta.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Positionen werden bei der nächsten Neugewichtung angepasst.
- **Stops**: Keine explizite Stop-Logik.
- **Standardwerte**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filter**:
  - Kategorie: Statistisch
  - Richtung: Beide
  - Indikatoren: Beta
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
