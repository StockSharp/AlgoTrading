# Full-Damp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Full Damp-Strategie ist ein Trendumkehrsystem, das auf einem dreifachen Satz von Bollinger-Bändern in Kombination mit einem Bestätigungsfilter für den Relative Strength Index (RSI) basiert. Die Strategie wartet auf Preisspitzen über das breiteste Bollinger-Band hinaus, um eine mögliche Erschöpfung zu erkennen. Ein aktueller überverkaufter oder überkaufter RSI-Wert validiert das Signal, bevor der Handel ausgelöst wird, wenn der Preis innerhalb des mittelbreiten Bandes zurückkehrt. Nach der Positionierung werden Exits mit teilweisen Gewinnmitnahmen, dynamischen Stop-Anpassungen und Bollinger-basierten Trailing-Regeln verwaltet.

## Handelslogik

1. **Signalerkennung**
   * Long-Setups treten auf, wenn das Kerzentief am oder unter dem unteren Band eines Bollinger-Sets mit der Breite 3 schließt. Short-Setups treten auf, wenn das Kerzenhoch das obere Band desselben Sets erreicht.
   * Der RSI muss innerhalb der letzten *Lookback Bars*-Kerzen den überverkauften (Long) oder überkauften (Short) Schwellenwert erreicht haben. Dieser Zustand wird kontinuierlich überwacht, sodass ein neues RSI-Extrem den Countdown aktualisiert.
2. **Eintrittsauslöser**
   * Eine Long-Position wird eröffnet, sobald der Preis wieder über dem unteren Band des mittleren Bollinger-Sets (Breite 2) schließt, sofern noch keine Position offen ist.
   * Eine Short-Position wird eröffnet, nachdem der Preis unter dem oberen Band des mittleren Bollinger-Satzes liegt.
   * Die anfänglichen Stop-Loss-Werte sind am tiefsten Tief (bei Long-Positionen) oder am höchsten Hoch (bei Short-Positionen) seit der Signalkerze verankert, erweitert um den konfigurierbaren Punktversatz.
3. **Positionsverwaltung**
   * Wenn der Markt einen Gewinn in Höhe des ursprünglichen Risikos erzielt, wird die Hälfte der Position geschlossen und der Stop-Loss auf die Gewinnschwelle verschoben.
   * Das verbleibende Volumen wird verlassen, wenn das Kerzenhoch (für Longs) oder das Tief (für Shorts) das mittlere Bollinger-Band in die entgegengesetzte Richtung kreuzt.
   * Wenn der Preis vor Erreichen eines Gewinnziels zum Stop-Level zurückkehrt, wird die gesamte Position geschlossen.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Kerzendatenquelle, die für die Analyse und Ausführung verwendet wird. | Stündliche Kerzen |
| `BollingerPeriod1` | Periode der schmalen Bollinger-Bänder (Breite = 1). | 20 |
| `BollingerPeriod2` | Periode der mittleren Bollinger-Bänder (Breite = 2). | 20 |
| `BollingerPeriod3` | Periode der breiten Bollinger-Bänder (Breite = 3). | 20 |
| `RsiPeriod` | RSI Zeitraum, der für die Signalbestätigung verwendet wird. | 14 |
| `LookbackBars` | Anzahl der abgeschlossenen Kerzen, innerhalb derer RSI die Extremwerte erreichen muss. | 6 |
| `StopOffsetPoints` | Zusätzlicher Puffer (in Preispunkten), der dem anfänglichen Stop-Loss-Level hinzugefügt wird. | 10 |
| `Volume` | Von der Basisstrategie übernommenes Auftragsvolumen. | 1 |

## Notizen

* Die RSI-Schwellenwerte sind auf 30 für lange Signale und 70 für kurze Signale festgelegt, um die ursprüngliche MQL-Logik nachzuahmen.
* Die Strategie verwendet das übergeordnete StockSharp API: Indikatoren sind an das Kerzenabonnement gebunden, das Handelsmanagement verwendet Marktaufträge und die Schutzlogik wird intern ohne manuelle Abfrage der Indikatorwerte gehandhabt.
* Teilausstiege und Stop-Anpassungen werden bei Kerzenschluss ausgeführt, um das Verhalten mit der ursprünglichen Implementierung in Einklang zu bringen.
