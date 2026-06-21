# EMA SAR Power-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Intraday-Strategie kombiniert schnelle und langsame exponentielle gleitende Durchschnitte mit Parabolic SAR und Bulls/Bears-Power-Indikatoren. Sie handelt nur während aktiver Marktstunden und erfordert ausreichend freie Margin vor dem Einstieg in eine Position.

Das System geht short, wenn die schnelle EMA unter der langsamen EMA liegt, Parabolic SAR über dem Kerzenhoch sitzt und Bears Power steigt, während sie negativ bleibt. Es geht long, wenn die schnelle EMA über der langsamen EMA liegt, Parabolic SAR unter dem Kerzentief liegt und Bulls Power fällt, aber noch positiv ist. Jeder Trade platziert einen weiten Stop-Loss und einen näheren Take-Profit.

**Dynamischer Margin-Filter**

Vor dem Handel prüft die Strategie die freie Margin des Portfolios. Abhängig von deren Wert erhöht sich die erforderliche Mindestmarge stufenweise: 600 → 1000 → 1300 → 1500 → 1800 → 2000 → 2500. Der Handel wird übersprungen, wenn die freie Margin unter den aktuellen Schwellenwert fällt.

## Details

- **Einstiegskriterien**:
  - **Short**: `EMA3 < EMA34` && `SAR > High` && `BearsPower < 0` && `BearsPower > BearsPower[1]`.
  - **Long**: `EMA3 > EMA34` && `SAR < Low` && `BullsPower > 0` && `BullsPower < BullsPower[1]`.
- **Long/Short**: Beide Seiten.
- **Stop/Ziel**: Stop-Loss bei 2000 Punkten, Take-Profit bei 400 Punkten.
- **Zeitfilter**: Handelt nur zwischen 09:00 und 16:59 Brokerzeit.
- **Indikatoren**:
  - Exponentielle gleitende Durchschnitte (3, 34) auf Median-Preis.
  - Parabolic SAR (Schritt 0.02, Maximum 0.2).
  - Bulls Power (13) und Bears Power (13).
- **Standardvolumen**: 30 Kontrakte.
- **Zeitrahmen**: 15-Minuten-Kerzen.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
