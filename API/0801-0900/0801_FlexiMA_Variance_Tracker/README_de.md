# FlexiMA Varianz-Tracker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verfolgt die Preisabweichung um einen gleitenden Durchschnitt und eröffnet Trades, wenn die Abweichung einen Volatilitätsschwellenwert überschreitet und die Supertrend-Richtung dies bestätigt.

## Details

- **Einstiegskriterien**:
  - Preis über Supertrend und Abweichung > Durchschnitt + Standardabweichung × Multiplikator → Kauf.
  - Preis unter Supertrend und Abweichung < -(Durchschnitt + Standardabweichung × Multiplikator) → Verkauf.
- **Long/Short**: Beide Richtungen können aktiviert werden.
- **Ausstiegskriterien**:
  - Entgegengesetzte Abweichung oder Supertrend-Umkehr.
- **Stops**: Standardmäßig keine Stop-Logik.
- **Standardwerte**:
  - MA-Länge = 20.
  - StdDev-Länge = 20.
  - StdDev-Multiplikator = 1.0.
  - ATR-Periode = 10.
  - ATR-Faktor = 3.0.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, StandardDeviation, SuperTrend
  - Stops: Keine
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
