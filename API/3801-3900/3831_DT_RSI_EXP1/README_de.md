# DT RSI EXP1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Port repliziert den MT4 Expert Advisor **DT-RSI-EXP1**. Die Strategie scannt 15-minütige RSI-Schwünge, um Double-Tops oder Double-Bottoms um die 60/40-Level herum zu erkennen. Ein Long-Trade wird abgeschlossen, wenn die jüngsten RSI-Höchstwerte zurückgehen, ohne dass es zu Tiefstständen unter 40 kommt, während der 4-Stunden-Trendfilter nach unten zeigt. Shorts spiegeln die Logik mit Tiefstständen über 60 und einem Filter für steigende Trends wider. Mit jeder Position sind ein fester Stop-Loss und ein Take-Profit verbunden, und ein optionaler Trailing-Stop schützt die Gewinne. Positionen werden zwangsweise geschlossen, wenn RSI extreme 70/30-Werte erreicht, wodurch das ursprüngliche Ausstiegsverhalten kopiert wird.

## Einzelheiten

- **Eintrittskriterien**:
  - **Long**: zwei zinsbullische RSI-Höchststände, wobei der zweite über 60 liegt, keine rückläufigen Tiefststände unter 40 dazwischen, 4 Stunden EMA unter dem vorherigen Schlusskurs, RSI(1) Überschreitung der prognostizierten Nackenlinie, RSI(2) immer noch darunter, RSI(2) < 50 und RSI(0) < 55.
  - **Short**: zwei rückläufige RSI-Tiefststände, wobei der zweite unter 40 liegt, keine zinsbullischen Spitzen über 60 dazwischen, 4 Stunden EMA über dem vorherigen Schlusskurs, RSI(1) Kreuzung unter der prognostizierten Nackenlinie, RSI(2) > 50 und RSI(0) > 47.
- **Lang/Kurz**: Beide Richtungen.
- **Ausstiegskriterien**:
  - RSI Extreme (RSI > 70 für Long-Positionen, RSI < 30 für Short-Positionen).
  - Aus Preisschritten berechnete Stop-Loss-/Take-Profit-Ziele.
  - Optionaler Trailing-Stop, der Gewinne sperrt, sobald sich der Preis um `TrailingStopPoints` bewegt.
- **Stops**: Fester Stop-Loss und Take-Profit, optionaler Trailing-Stop.
- **Standardwerte**:
  - `CandleType` = 15-Minuten-Kerzen.
  - `TrendCandleType` = 240-Minuten-Kerzen (Trendfilter EMA).
  - `RsiPeriod` = 47.
  - `StopLossPoints` = 26.
  - `TakeProfitPoints` = 76.
  - `TrailingStopPoints` = 0 (deaktiviert).
- **Filter**:
  - Kategorie: Trendfolgende Einträge zu RSI-Strukturen.
  - Richtung: Beide.
  - Indikatoren: RSI, EMA Trendfilter.
  - Stoppt: Ja.
  - Komplexität: Mittelschwer (Multi-Constraint-Swing-Erkennung).
  - Zeitrahmen: Intraday (M15 mit H4-Filter).
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikostufe: Mittel.

## Parameter

| Name | Standard | Beschreibung | Optimierbar |
| ---- | ------- | ----------- | ----------- |
| `CandleType` | 15 Minuten | Primäre Kerzenserie zur Berechnung von RSI und Signalen. | Ja |
| `TrendCandleType` | 240 Minuten | Höherer Zeitrahmen, der vom Trendfilter EMA verwendet wird (Ersatz für den MT4 RFTL-Indikator). | Ja |
| `RsiPeriod` | 47 | RSI Länge, angewendet auf die Primärkerzen. | Ja |
| `StopLossPoints` | 26 | Abstand zum Stop-Loss in Preisschritten. | Ja |
| `TakeProfitPoints` | 76 | Abstand zum Take-Profit in Preisschritten. | Ja |
| `TrailingStopPoints` | 0 | Trailing-Stop-Offset in Preisschritten (`0` deaktiviert das Trailing). | Ja |

## Notizen

- Der benutzerdefinierte Indikator MetaTrader `RFTL` wird durch einen 10-Perioden-EMA im 240-Minuten-Zeitrahmen angenähert. Passen Sie den längeren Zeitrahmen oder die Länge von EMA an, um sie besser an die ursprüngliche Umgebung anzupassen.
- Stellen Sie sicher, dass `PriceStep` und `StepPrice` des Instruments so konfiguriert sind, dass punktbasierte Stopps mit der Tick-Größe des Brokers übereinstimmen.
- Der Trailing Stop wird erst aktiviert, wenn der Preis um mehr als `TrailingStopPoints` vom Einstiegspreis steigt, und lockert sich nie über den ursprünglichen Stop hinaus.
