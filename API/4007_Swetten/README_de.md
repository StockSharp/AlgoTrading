# Swetten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Swetten ist eine durch ein neuronales Netzwerk gesteuerte Breakout-Strategie, die ursprünglich für MetaTrader 4 verbreitet wurde. Sie bewertet die Spanne zwischen einem langfristigen einfachen gleitenden Durchschnitt mit 233 Perioden und zehn schnelleren gleitenden Durchschnitten, die auf Ein-Minuten-Kerzen berechnet werden. Die Spreads werden in ein radiales Basisnetzwerk eingespeist, das ein bullisches oder bärisches Aktivierungsniveau erzeugt. Wenn die Aktivierung positiv ist, geht die Strategie in die Long-Position ein, ist sie negativ, geht sie in die Short-Position.

## Markt und Zeitrahmen
- Entwickelt für die wichtigsten FX-Paare (der ursprüngliche Code zielte auf EURUSD ab).
- Bei der Analyse werden Ein-Minuten-Kerzen verwendet und Entscheidungen werden nur anhand abgeschlossener Kerzen getroffen.
- Die Signalauswertung erfolgt alle zwei Stunden zur vollen Stunde (00:00, 02:00, …, 22:00 Uhr Wechselzeit). Samstags und sonntags sind keine Geschäfte geöffnet.

## Indikatoren und Funktionen
- Simple moving averages with periods: 233 (baseline), 144, 89, 55, 34, 21, 13, 8, 5, 3, 2.
- Eingaben des neuronalen Netzwerks sind die Unterschiede zwischen dem 233-Perioden-Durchschnitt und jedem schnelleren Durchschnitt.
- Vor der Übergabe an das Netzwerk werden die Eingaben auf trainierte Bereiche beschränkt, normalisiert und mit denselben Koeffizienten skaliert, die in der ursprünglichen DLL verwendet wurden.
- Das radiale Basisnetzwerk wird exakt aus der exportierten `EURUSDn`-Funktion repliziert und besteht aus 38 Gaußschen Merkmalen, deren Ausgaben gemittelt werden, um die endgültige Aktivierung zu erhalten.

## Handelsregeln
1. Warten Sie auf den Schluss einer einminütigen Kerze, die zu einer geraden Stunde endet und auf einen Wochentag fällt.
2. Berechnen Sie die Aktivierung des neuronalen Netzwerks anhand der Spreads des gleitenden Durchschnitts.
3. Wenn die Aktivierung > 0 ist und die aktuelle Position nicht lang ist, senden Sie einen Marktkauf für `TradeVolume + abs(current position)` Lots.
4. Wenn die Aktivierung < 0 ist und die aktuelle Position nicht leer ist, senden Sie einen Marktverkauf für `TradeVolume + abs(current position)` Lots.
5. Positionen sind geschützt durch:
   - Ein fester Take-Profit, definiert in Preisschritten (`TakeProfitPoints`).
   - Ein fester Stop-Loss, definiert in Preisschritten (`StopLossPoints`).
   - Wenn eines der beiden Niveaus durch die Hoch-/Tief-Extremwerte der Kerze erreicht wird, wird die Position durch eine Marktorder geschlossen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Für Berechnungen verwendete Kerzenreihe. | Zeitrahmen von 1 Minute |
| `TradeVolume` | Basisauftragsvolumen in Losen. | 0,1 |
| `SlowPeriod` | Periode des einfachen gleitenden Basisdurchschnitts. | 233 |
| `TakeProfitPoints` | Gewinnzielentfernung in Preisschritten. | 150 |
| `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. | 40 |

## Konvertierungshinweise
- Das DLL-basierte neuronale Netzwerk von MetaTrader wurde vollständig auf C# portiert, indem die exportierte Funktion in verwalteten Code übersetzt wurde.
- Protective exits mimic the original `OrderClose` conditions by checking candle highs and lows against price step thresholds.
- Die Eintrittsverwaltung verfolgt den letzten Füllpreis über `OnNewMyTrade`, um Ausstiege mit tatsächlichen Füllungen in Einklang zu bringen.
- Alle Kommentare wurden in Englisch umgeschrieben und der Code verwendet High-Level-StockSharp-APIs (`SubscribeCandles`, `Bind`), wie in den Konvertierungsrichtlinien erforderlich.
