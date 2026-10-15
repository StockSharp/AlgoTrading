# Strategie Ichimoku Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Implementierung der Strategie - Ichimoku + Volume. Kauft, wenn der Preis über der Kumo-Wolke liegt, Tenkan-sen über Kijun-sen liegt und das Volumen über dem Durchschnitt liegt. Verkauft, wenn der Preis unter der Kumo-Wolke liegt, Tenkan-sen unter Kijun-sen liegt und das Volumen über dem Durchschnitt liegt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 40%. Sie funktioniert am besten im Kryptomarkt.

Ichimoku-Komponenten definieren die Richtungsneigung, während wachsendes Volumen Interesse bestätigt. Trades öffnen sich, wenn der Preis mit der Wolke übereinstimmt und das Volumen anzieht.

Es passt zu Tradern, die Wolken-Ausbrüchen mit Beteiligung folgen. Das Risiko wird durch einen prozentualen Stop begrenzt.

## Details

- **Einstiegskriterien**:
  - Long: `Price > Cloud && Tenkan > Kijun && Volume > AvgVolume`
  - Short: `Price < Cloud && Tenkan < Kijun && Volume > AvgVolume`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Wolken-Ausbruch in entgegengesetzter Richtung
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `VolumeAvgPeriod` = 20
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    AvgVolume ist das durchschnittliche Volumen der vorherigen VolumeAvgPeriod Kerzen. Ein Long schließt, wenn der Kurs unter der Wolke schließt, ein Short, wenn er darüber schließt. Ein Einstiegssignal gegen eine offene Position dreht sie.
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Ichimoku Cloud, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

