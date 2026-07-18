# Bronzepfannenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „Bronzew_pan“. Es handelt ein einzelnes Instrument auf fertige Kerzen und kombiniert den proprietären DayImpuls-Oszillator mit Williams %R und dem Commodity Channel Index (CCI), um Momentumumkehrungen zu erkennen.

## Wie es funktioniert

1. Abonniert den konfigurierten Kerzentyp und führt DayImpuls, Williams %R und CCI mit demselben Zeitraum aus.
2. Führt eine unabhängige Abrechnung von Long- und Short-Engagements durch, um das ursprüngliche Absicherungsverhalten nachzubilden.
3. Schließt alle Positionen, sobald der variable Gewinn `ProfitTarget` erreicht oder unter `LossTarget` fällt.
4. Öffnet einen Short, wenn DayImpuls über `DayImpulsShortLevel` bleibt und abnimmt, während Williams %R über `WilliamsLevelUp` liegt und CCI `CciLevel` überschreitet.
5. Öffnet eine Long-Position, wenn DayImpuls unter `DayImpulsLongLevel` bleibt und steigt, während Williams %R unter `WilliamsLevelDown` und CCI unter `-CciLevel` liegt.
6. Wenn sich der schwebende PnL über die `PredBand`-Grenzen hinaus bewegt, sendet die Strategie einen großen Durchschnittsauftrag multipliziert mit `LotMultiplier`, um die Richtung umzukehren, was die Notfallwiederherstellungslogik von MetaTrader widerspiegelt.
7. Einzelne Stop-Loss- und Take-Profit-Werte werden für Long- und Short-Körbe anhand der in Preise umgewandelten Pip-Abstände überwacht.
8. Es werden keine neuen Geschäfte eröffnet, wenn der Kontostand unter `MinimumBalance` fällt oder wenn sowohl Long- als auch Short-Körbe aktiv sind.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Grundvolumen für Einträge. | `0.1` |
| `LongStopLossPips` | Stop-Loss-Distanz für lange Körbe in Pips. | `0` |
| `ShortStopLossPips` | Stop-Loss-Distanz für kurze Körbe in Pips. | `0` |
| `LongTakeProfitPips` | Take-Profit-Distanz für lange Körbe in Pips. | `0` |
| `ShortTakeProfitPips` | Take-Profit-Distanz für kurze Körbe in Pips. | `0` |
| `IndicatorPeriod` | Von DayImpuls verwendete Länge, Williams %R und CCI. | `14` |
| `CciLevel` | Absoluter Schwellenwert von CCI, der Überkauft/Überverkauft bestätigt. | `150` |
| `WilliamsLevelUp` | Williams %R-Level für Shorts erforderlich. | `-15` |
| `WilliamsLevelDown` | Williams %R-Level für Long-Positionen erforderlich. | `-85` |
| `DayImpulsShortLevel` | DayImpuls-Ebene, die kurze Einstiege ermöglicht. | `50` |
| `DayImpulsLongLevel` | DayImpuls-Ebene, die lange Eingaben ermöglicht. | `-50` |
| `ProfitTarget` | Variabler Gewinn, der jede Position schließt. | `500` |
| `LossTarget` | Floating-Loss, der jede Position schließt. | `-2000` |
| `PredBand` | Gewinnband, das zur Auslösung von Durchschnittsumkehrungen verwendet wird. | `100` |
| `LotMultiplier` | Multiplikator, der bei Umkehrungen auf das Basisvolumen angewendet wird. | `30` |
| `MinimumBalance` | Minimaler Kontostand erforderlich, um den Handel fortzusetzen. | `3000` |
| `CandleType` | Für Kerzenabonnements verwendeter Zeitrahmen. | `15m` |

## Notizen

- Der DayImpuls-Oszillator reproduziert die ursprüngliche doppelte EMA-Glättung über Kerzenkörper, ausgedrückt in Punkten.
- Stop-Loss- und Take-Profit-Werte sind optional; Die Einstellung `0` deaktiviert die entsprechende Schutzseite.
- Die Strategie basiert auf fertigen Kerzen und ignoriert unvollständige Balken.
