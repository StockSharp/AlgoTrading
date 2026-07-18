# Multi-Schritt FlexiSuperTrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein SuperTrend-Filter kombiniert mit einem geglätteten Abweichungs-Oszillator.
Die Strategie umfasst drei konfigurierbare Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**:
  - Preis unter SuperTrend und Abweichung (SMA des Preises minus SuperTrend) > 0 → kaufen.
  - Preis über SuperTrend und Abweichung < 0 → verkaufen.
- **Long/Short**: Long, Short oder beide Richtungen.
- **Ausstiegskriterien**:
  - Partieller Take-Profit auf 3 Niveaus.
  - Verbleibende Position beim Trendwechsel geschlossen, wenn der Preis den SuperTrend kreuzt.
- **Stops**: Standardmäßig keine Stop-Logik.
- **Standardwerte**:
  - ATR-Periode = 10.
  - ATR-Faktor = 3.0.
  - SMA-Länge = 10.
  - Take-Profit-Niveaus = 2%, 8%, 18%.
  - Take-Profit-Prozentsätze = 30%, 20%, 15%.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend, SMA
  - Stops: Take-Profit
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
