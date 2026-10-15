# Strategie Pivot Point Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Pivot Points mit einem ATR-basierten Supertrend kombiniert, um Trendwenden zu erfassen.

Pivot Points definieren eine dynamische Mittellinie. Ein ATR-Multiplikator bildet obere und untere Bänder, die dem Kurs folgen. Wenn der Trend die Richtung wechselt, eröffnet die Strategie entsprechend eine Position.

## Details

- **Einstiegskriterien**: Signale basierend auf Pivot Points und ATR Supertrend.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `PivotPeriod` = 2
  - `AtrFactor` = 3m
  - `AtrPeriod` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Pivot Points, ATR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Berechnung und Ausführung

Nur abgeschlossene Kerzen werden verwendet. Ein Pivot-Hoch/-Tief muss strikt über/unter allen `PivotPeriod` Kerzen auf jeder Seite liegen und wird erst nach Abschluss der rechten Kerzen bestätigt. Erfüllt eine Kerze beide Bedingungen, hat das Hoch Vorrang. Der erste Pivot initialisiert `center`; danach gilt `(2 * center + pivot) / 3`.

Die Pivot-Bänder verwenden Wilder ATR: Mittelwert der ersten `AtrPeriod` True Ranges, danach Glättung mit Gewicht `1 / AtrPeriod`. Grundwerte für Unterstützung/Widerstand sind `center ± AtrFactor * ATR`. Unterstützung übernimmt das Maximum aus Grundwert und Vorwert, wenn der vorige Schlusskurs über der vorigen Unterstützung liegt; sonst wird sie zurückgesetzt. Widerstand übernimmt das Minimum, wenn der vorige Schlusskurs unter dem vorigen Widerstand liegt; sonst wird er zurückgesetzt. Schlusskurs strikt über dem vorigen Widerstand schaltet aufwärts, strikt unter der vorigen Unterstützung abwärts; sonst bleibt die Richtung. Initialrichtung ist aufwärts; Initialisierung ist kein Signal.

Wilder ATR und eine bestätigte Pivot-Mitte müssen bereit sein. Nur ein Richtungswechsel der Pivot-Bänder eröffnet eine Position: aufwärts Long, abwärts Short. Initialrichtung ist aufwärts; die Initialisierung erzeugt keine Order. Jedes Gegensignal kehrt die ganze Position mit einer Market-Order von `Volume + abs(Position)` um; ein Einstieg ohne Position nutzt `Volume`. Kein SMA, separater Preis-SuperTrend, Datumsfilter, keine Wartefrist und keine Schutzstopps.

Pivot-Konzept: [Pivot Point Supertrend von LonesomeTheBlue](https://www.tradingview.com/script/L0AIiLvH-Pivot-Point-Supertrend/). Die obigen Regeln definieren das bidirektionale Modell dieses Beispiels.
