# Bearish Wick Reversal

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Reine Long-Umkehrstrategie für untere Dochte abgeschlossener Minutenkerzen. Ohne Position wird `Volume` gekauft, wenn `Close < Open` und `100 * (Low - Close) / Close <= Threshold`. Der Standardwert `-1` fordert einen unteren Docht von mindestens einem Prozent des Schlusskurses.

Die UTC-Eröffnung der Kerze muss im einschließlich begrenzten Fenster `StartTime`–`EndTime` liegen. Bei aktiviertem `UseEmaFilter` werden außerdem eine gebildete `EMA(EmaPeriod)` und `Close > EMA` verlangt. Ohne Filter verzögert das EMA-Aufwärmen keinen Einstieg.

Die gesamte ausgeführte Long-Position wird geschlossen, wenn eine vollständige Kerze über dem Hoch der vorherigen vollständigen Kerze schließt. Einstiegszeitfenster und EMA verhindern keinen Ausstieg. Keine Short-Einstiege, versteckten Kerzenpausen oder automatischen Gewinn-/Verlustlimits. Eine aktive Order verhindert Duplikate.

## Parameter

| Parameter | Standardwert |
|---|---|
| `Threshold` | `-1` |
| `UseEmaFilter` | `false` |
| `EmaPeriod` | `200` |
| `CandleType` | `1m` |
| `StartTime` | `2014-01-01T00:00:00Z` |
| `EndTime` | `2099-01-01T00:00:00Z` |
