# Strategie mit Modifiziertem Optimalen Elliptischen Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet den von John F. Ehlers beschriebenen *Modified Optimum Elliptic Filter*-Indikator an, um Richtungswenden zu erkennen. Der Indikator ist ein digitaler Zweipolfilter, der den Durchschnitt von Hoch- und Tiefpreisen mithilfe der folgenden rekursiven Formel glättet:

```
F(t) = 0.13785*(2*HL2(t) - HL2(t-1))
     + 0.0007 *(2*HL2(t-1) - HL2(t-2))
     + 0.13785*(2*HL2(t-2) - HL2(t-3))
     + 1.2103 * F(t-1) - 0.4867 * F(t-2)
```

Dabei ist `HL2` der Mittelpunkt `(High + Low)/2` jeder Kerze.

Die Strategie liest die letzten drei Filterwerte, um das Momentum zu bestimmen. Wenn der Indikator steigt und der neueste Wert den vorherigen überschreitet, wird eine Long-Position eröffnet. Wenn der Indikator fällt und der aktuelle Wert unter dem vorherigen liegt, wird eine Short-Position eröffnet. Positionen werden umgekehrt, wenn die entgegengesetzte Bedingung eintritt.

## Details

- **Einstiegskriterien**:
  - **Long**: `F(t-1) < F(t-2)` und `F(t) > F(t-1)`.
  - **Short**: `F(t-1) > F(t-2)` und `F(t) < F(t-1)`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Position wird beim entgegengesetzten Signal umgekehrt.
- **Stops**: Keine expliziten Stops.
- **Standardwerte**:
  - `Candle Type` = 4-Stunden-Zeitrahmen.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
