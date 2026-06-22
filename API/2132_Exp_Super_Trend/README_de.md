# Exp Super Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie konvertiert aus dem MQL-Skript **Exp_Super_Trend.mq5** (ID 14269). Sie folgt der Richtung des SuperTrend-Indikators und kehrt Positionen um, sobald der Trend wechselt. Die Implementierung verwendet die High-Level StockSharp API und den integrierten SuperTrend-Indikator.

Der Indikator berechnet eine dynamische Support- oder Widerstandslinie basierend auf ATR. Wenn der Preis über dieser Linie bleibt, gilt der Trend als bullisch, andernfalls als bärisch. Die Strategie eröffnet während bullischer Phasen eine Long-Position und wechselt in bärischen Phasen zur Short-Position. Jeder Indikatorwechsel bewirkt eine sofortige Positionsumkehr.

Dieser Ansatz funktioniert am besten in Trendmärkten, wo nach einem Ausbruch große Bewegungen folgen. Er eignet sich auch als Lehrvorlage, die zeigt, wie man einen Indikator mit `BindEx` verbindet und Marktaufträge auf abgeschlossenen Kerzen ausführt.

## Details

- **Einstiegskriterien**:
  - Long: SuperTrend signalisiert einen Aufwärtstrend.
  - Short: SuperTrend signalisiert einen Abwärtstrend.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal von SuperTrend (Position wird umgekehrt).
- **Stops**: Kein expliziter Stop-Loss; die Indikatorlinie wirkt als Trailing Stop.
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SuperTrend
  - Stops: Indikatorbasiert
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittel (standardmäßig 1 Stunde)
  - Saisonalität: Keine
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
