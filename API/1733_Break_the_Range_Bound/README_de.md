# Strategie zum Ausbruch aus dem Seitwärtsbereich
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erkennt ruhige Marktphasen, in denen drei gleitende Durchschnitte innerhalb eines engen Bandes konvergieren. Wenn der Preis schließlich über oder unter diesen Bereich ausbricht, tritt die Strategie in Richtung des Ausbruchs ein und zielt darauf ab, den entstehenden Trend zu erfassen.

Das System beobachtet die Spanne zwischen dem schnellen, mittleren und langsamen SMA. Wenn der maximale Unterschied zwischen diesen Durchschnitten für eine bestimmte Anzahl von Bars unterhalb des konfigurierten Schwellenwerts bleibt, wird der Markt als "seitwärtsgebunden" angesehen. Das höchste Hoch und das niedrigste Tief dieses Zeitraums definieren die Ausbruchsniveaus.

Trades werden eröffnet, wenn der Preis jenseits dieser Extremwerte schließt. Positionen werden durch umgekehrte Bedingungen geschützt: Wenn der Preis in den Bereich zurückkehrt oder ein Vielfaches der Bereichsbreite als Gewinn erreicht, wird die Position geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Nach einem Bereich von `RangeLength` Bars, in dem die SMA-Spanne unter `ShakeThreshold` liegt, einsteigen wenn der Preis über das höchste Hoch des Bereichs schließt.
  - **Short**: Unter denselben Bereichsbedingungen einsteigen, wenn der Preis unter das niedrigste Tief des Bereichs schließt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - **Long**: Schließen wenn der Preis unter das Bereichstief zurückkehrt oder der Gewinn `4 * (Bereichshoch - Bereichstief)` übersteigt.
  - **Short**: Schließen wenn der Preis über das Bereichshoch zurückkehrt oder der Gewinn `4 * (Bereichshoch - Bereichstief)` übersteigt.
- **Stops**: Implizite Ausstiege basierend auf Bereichsgrenzen und Gewinnmultiplikator.
- **Standardwerte**:
  - `FastSma` = 38
  - `MidSma` = 140
  - `SlowSma` = 210
  - `ShakeThreshold` = 250
  - `RangeLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: SMA, Highest, Lowest
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
