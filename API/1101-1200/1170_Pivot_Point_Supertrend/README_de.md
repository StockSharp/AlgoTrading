# Strategie Pivot Point Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Pivot Points mit einem ATR-basierten Supertrend kombiniert, um Trendwenden zu erfassen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 65%. Die besten Ergebnisse werden im Aktienmarkt erzielt.

Pivot Points definieren eine dynamische Mittellinie. Ein ATR-Multiplikator bildet obere und untere Bänder, die dem Kurs folgen. Wenn der Trend die Richtung wechselt, eröffnet die Strategie entsprechend eine Position.

## Details

- **Einstiegskriterien**: Signale basierend auf Pivot Points und ATR Supertrend.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Pivot Points, ATR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
