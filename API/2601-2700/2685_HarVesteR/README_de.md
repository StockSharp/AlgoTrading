# HarVesteR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die HarVesteR-Strategie kombiniert MACD-Momentum mit zwei einfachen gleitenden Durchschnitten und einem optionalen ADX-Trendstärkefilter.
Sie sucht nach Situationen, in denen der Kurs an den gleitenden Durchschnitten entlangläuft, während der MACD kürzlich die Nulllinie gekreuzt hat, was einen potenziellen Ausbruch aus der Konsolidierung signalisiert.
Stops werden an Swing-Hochs oder -Tiefs gesetzt, die Hälfte der Position wird bei einem festen Gewinnmultiplikator geschlossen, und der Rest wird mit einem Break-even-Ausstieg geschützt, der vom schnellen gleitenden Durchschnitt gesteuert wird.

## Details

- **Einstiegskriterien**:
  - Long: `MACD > 0 && MACD history contains negative value && Close < SlowSMA && Close + Indentation > FastSMA && Close + Indentation > SlowSMA && ADX ≥ AdxBuyLevel (if enabled)`
  - Short: `MACD < 0 && MACD history contains positive value && Close > SlowSMA && Close - Indentation < FastSMA && Close - Indentation < SlowSMA && ADX ≥ AdxSellLevel (if enabled)`
- **Stop Loss**: Letztes Swing-Tief/-Hoch über `StopLookback` abgeschlossene Kerzen.
- **Teilausstieg**: Schließt die Hälfte der Position, wenn der Kurs sich `HalfCloseRatio` mal den Abstand zwischen Einstieg und Stop bewegt, und verschiebt dann den Stop auf Break-even.
- **Endgültiger Ausstieg**:
  - Long: schließt den Rest, wenn der Kurs unter `FastSMA + Indentation` fällt, nachdem der Stop auf Break-even liegt.
  - Short: schließt den Rest, wenn der Kurs über `FastSMA + Indentation` steigt, nachdem der Stop auf Break-even liegt.
- **Long/Short**: Beide Richtungen unterstützt.
- **Filter**: Optionaler ADX-Trendstärkefilter; setzen Sie `UseAdxFilter` auf `false`, um ihn zu deaktivieren.
- **Positionsmanagement**: Kehrt die Position um, indem das entgegengesetzte Signalvolumen zuzüglich des aktuellen Engagements verrechnet wird.

## Parameter

| Name | Standard | Beschreibung |
|------|----------|--------------|
| `MacdFast` | 12 | Schnelle EMA-Periode für die MACD-Differenzlinie. |
| `MacdSlow` | 24 | Langsame EMA-Periode für die MACD-Differenzlinie. |
| `MacdSignal` | 9 | Signal-EMA-Periode für die MACD-Glättung. |
| `MacdLookback` | 6 | Anzahl der zuletzt abgeschlossenen Kerzen, die auf einen MACD-Vorzeichenwechsel geprüft werden. |
| `SmaFastLength` | 50 | Länge des schnellen einfachen gleitenden Durchschnitts. |
| `SmaSlowLength` | 100 | Länge des langsamen einfachen gleitenden Durchschnitts. |
| `MinIndentation` | 10 | Versatz in Pips, der um die gleitenden Durchschnitte beim Ein- oder Ausstieg angewendet wird. |
| `StopLookback` | 6 | Swing-Hoch/Tief-Rückblick zur Initialisierung des anfänglichen Stop-Niveaus. |
| `UseAdxFilter` | false | Aktiviert den ADX-Stärkefilter für beide Richtungen. |
| `AdxBuyLevel` | 50 | Minimales ADX-Niveau, das bei aktiviertem Filter für Long-Einstiege erforderlich ist. |
| `AdxSellLevel` | 50 | Minimales ADX-Niveau, das bei aktiviertem Filter für Short-Einstiege erforderlich ist. |
| `AdxPeriod` | 14 | Periode für die ADX-Berechnung. |
| `HalfCloseRatio` | 2 | Multiplikator für den Einstieg-zu-Stop-Abstand vor der Gewinnmitnahme. |
| `Volume` | 1 | Ordervolumen für neue Einstiege (mit Verrechnung gegenläufiger Positionen). |
| `CandleType` | 1 hour | Primärer Zeitrahmen für den Aufbau von Kerzen und Indikatoren. |

## Hinweise

- `MinIndentation` wird über die Tick-Größe des Instruments in Kursabstand umgerechnet. Instrumente mit drei oder fünf Nachkommastellen erhalten eine zehnfache Anpassung zur Annäherung an Pip-Einheiten.
- Wenn `UseAdxFilter` deaktiviert ist, akzeptiert die Strategie Signale in beide Richtungen ohne Prüfung des ADX-Werts.
- Teilgewinnmitnahme und Break-even-Ausstiege werden bei jeder abgeschlossenen Kerze ausgeführt, um offene Positionen zu schützen, auch wenn keine neuen Trades erlaubt sind.
