# Martin-Strategie - Verlustfreier Ausstieg V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Martingale-Mittelungsstrategie fügt einer Long-Position hinzu, wenn der Preis um einen konfigurierten Prozentsatz vom ersten Einstieg fällt. Jede neue Order erhöht den Geldbetrag durch einen Multiplikator und passt den Durchschnittspreis an. Die Position wird geschlossen, wenn das Kerzenhoch den Durchschnittspreis plus den Take-Profit-Prozentsatz erreicht, um Ausstiege nur im Gewinn zu gewährleisten.

## Details

- **Einstiegskriterien**:
  - **Long**: `Flat` → kaufen für `Initial Cash`
  - **Hinzufügen**: `Price <= EntryPrice * (1 - PriceStep% * orderCount)` && `orderCount < MaxOrders`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - `High >= AvgPrice * (1 + TakeProfit%)`
- **Stops**: Nein
- **Standardwerte**:
  - `Initial Cash` = 100
  - `Max Orders` = 20
  - `Price Step %` = 1.5
  - `Take Profit %` = 1
  - `Increase Factor` = 1.05
- **Filter**:
  - Kategorie: Mittelung nach unten
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
