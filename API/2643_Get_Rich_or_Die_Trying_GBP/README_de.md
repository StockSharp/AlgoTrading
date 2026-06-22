# Get Rich or Die Trying GBP Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie reproduziert das Verhalten des MetaTrader-Experten «Get Rich or Die Trying GBP». Sie konzentriert sich auf die aktive Überschneidung zwischen den New Yorker und Londoner Sessions und wartet auf einen kurzen Ausbruch des Richtungsungleichgewichts auf 1-Minuten-Kerzen. Der Algorithmus zählt, wie viele der letzten Balken unter ihrer Eröffnung schlossen (im Originalcode als "up" bezeichnet) gegenüber der Anzahl, die über ihrer Eröffnung schloss. Wenn die Zähler abweichen, sucht die Strategie nach einer Gelegenheit, die schwächere Seite während der ersten fünf Minuten der gewählten Zeitfenster zu handeln.

Das System handelt immer nur eine Position gleichzeitig. Es erzwingt eine 61-Sekunden-Abkühlzeit nach jedem Einstieg, trägt sowohl ein primäres festes Take-Profit als auch ein engeres sekundäres Ziel, und folgt optional dem Stop, sobald sich der Preis ausreichend zu seinen Gunsten bewegt. Alle Abstände werden in Pips ausgedrückt, intern durch Verwendung des Wertpapier-Preisschritts umgerechnet (mit einem ×10-Multiplikator für 3- und 5-stellige Kurse), damit die Logik der ursprünglichen MT5-Implementierung entspricht.

## Details

- **Einstiegskriterien**:
  - **Long**: Mehr Kerzen mit `Open > Close` als mit `Open < Close` über die letzten `CountBars` 1-Minuten-Kerzen, aktuelle Zeit innerhalb der ersten fünf Minuten von `22:00 + AdditionalHour` oder `19:00 + AdditionalHour`, keine offene Position, und die 61-Sekunden-Abkühlung ist abgelaufen.
  - **Short**: Mehr Kerzen mit `Open < Close` als mit `Open > Close` unter denselben Zeitbeschränkungen und Abkühlung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Primäres Take-Profit bei `TakeProfitPips` vom Einstieg und Stop-Loss bei `StopLossPips`.
  - Früher Ausstieg, wenn der schwebende Gewinn `SecondaryTakeProfitPips` erreicht.
  - Optionaler Trailing-Stop, der aktiviert wird, sobald der Preis über `TrailingStopPips + TrailingStepPips` hinausgeht, und den Stop um `TrailingStopPips` verschiebt, dabei den Trailing-Schritt respektierend.
- **Stops**: Fester Stop-Loss, festes Take-Profit, sekundäres Take-Profit und optionaler Trailing-Stop.
- **Zeitfilter**: Handelt nur während der ersten fünf Minuten nach den angepassten Stunden 19:00 und 22:00.
- **Abkühlung**: Wartet mindestens 61 Sekunden nach jedem Einstieg, bevor ein neuer Trade erlaubt wird.
- **Standardwerte**:
  - `StopLossPips` = 100
  - `TakeProfitPips` = 100
  - `SecondaryTakeProfitPips` = 40
  - `TrailingStopPips` = 30
  - `TrailingStepPips` = 5
  - `CountBars` = 18
  - `AdditionalHour` = 2
  - `MaxPositions` = 1000
  - `CandleType` = 1-Minuten-Zeitrahmen
- **Hinweise**:
  - `MaxPositions` wird aus Kompatibilitätsgründen mit dem ursprünglichen Experten beibehalten, aber dieser Port hält nur eine aktive Position gleichzeitig.
  - Die Pip-Konvertierung passt sich automatisch an 3- und 5-stellige FX-Symbole an, indem der Preisschritt mit 10 multipliziert wird.
  - Die Trailing-Stop-Logik spiegelt die MT5-Version wider: Sie bewegt sich nicht, bis der Preis über sowohl die Trailing-Distanz als auch den Trailing-Schritt hinaus verbessert.
