# Pivot Point SuperTrend Trendfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert eine pivot-basierte SuperTrend-Linie mit einem SuperTrend-Trendfilter und einer gleitenden Durchschnitts-Bestätigung. Handelt, wenn der Trend dreht oder wenn ein Pivot SuperTrend-Signal innerhalb eines Datumsfensters erscheint.

## Details

- **Einstiegskriterien**:
  - Long: Der Preis-SuperTrend dreht nach oben und der Schlusskurs liegt über dem Schlusskurs-SMA, oder der Pivot-SuperTrend dreht im Datumsfenster nach oben.
  - Short: Der Preis-SuperTrend dreht nach unten und der Schlusskurs liegt unter dem SMA, oder der Pivot-SuperTrend dreht im Datumsfenster nach unten.
- **Ausstiegskriterien**:
  - Jede Abwärtsdrehung schließt den gesamten Long, jede Aufwärtsdrehung den gesamten Short, unabhängig von SMA und Datum.
  - Umkehr nur bei bestätigtem Gegeneinstieg. Gleichzeitige entgegengesetzte Drehungen schließen die Position ohne Neueinstieg.
- **Stops**: Keine
- **Standardwerte**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
  - `CandleType` = 5 Minuten
  - `StartDate` / `EndDate` = UTC `DateTime.MinValue` / `DateTime.MaxValue`
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Pivot, SuperTrend, SMA
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Optional
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Berechnung und Ausführung

Nur abgeschlossene Kerzen werden verwendet. Ein Pivot-Hoch/-Tief muss strikt über/unter allen `PivotPeriod` Kerzen auf jeder Seite liegen und wird erst nach Abschluss der rechten Kerzen bestätigt. Erfüllt eine Kerze beide Bedingungen, hat das Hoch Vorrang. Der erste Pivot initialisiert `center`; danach gilt `(2 * center + pivot) / 3`.

Die Pivot-Bänder verwenden Wilder ATR: Mittelwert der ersten `AtrPeriod` True Ranges, danach Glättung mit Gewicht `1 / AtrPeriod`. Grundwerte für Unterstützung/Widerstand sind `center ± Factor * ATR`. Unterstützung übernimmt das Maximum aus Grundwert und Vorwert, wenn der vorige Schlusskurs über der vorigen Unterstützung liegt; sonst wird sie zurückgesetzt. Widerstand übernimmt das Minimum, wenn der vorige Schlusskurs unter dem vorigen Widerstand liegt; sonst wird er zurückgesetzt. Schlusskurs strikt über dem vorigen Widerstand schaltet aufwärts, strikt unter der vorigen Unterstützung abwärts; sonst bleibt die Richtung. Initialrichtung ist aufwärts; Initialisierung ist kein Signal.

Der separate Preisfilter verwendet den nativen StockSharp-SuperTrend (`TrendAtrPeriod`, `TrendMultiplier`). SMA enthält den aktuellen Schlusskurs. Nur Richtungswechsel sind Signale; beide ATRs, SMA und Pivot-Mitte müssen bereit sein. `StartDate <= candle.OpenTime <= EndDate` gilt nur für Pivot-Einstiege. Datumswerte müssen geordnet und UTC sein. Einstieg: `Volume`; Umkehrorder: `Volume + abs(Position)`; Schließung: `abs(Position)`. Keine Wartefrist und keine Schutzstopps.

Pivot-Konzept: [Pivot Point Supertrend von LonesomeTheBlue](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). Die obigen Regeln definieren das bidirektionale Modell dieses Beispiels.
