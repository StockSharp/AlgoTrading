# Alexav D1 Profit GBPUSD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Alexav D1 Profit GBPUSD ist ein tägliches Breakout-System, das aus dem MetaTrader 4 Expert Advisor *Alexav_d1_profit_gbpusd.mq4* konvertiert wurde. Die Strategie basiert auf täglichen GBP/USD-Kerzen und wertet die abgeschlossene Sitzung einmal täglich (Dienstag bis Freitag) aus. Die Bestätigung des Momentums erfolgt durch RSI und MACD, während volatilitätsbereinigte Stopps und gestaffelte Gewinnziele von ATR abgeleitet werden.

## Handelslogik
1. **Indikatorvorbereitung**
   - Zwei EMAs mit demselben Zeitraum werden auf die täglichen Höchst- und Tiefstpreise angewendet, um bullische und bärische Referenzniveaus zu definieren.
   - RSI mit einem 10-Perioden-Lookback misst die Dynamik. Extreme RSI-Werte blockieren vorübergehend neue Trades in diese Richtung.
   - MACD (24.05.14) liefert einen Beschleunigungsfilter durch Vergleich der letzten beiden Histogrammwerte.
   - ATR (28) stellt die Volatilitätseinheit dar, die für Stopps und Gewinnziele verwendet wird.
2. **Sitzungsfilter**
   - Für jede abgeschlossene Tageskerze von Dienstag bis Freitag wird nur eine Auswertung durchgeführt. Montags und Wochenenden werden ausgelassen.
3. **Lange Einrichtung**
   - Die vorherige Tageskerze muss über den vor zwei Sitzungen berechneten Höchstständen von EMA schließen.
   - RSI der vorherigen Sitzung muss über der Obergrenze (Standard 60), aber unter der Obergrenze (Standard 80) liegen.
   - MACD muss entweder vor zwei Sitzungen unter Null liegen oder eine ausreichende positive Beschleunigung im Vergleich zum vorherigen Wert aufweisen.
   - Wenn die vorherige Eröffnung wieder unter die EMA-Höchstwerte fällt, ermöglicht die Strategie eine neue Reihe von Käufen, nachdem der Block zurückgesetzt wurde.
4. **Kurze Einrichtung**
   - Spiegeln Sie die Logik des langen Setups wider und verwenden Sie EMA von Tiefstwerten, RSI untere Schwellenwerte (39/25) und MACD-Filter.

## Orderverwaltung
Wenn eine Einrichtung bestätigt wird, öffnet die Strategie einen Stapel von vier Marktaufträgen (jeweils unter Verwendung der Strategie `Volume`):
- **Stops**: Jede Order hat denselben Schutzstopp, der `ATR * AtrStopMultiplier` (Standard 1,6) vom Einstiegspreis entfernt ist.
- **Ziele**: Gewinnziele werden für den Auftragsindex `i` in `[0..3]` um `AtrTargetMultiplier * (1 + i / 2)` skaliert und replizieren die ursprünglichen EA-Offsets von 1,0, 1,5, 2,0 und 2,5 ATR.
- **Konfliktbehandlung**: Gegensätzliche Positionen werden abgeflacht, bevor ein neuer Stapel geöffnet wird. Durch das Auslösen einer langen Charge werden alle ausstehenden kurzen Chargen gelöscht (und umgekehrt).

Die Strategie überwacht abgeschlossene Kerzen. Wenn das Tagestief den Stop berührt, wird die entsprechende Long-Order zum Marktwert geschlossen; Erreicht das Hoch das Ziel, wird die Order ebenfalls geschlossen. Shorts werden symmetrisch gehandhabt, wobei das Hoch der Kerze für Stopps und das Tief der Kerze für Ziele verwendet wird.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Primäre Kerzenserie, standardmäßig täglich. | 1 Tag |
| `MaPeriod` | Zeitraum von EMA, angewendet auf Höchst-/Tiefstwerte. | 6 |
| `RsiPeriod` | RSI Zeitraum für Momentumfilter. | 10 |
| `AtrPeriod` | ATR Zeitraum für Stopp-/Zielgrößenbestimmung. | 28 |
| `AtrStopMultiplier` | ATR Vielfaches für Stopps. | 1.6 |
| `AtrTargetMultiplier` | Basis ATR Vielfaches für Ziele. | 1,0 |
| `RsiUpperLevel` | Der Schwellenwert von RSI bestätigt die Aufwärtsdynamik. | 60 |
| `RsiUpperLimit` | RSI-Obergrenze, die neue Long-Positionen blockiert. | 80 |
| `RsiLowerLevel` | Der Schwellenwert von RSI bestätigt die rückläufige Dynamik. | 39 |
| `RsiLowerLimit` | RSI Boden, der neue Shorts blockiert. | 25 |
| `FastMaPeriod` | Schneller Zeitraum von EMA für MACD. | 5 |
| `SlowMaPeriod` | Langsamer Zeitraum von EMA für MACD. | 24 |
| `SignalMaPeriod` | Signalisieren Sie einen Zeitraum von EMA für MACD. | 14 |
| `MacdDiffBuy` | Mindestbeschleunigung von MACD für lange Strecken. | 0,5 |
| `MacdDiffSell` | Mindestbeschleunigung MACD für Kurzschlüsse. | 0,15 |

Stellen Sie die Strategie `Volume` auf die gewünschte Losgröße pro Auftrag ein, bevor Sie die Strategie starten.

## Notizen
- Bei der Konvertierung wird die Logik der Einzelauswertung pro Tag des ursprünglichen Expert Advisors beibehalten.
- Verwenden Sie beim Backtesting historische Tagesdaten für GBP/USD, um das beabsichtigte Verhalten zu reproduzieren.
- Schutzstopps und -ziele werden anhand abgeschlossener Kerzenextreme simuliert. Intraday-Spitzen innerhalb einer Tageskerze sind für die Strategie nicht sichtbar.
