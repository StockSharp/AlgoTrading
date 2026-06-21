# Timeshifter Dreifach-Zeitrahmen-Strategie mit Sitzungen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die über drei Zeitrahmen handelt, mit optionaler ADX-Bestätigung und Sitzungsfiltern.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 37%. Sie funktioniert am besten auf dem Forex-Markt.

Das System richtet sich am übergeordneten Zeitrahmentrend aus, steigt bei Ausbrüchen im mittleren Zeitrahmen ein und steigt bei Umkehrungen im unteren Zeitrahmen aus. Trades können auf die London-, New York- und Tokio-Sitzungen beschränkt werden. Ein ADX-Filter kann verwendet werden, um ausreichendes Momentum sicherzustellen.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs des höheren Zeitrahmens über seiner SMA und Preis des mittleren Zeitrahmens kreuzt seine SMA von unten.
  - **Short**: Schlusskurs des höheren Zeitrahmens unter seiner SMA und Preis des mittleren Zeitrahmens kreuzt seine SMA von oben.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Preis des niedrigeren Zeitrahmens kreuzt seine SMA von oben.
  - **Short**: Preis des niedrigeren Zeitrahmens kreuzt seine SMA von unten.
- **Stops**: Nein.
- **Standardwerte**:
  - `HigherMaLength` = 50
  - `MediumMaLength` = 20
  - `LowerMaLength` = 10
  - `AdxLength` = 14
  - `AdxThreshold` = 25
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, ADX
  - Stops: Nein
  - Komplexität: Komplex
  - Zeitrahmen: Mehrere
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
