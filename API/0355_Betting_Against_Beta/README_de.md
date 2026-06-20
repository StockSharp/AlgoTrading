# Wetten gegen Beta-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Betting Against Beta**-Strategie geht long bei den Vermögenswerten mit dem niedrigsten Beta und short bei jenen mit dem höchsten Beta. Die Betas werden
gegenüber einer Benchmark über ein gleitendes Fenster berechnet, und das Portfolio wird am ersten Handelstag jedes
Monats neu gewichtet.

## Details
- **Einstiegskriterien**: Universum nach Beta relativ zur Benchmark einordnen; long im untersten Dezil, short im höchsten Dezil.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Positionen werden bei der nächsten monatlichen Neugewichtung angepasst.
- **Stops**: Keine explizite Stop-Logik.
- **Standardwerte**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filter**:
  - Kategorie: Faktor
  - Richtung: Beide
  - Indikatoren: Statistisch
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
