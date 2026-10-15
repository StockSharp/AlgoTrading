# RAVI + Awesome Oscillator Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Port des MetaTrader 5 Expert Advisors "Ravi AO (barabashkakvns Edition)" auf die High-Level-API von StockSharp.
- Kombiniert den Range Action Verification Index (RAVI) mit dem Awesome Oscillator (AO), um synchronisierte bullische und bärische Impulswechsel zu erkennen.
- Funktioniert auf jedem von StockSharp unterstützten Zeitrahmen und Instrument; alle numerischen Einstellungen sind in Pips ausgedrückt, um nah an der ursprünglichen Implementierung zu bleiben.

## Indikatoren
- **RAVI** – berechnet als `100 * (FastMA - SlowMA) / SlowMA` auf der ausgewählten Preisreihe. Sie können die Glättungsmethode (einfach, exponentiell, geglättet, gewichtet), die Längen und die Preisquelle wählen (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet, einfach, Quartal, Trend-Follow, Demark).
- **Awesome Oscillator** – Median-Preis-Momentum-Indikator mit konfigurierbaren kurzen und langen Perioden. Die Standardwerte entsprechen den MT5-Werten (5 und 34).

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Kerzen-Zeitrahmen oder Datentyp für das Abonnement. |
| `StopLossPips` | Schutz-Stop-Loss-Distanz in Pips. `0` deaktiviert den Stop. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. `0` deaktiviert den Take-Profit. |
| `TrailingStopPips` | Basis-Trailing-Stop-Distanz in Pips. `0` deaktiviert das Trailing. |
| `TrailingStepPips` | Minimaler zusätzlicher Gewinn (in Pips), der erforderlich ist, bevor der Trailing-Stop enger gezogen wird. Muss > 0 sein, wenn Trailing aktiviert ist. |
| `FastMethod` / `FastLength` | Glättungsmethode und Länge des schnellen RAVI-gleitenden Durchschnitts. |
| `SlowMethod` / `SlowLength` | Glättungsmethode und Länge des langsamen RAVI-gleitenden Durchschnitts. |
| `AppliedPrices` | Preisformel, die von beiden RAVI-Durchschnitten verwendet wird (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet, einfach, Quartal, Trend-Follow #1/#2, Demark). |
| `AoShortPeriod` / `AoLongPeriod` | Schnelle und langsame Perioden des Awesome Oscillators. |

## Handelsregeln
1. Die Strategie aktualisiert Indikatoren, wenn eine Kerze schließt (`CandleStates.Finished`).
2. Ein **bullischer Einstieg** wird ausgelöst, wenn:
   - AO vor zwei Balken `< 0` und AO vor einem Balken `> 0` (positiver Nulldurchgang), und
   - RAVI vor zwei Balken `< 0` und RAVI vor einem Balken `> 0`.
3. Ein **bärischer Einstieg** wird ausgelöst, wenn:
   - AO vor zwei Balken `> 0` und AO vor einem Balken `< 0`, und
   - RAVI vor zwei Balken `> 0` und RAVI vor einem Balken `< 0`.
4. Es kann nur eine Position gleichzeitig offen sein. Signale werden ignoriert, solange eine Position besteht.

## Exit-Management
- **Stop-Loss**: berechnet aus `StopLossPips` unter Verwendung des Instrument-Preisschritts (5- und 3-stellige FX-Symbole verwenden einen 10×-Multiplikator, passend zur MT5-Pip-Definition). Wird ausgelöst, wenn Kerzenextreme das Stop-Level berühren.
- **Take-Profit**: optionales Ziel, das auf dieselbe Weise berechnet wird; deaktiviert wenn `TakeProfitPips = 0`.
- **Trailing-Stop**: wenn aktiviert, wird der Stop einmal enger gezogen, wenn der schwebende Gewinn `TrailingStopPips + TrailingStepPips` übersteigt. Für Longs bewegt sich der Stop zu `ClosePrice - TrailingStopPips`; für Shorts zu `ClosePrice + TrailingStopPips`.
- Alle Ausstiege schließen die volle Position mit Marktorders.

## Implementierungshinweise
- Signale werden beim Balkenschluss ausgewertet; tatsächliche Einstiege erfolgen beim gleichen Kerzenschluss, während die MT5-Version beim nächsten Balkeneröffnung eintritt. Passen Sie die Einstellungen an, wenn Sie diesen Unterschied kompensieren müssen.
- Es werden nur StockSharp-bereitgestellte gleitende Durchschnitte verwendet; exotische Glättungsmodi aus der MT5-Bibliothek (JJMA, Jurik, T3, etc.) sind nicht verfügbar.
- Der visuelle `Shift`-Parameter des MT5-Indikators beeinflusst nur die Darstellung; er hat keine Handelswirkung und wird daher weggelassen.
- `AppliedPrices`-Formeln folgen den MetaTrader-Definitionen, einschließlich TrendFollow- und Demark-Optionen.

## Verwendungstipps
- Die Strategie ist trendverfolgend; kombinieren Sie sie mit höheren Zeitrahmen-Filtern oder Volatilitätsfiltern, um Fehlsignale zu reduzieren.
- Optimieren Sie Längen und Pip-Abstände je Instrument, besonders beim Wechsel zwischen FX, CFDs und Futures, da die Pip-Größe aus `Security.PriceStep` abgeleitet wird.
- Aktivieren Sie `Strategy.StartProtection` extern, wenn Sie broker-seitige Stop-Orders anstelle von strategie-internen Ausstiegen wünschen.
