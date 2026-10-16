# DEMA-RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Kreuzungen zwischen dem RSI eines doppelten exponentiellen gleitenden Durchschnitts und seinem geglätteten Wert. Eine Long-Position wird eröffnet, wenn der RSI die geglättete Linie nach oben kreuzt, und eine Short-Position bei der entgegengesetzten Kreuzung. Positionen können durch Take-Profit, Trailing-Stop und einen optionalen Handelssitzungsfilter geschützt werden.

## Parameter
- Kerzentyp
- MA-Länge
- RSI-Länge
- RSI-Glättungslänge
- Take-Profit-Punkte
- Trailing-Stop-Punkte
- Sitzung verwenden
- Sitzungsbeginn
- Sitzungsende

### Tatsächliche RSI(DEMA)-Regeln

Standard: `CandleType=1m`, `MaLength=20`, `RsiLength=14`, `RsiSmoothingLength=5`, `TakeProfitPoints=0`, `TrailStopPoints=0`, `UseSession=false`, `SessionStart=09:00`, `SessionEnd=17:00`. Bei abgeschlossener Kerze: `EMA1=EMA(Close,20)`, `EMA2=EMA(EMA1,20)`, `DEMA=2×EMA1−EMA2`, `R=RSI(DEMA,14)`, `S=SMA(R,5)`. Kauf beim Wechsel von vorher `R <= S` zu jetzt `R > S`, Verkauf beim Wechsel von `R >= S` zu `R < S`, auch als Richtungswechsel mit `Volume + abs(Position)`. Bei `UseSession=true` gelten Einstiege nur in `[SessionStart,SessionEnd)`, auch über Mitternacht; Schutz-Exits sind unabhängig. `TakeProfitPoints` und `TrailStopPoints` werden in **minimalen Preisticks** gemessen; positive Werte aktivieren nativen Take-Profit und Trailing-Stop mit absolutem Preisabstand, Null deaktiviert die jeweilige Seite. Die ursprünglichen Standardzahlen fehlten im README und sind hier festgelegt. Der vorherige Preis-EMA120/450-Cross war nicht dieser Algorithmus.

