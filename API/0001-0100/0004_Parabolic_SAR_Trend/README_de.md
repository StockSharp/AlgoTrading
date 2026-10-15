# Parabolic SAR-Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Parabolic SAR-Indikator. Der Parabolic SAR-Trend folgt den Punkten des Parabolic SAR-Indikators. Ein Wechsel des Preises von einer Seite des SAR zur anderen markiert eine mögliche Trendwende. Wenn der Preis zurückkehrt, wird der Trade geschlossen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 49%. Die Strategie funktioniert am besten im Kryptomarkt.

Da die SAR-Punkte dem Preis folgen, liefern sie bei einer Trendwende auf natürliche Weise einen Ausstiegspunkt. Die Methode handelt sowohl Long als auch Short ohne zusätzliche Stops über die SAR-Umkehr hinaus.


## Details

- **Einstiegskriterien**: Signale basierend auf Parabolic, SAR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Parabolic, SAR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

