# FlexiSuperTrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert einen Supertrend-Filter mit einem geglätteten Abweichungsoszillator.
Eine Position wird eröffnet, wenn der Preis mit der Supertrend-Richtung übereinstimmt und der
Oszillator den Momentum bestätigt.

## Details

- **Einstiegskriterien**:
  - Preis über Supertrend und Abweichung (SMA von Preis minus Supertrend) > 0 → Kauf.
  - Preis unter Supertrend und Abweichung < 0 → Verkauf.
- **Long/Short**: Beide Richtungen können aktiviert werden.
- **Ausstiegskriterien**:
  - Trendumkehr, wenn der Preis die Supertrend-Linie kreuzt.
- **Stops**: Standardmäßig keine Stop-Logik.
- **Standardwerte**:
  - ATR-Periode = 10.
  - ATR-Faktor = 3.0.
  - SMA-Länge = 10.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend, SMA
  - Stops: Keine
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
