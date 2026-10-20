# Ichimoku Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendstrategie aus Tenkan/Kijun-Kreuzungen, Ichimoku-Wolke und einem Hurst-Filter auf logarithmischen Renditen. Signale verwenden abgeschlossene Kerzen.

## Berechnung

Tenkan und Kijun sind `(höchstes High + niedrigstes Low) / 2` ihres jeweiligen Fensters. Die unverschobene Senkou A ist ihr Mittelwert, Senkou B der Mittelpunkt über `SenkouSpanBPeriod`. Verwendet werden die kausal verschobenen Linien von StockSharp Ichimoku mit `KijunPeriod` Verschiebung und dessen Initialisierungspuffer; zukünftige Kerzen werden nicht gelesen.

Aus den letzten `HurstPeriod` Schlusskursen entstehen `n = HurstPeriod - 1` Renditen `r[i] = ln(C[i] / C[i-1])`. Mit Mittelwert `mu`, `X[k] = sum(r[i] - mu)`, `R = max(X) - min(X)` und Populationsstandardabweichung `S` gilt `H = ln(R / S) / ln(n)`. StockSharp HurstExponent verarbeitet diese Renditen ab der ersten abgeschlossenen Kerze. Mindestens elf Schlusskurse, ein vollständiges Fenster und positive Varianz sind erforderlich. Ein nichtpositiver Schlusskurs startet das Fenster neu; ohne gültigen Hurst-Wert ist kein Einstieg erlaubt.

## Handel

- **Long:** keine Position, Schlusskurs strikt über beiden Wolkenlinien, Tenkan kreuzt Kijun von `<=` nach `>`, `H > HurstThreshold`, gebildeter positiver ATR und abgelaufene Wartezeit.
- **Short:** keine Position, Schlusskurs strikt unter beiden Linien und umgekehrte Kreuzung; dieselben Hurst-, ATR- und Wartebedingungen gelten.
- **Signalausstieg:** den gesamten Long unter beiden Linien oder bei Abwärtskreuzung schließen; für Short gelten die umgekehrten Bedingungen. Ausstiege bleiben während der Wartezeit aktiv und benötigen den Hurst-Einstiegsfilter nicht.
- **Orders:** Markteinstieg mit `Volume`, Ausstieg mit der tatsächlichen Restposition. Eine noch nicht abgeschlossene Order blockiert weitere Signalorders. Ein Ausstieg schließt vor einem späteren Neueinstieg.
- **Wartezeit:** jede Signalorder startet `SignalCooldownBars` neu; nach so vielen abgeschlossenen Kerzen ist der nächste Einstieg möglich.

## Schutz

Wilder-ATR verwendet `AtrPeriod`. Jeder Einstieg fixiert `StopLossAtr * ATR` der Signalkerze als Abstand zum tatsächlichen Ausführungskurs. Lokaler Schutz verarbeitet Level1 und kann die Position zwischen Signalkerzen per Marketorder schließen. Spätere ATR-Werte verändern den Abstand nicht. `StopLossAtr = 0` deaktiviert den Schutz für den Lauf. Es gibt weder Take-Profit noch Trailing-Stop.

## Standardwerte

| Parameter | Standardwert |
|---|---|
| `TenkanPeriod` | 9 |
| `KijunPeriod` | 26 |
| `SenkouSpanBPeriod` | 52 |
| `HurstPeriod` | 100 Schlusskurse |
| `HurstThreshold` | 0.5 |
| `SignalCooldownBars` | 6 |
| `AtrPeriod` | 14 |
| `StopLossAtr` | 2 |
| `CandleType` | 15 Minuten |

Tests vergleichen jede Signalorder und ihre Ausführungen mit unabhängig berechneten Wolkenlinien und Rendite-R/S auf dem mitgelieferten BTC/TON-Archiv. Separate Läufe prüfen Hurst-Aufwärmung, feste ATR-Abstände und tatsächliche Schutz-Ausführungen innerhalb der Kerze.

Der Schutzvergleich im Archiv verwendet Tenkan/Kijun/Senkou B 15/40/70 und HurstThreshold 0.45, damit der Standardabstand 2 × ATR tatsächlich ausgelöst wird. Die Standardsignale 9/26/52 werden separat geprüft. Schutzläufe verwenden Einstiegsvolumen 3.
