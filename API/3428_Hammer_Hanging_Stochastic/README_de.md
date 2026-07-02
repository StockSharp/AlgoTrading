# Hammer hängend Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Experten „Expert_AH_HM_Stoch“ auf die StockSharp-Hochebene API. Es kombiniert Hammer- und Hängemann-Kerzenmuster mit der Bestätigung eines stochastischen Oszillators, um Umkehrsituationen nach längeren Bewegungen zu erfassen.

Die Strategie wartet auf eine abgeschlossene Kerze, bevor sie handelt, verwendet die stochastische Signallinie zum Filtern und schließt Positionen, wenn das Momentum die Extremzonen verlässt.

## Einzelheiten

- **Eintrittskriterien**:
  - Long: Bullische Hammerkerze und stochastischer %D (vorheriger Balken) unterhalb des überverkauften Niveaus.
  - Short: Bärische „Hanging Man“-Kerze und stochastischer %D (vorheriger Balken) über dem überkauften Niveau.
- **Lang/Kurz**: Beides.
- **Ausstiegskriterien**: Positionen schließen, wenn der stochastische %D über/unter konfigurierbare Erholungs- und Extremniveaus kreuzt.
- **Stoppt**: Aktiviert über den integrierten `StartProtection()`-Hook (standardmäßig Schutz auf Kontoebene).
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromHours(1)
  - `StochPeriodK` = 15
  - `StochPeriodD` = 49
  - `StochPeriodSlow` = 25
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `ExitLowerLevel` = 20
  - `ExitUpperLevel` = 80
  - `MaxBodyRatio` = 0,35
  - `LowerShadowMultiplier` = 2,5
  - `UpperShadowMultiplier` = 0,3
- **Filter**:
  - Kategorie: Muster + Oszillatorbestätigung
  - Richtung: Beide
  - Indikatoren: Candlestick, Stochastic
  - Stopps: Optionale Risikokontrollen über `StartProtection`
  - Komplexität: Mittelschwer
  - Zeitrahmen: Swing / Intraday (Standard: 1 Stunde)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikostufe: Moderat

## Wie es funktioniert

1. Abonniert die konfigurierte Kerzenserie und den stochastischen Oszillator mithilfe des High-Levels `BindEx` API.
2. Erkennt Hammer- und Hängende-Mann-Formationen anhand des Körper- und Schattenverhältnisses.
3. Bestätigt Eingaben mit der stochastischen %D-Linie unter Verwendung des vorherigen geschlossenen Balkenwerts.
4. Verwaltet Exits, wenn die Stochastik die überverkauften/überkauften Zonen verlässt, und spiegelt die Logik des ursprünglichen MQL-Experten wider.
5. Bietet Diagrammvisualisierung für Kerzen, stochastische und eigene Trades, wenn ein Diagrammbereich verfügbar ist.
