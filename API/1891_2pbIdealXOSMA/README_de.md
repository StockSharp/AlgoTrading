# 2pbIdeal XOSMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Übersetzung des MQL5-Expertenberaters **Exp_2pbIdealXOSMA**. Sie analysiert die Steigung des MACD-Histogramms, um den Marktimpuls zu bestimmen. Wenn das Histogramm über zwei aufeinanderfolgende Bars steigt, geht das System eine Long-Position ein und schließt offene Shorts. Wenn das Histogramm über zwei aufeinanderfolgende Bars fällt, geht die Strategie eine Short-Position ein und schließt offene Longs.

Standardmäßig arbeitet der Algorithmus auf 4-Stunden-Kerzen, der Zeitrahmen ist jedoch konfigurierbar. Alle Trades werden zum Marktpreis ausgeführt und die Position wird umgekehrt, wenn das entgegengesetzte Signal erscheint. Im Beispiel wird kein Stop-Loss oder Take-Profit angewendet; die Risikosteuerung kann bei Bedarf extern hinzugefügt werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Das Histogramm bei Bar `t-1` liegt unter `t-2` und das aktuelle Histogramm übertrifft `t-1`.
  - **Short**: Das Histogramm bei Bar `t-1` liegt über `t-2` und das aktuelle Histogramm liegt unter `t-1`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Das entgegengesetzte Signal schließt die aktuelle Position.
- **Stops**: Keine.
- **Standardwerte**:
  - `FastPeriod` = 10
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `SignalBar` = 1
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln (MACD)
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: 4 Stunden (konfigurierbar)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
