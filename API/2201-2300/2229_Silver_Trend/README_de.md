# Silver Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Trendfolge-Strategie basierend auf dem benutzerdefinierten SilverTrend-Indikator. Der Indikator baut einen dynamischen Preiskanal mithilfe des höchsten Hochs und niedrigsten Tiefs über ein Lookback-Fenster und einem Risikofaktor auf. Ein Handelssignal tritt auf, wenn der Kurs den Kanal kreuzt und die Trendrichtung sich umkehrt.

## Details

- **Einstieg**: Kaufen, wenn der Indikator in einen Aufwärtstrend wechselt. Verkaufen, wenn der Indikator in einen Abwärtstrend wechselt.
- **Ausstieg**: Position kehrt sich beim entgegengesetzten Signal um.
- **Indikatoren**: Highest, Lowest, SimpleMovingAverage (innerhalb der SilverTrend-Berechnung).
- **Stops**: Keine.
- **Standardwerte**:
  - `Ssp` = 9 — Anzahl der Balken für die Kanalberechnung.
  - `Risk` = 3 — Prozentsatz, der die Kanalbreite verringert.
  - `CandleType` = 1-Stunden-Kerzen.
- **Richtung**: Sowohl Long als auch Short.

Der SilverTrend-Indikator berechnet den durchschnittlichen Hoch-Tief-Bereich über `Ssp + 1` Balken und findet das höchste Hoch und das niedrigste Tief über `Ssp` Balken. Die Kanalgrenzen sind:

```
smin = minLow + (maxHigh - minLow) * (33 - Risk) / 100
smax = maxHigh - (maxHigh - minLow) * (33 - Risk) / 100
```

Wenn der Schlusskurs unter `smin` fällt, wird der Trend bärisch. Wenn der Schlusskurs über `smax` steigt, wird der Trend bullisch. Ein Signal wird generiert, wenn der Trend kippt, und die Strategie kehrt ihre Position sofort entsprechend um.
