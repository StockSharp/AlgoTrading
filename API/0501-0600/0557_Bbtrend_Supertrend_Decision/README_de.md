# BBTrend SuperTrend Decision

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Zwei Bollinger-Bänder werden aus abgeschlossenen Schlusskursen berechnet: 20 und 50 Kerzen, jeweils zwei Populationsstandardabweichungen. Mit unteren Bändern `Ls/Ll`, oberen `Us/Ul` und kurzem Mittelwert `Ms` gilt: `BBTrend = 100 * (abs(Ls-Ll) - abs(Us-Ul)) / Ms`.

SuperTrend arbeitet auf diesem Wert. Vorheriger/aktueller Wert bilden synthetische Eröffnung/Schluss und ihr Minimum/Maximum die Extreme. True Range ist die absolute Änderung; ATR beginnt mit zehn gemittelten Bereichen und wird danach nach Wilder geglättet. Bänder liegen am synthetischen Mittelpunkt mit Abstand `7 * ATR`; bisherige Grenzen werden bis zum Durchbruch nachgezogen. Beide Bollinger und ATR müssen gebildet sein.

Aufwärtstrend setzt das Ziel `+Volume`, Abwärtstrend `-Volume`. Eine Order über die Differenz dreht die gesamte Position. `TradeDirection = Buy/Sell` beschränkt Einstiege; das Gegensignal schließt trotzdem die bestehende Position. `null` erlaubt beide Seiten. Offene Orders verhindern weitere Aufträge.

`TpSlCondition` wählt `None`, `TP`, `SL`, `Both` (Python 0/1/2/3), standardmäßig ohne Schutz. Aktivierte Abstände werden als Prozentwerte durch `TakeProfitPerc` und `StopLossPerc` festgelegt.

Der native lokale Schutz verwendet Kerzen und abonnierte Preisaktualisierungen des Instruments. Abstände beziehen sich auf tatsächliche Einstiegsausführungen. Ausstiege sind Marktorders; der Ausführungspreis hängt vom verfügbaren Markt und Preisraster ab.

## Parameter

| Parameter | Standardwert |
|---|---|
| `ShortBbLength` | `20` |
| `LongBbLength` | `50` |
| `StdDev` | `2` |
| `SupertrendLength` | `10` |
| `SupertrendMultiplier` | `7` |
| `TradeDirection` | `null` |
| `TpSlCondition` | `None` |
| `TakeProfitPerc` | `30` |
| `StopLossPerc` | `20` |
| `CandleType` | `5m` |
