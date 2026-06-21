# Color Zerolag X10 Ma-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein vereinfachter Port des MetaTrader-Beispiels **Exp_ColorZerolagX10MA.mq5**. Sie verwendet einen Zero-Lag-exponentiellen gleitenden Durchschnitt, um Steigungsänderungen zu erkennen. Wenn der gleitende Durchschnitt nach zwei Balken des Rückgangs nach oben dreht, öffnet oder dreht die Strategie eine Long-Position um. Wenn der gleitende Durchschnitt nach dem Anstieg nach unten dreht, öffnet oder dreht sie eine Short-Position um.

Die Logik ahmt die ursprüngliche Idee nach, bei der ein kombinierter Satz von zehn geglätteten gleitenden Durchschnitten eine einzelne farbcodierte Linie erzeugt. Hier ersetzen wir diesen komplexen Indikator durch den integrierten `ZeroLagExponentialMovingAverage` von StockSharp, um die Implementierung kompakt und wiederverwendbar zu halten. Das System arbeitet mit dem ausgewählten Kerzen-Zeitrahmen und kann einzelne Aktionen (Long/Short öffnen/schließen) über Parameter aktivieren oder deaktivieren.

## Details

- **Einstiegskriterien**:
  - **Long**: `ZLEMA[t-2] > ZLEMA[t-1]` und `ZLEMA[t] > ZLEMA[t-1]`.
  - **Short**: `ZLEMA[t-2] < ZLEMA[t-1]` und `ZLEMA[t] < ZLEMA[t-1]`.
- **Long/Short**: Beide Richtungen unterstützt.
- **Ausstiegskriterien**:
  - Long-Positionen werden geschlossen, wenn ein Short-Signal erscheint und `BuyPosClose` aktiviert ist.
  - Short-Positionen werden geschlossen, wenn ein Long-Signal erscheint und `SellPosClose` aktiviert ist.
- **Stops**: Standardmäßig keine; Ausstiege basieren auf entgegengesetzten Signalen.
- **Standardwerte**:
  - `Length` = 20.
  - `CandleType` = 4-Stunden-Zeitrahmen.
  - Alle Aktionsflags (`BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose`) aktiviert.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
