# Volatility Cluster Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet abgeschlossene 5-Minuten-Kerzen und wartet auf vollständig gebildete Indikatoren und eine Handelsfreigabe. Alle gleitenden Statistiken enthalten die Signalkerze. `C` ist ihr Schlusskurs, `M = SMA(C, 20)`, `D = Populationsstandardabweichung(C, 20)`, `U = M + 2 × D`, `L = M − 2 × D` und `A = ATR(14)` mit Wilder-Glättung der True Range.

Hohe Volatilität liegt bei `A >= 1.15 × SMA(A, 28)` vor.

Ohne Position wird bei bestätigter hoher Volatilität und `C >= U` gekauft beziehungsweise bei `C <= L` verkauft. Das Einstiegsvolumen ist `Volume`. Ein neuer Einstieg ist erst nach der Wartefrist und ohne ausstehende Order erlaubt. Für den Einstieg muss `A > 0` gelten.

`k = StopMultiplier`. Schlusskurs und ATR der Einstiegskerze werden festgehalten: Long-Stop `C_entry − k × A_entry`, Short-Stop `C_entry + k × A_entry`. Auf jeder abgeschlossenen Kerze wird die gesamte tatsächliche Position per Market-Order geschlossen, wenn der Schlusskurs den festen Stop erreicht, für Long `C <= M`, für Short `C >= M` gilt oder die hohe Volatilität endet.

Nach einer Order sind neue Einstiege für die folgenden 60 abgeschlossenen Kerzen gesperrt; Ausstiege bleiben aktiv. Die Stop-Prüfung erfolgt zum Kerzenschluss, die Market-Order wird zum verfügbaren Preis ausgeführt. Der Stop-Abstand bleibt während der Position fest. Es gibt weder Take-Profit noch Trailing-Stop.

## Standardparameter

```text
PriceAvgPeriod = 20
StdDevMultiplier = 2
AtrPeriod = 14
StopMultiplier = 2
CooldownBars = 60
CandleType = TimeSpan.FromMinutes(5).TimeFrame()
```
