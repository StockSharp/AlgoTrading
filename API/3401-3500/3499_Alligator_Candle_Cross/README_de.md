# Alligator Candle Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert die MetaTrader-Experten **Alligatorkerzenkreuz nach oben/unten** auf das StockSharp-Hochniveau API. Es überwacht den Bill Williams Alligator-Indikator, der aus geglätteten gleitenden Durchschnitten des Medianpreises besteht, und reagiert immer dann, wenn ein fertiger Kerzenkörper von einer Seite der Alligator-Mündung zur anderen wandert. Eingaben können über einen Parameter auf bullisch, bärisch oder beide Richtungen beschränkt werden, während feste Pip-basierte Stopps und Ziele das Risikomanagement übernehmen.

## Handelslogik

### Indikatorvorbereitung
- Berechnen Sie Alligator **Kiefer**, **Zähne** und **Lippen** mithilfe geglätteter gleitender Durchschnitte mit den klassischen 13/8/5-Längen.
- Wenden Sie die traditionellen Vorwärtsverschiebungen an (standardmäßig 8/5/3 Balken), sodass jede Linie mit der Kerze verglichen wird, die sich davor bildet.
- Alle Preise werden anhand des Kerzenmedians `(High + Low) / 2` abgetastet, um mit der MetaTrader-Implementierung übereinzustimmen.

### Langes Setup („Kerze nach oben“)
1. Die zuvor abgeschlossene Kerze muss auf oder unter der niedrigsten Alligator-Linie schließen (nach Anwendung der Verschiebung).
2. Der aktuelle Kerzenkörper öffnet sich bei oder unter dem höchsten verschobenen Alligator-Wert und schließt über diesem gleichen Wert, was beweist, dass der Körper die Alligator-Mündung nach oben durchquert hat.
3. Derzeit ist keine Position offen und der Handel ist erlaubt.
4. Wenn alle Bedingungen übereinstimmen, sendet die Strategie einen Markt-**Kauf** für das konfigurierte Volumen.

### Kurzer Aufbau („Kerzenkreuz nach unten“)
1. Der vorherige Schlusskurs muss auf oder über der höchsten verschobenen Alligator-Linie liegen.
2. Der aktuelle Kerzenkörper öffnet sich bei oder über dem niedrigsten verschobenen Alligator-Wert und endet darunter, was einen rückläufigen Cross durch den Alligator bestätigt.
3. Es ist keine Position offen und der Handel ist aktiviert.
4. Für das konfigurierte Volumen wird eine Marktorder **Verkauf** gesendet.

### Positionsmanagement
- Wenn eine neue Position eröffnet wird, wandelt die Strategie die Stop-Loss- und Take-Profit-Abstände von Pips mithilfe der Symbolpreisschrittweite in absolute Preise um.
- Long-Positionen werden beendet, wenn die Kerze den Stop-Loss berührt, das Ziel erreicht oder wieder unter dem Minimum der verschobenen Teeth- und Lips-Linien schließt.
- Short-Positionen werden beim Stop-Loss, dem Ziel oder einem Schlusskurs über dem Maximum der verschobenen Teeth- und Lips-Werte beendet.
- Der integrierte **StartProtection**-Aufruf wird beim Start aktiviert, um sicherzustellen, dass abnormale Füllungen sicher geschlossen werden.

## Parameter

| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.1` | Handelsgröße in Lots oder Kontrakten. |
| `StopLossPips` | `int` | `50` | Abstand vom Einstiegspreis bis zum Schutzstopp in Pips. Null deaktiviert den Stopp. |
| `TakeProfitPips` | `int` | `50` | Abstand vom Einstieg zum festgelegten Gewinnziel in Pips. Null deaktiviert das Ziel. |
| `JawPeriod` | `int` | `13` | Geglättete gleitende Durchschnittslänge für die Kieferlinie Alligator (blau). |
| `JawShift` | `int` | `8` | Auf die Kieferlinie angewendete Vorwärtsverschiebung vor der Signalauswertung. |
| `TeethPeriod` | `int` | `8` | Geglättete gleitende Durchschnittslänge für die Linie Alligator Zähne (rot). |
| `TeethShift` | `int` | `5` | Vorwärtsverschiebung der Zahnlinie. |
| `LipsPeriod` | `int` | `5` | Geglättete gleitende Durchschnittslänge für die Linie Alligator Lippen (grün). |
| `LipsShift` | `int` | `3` | Vorwärtsverschiebung der Lippenlinie. |
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Für Berechnungen verwendete Kerzenreihe. |
| `EntryMode` | `AlligatorCrossMode` | `Both` | Wählt aus, ob die Strategie Long-Setups, Short-Setups oder beides handelt. |

## Nutzungshinweise
- Funktioniert auf jedem von StockSharp unterstützten Instrument; Stellen Sie sicher, dass `CandleType` mit dem Zeitrahmen übereinstimmt, der in der ursprünglichen MetaTrader-Vorlage verwendet wurde.
- Pips werden aus der Preisstufe des Instruments abgeleitet: Bei Notierungen mit 3 oder 5 Dezimalstellen (z. B. EURUSD) entspricht der Pip zehn Preisstufen.
- Die Logik wirkt nur auf abgeschlossene Kerzen und verlässt sich nicht auf Tick-Daten, wodurch sie mit MetaTrader-Backtests in Einklang bleibt.
