# Ichimoku Retracement-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Konvertierung des MetaTrader Expert Advisors **"ICHMOKU RETRACEMENT"**. Sie behält die ursprüngliche Idee bei, Ichimoku-Pullbacks zu handeln, die innerhalb eines Trends auf einem höheren Zeitrahmen auftreten, während sie durch langfristige Momentum- und MACD-Readings gefiltert werden. Die StockSharp-Implementierung konzentriert sich auf Klarheit, Indikator-Wiederverwendung und Risikokontrolle durch die High-Level-API.

## Handelsidee

1. **Trendfilter** – die Strategie sucht nach einem bullischen oder bärischen Bias unter Verwendung eines Paares von Linear Gewichteten Gleitenden Durchschnitten (LWMA). Ein bullischer Kontext erfordert, dass die schnelle LWMA über der langsamen LWMA liegt, während ein bärischer Kontext die entgegengesetzte Beziehung erfordert.
2. **Ichimoku-Retracement** – nach Erkennung eines Trends muss die vorherige Kerze eine der Ichimoku-Linien berühren (Tenkan-sen, Kijun-sen oder die beiden führenden Spans). Die aktuelle Kerze muss auf der Trendseite der berührten Linie wieder öffnen und damit einen Momentum-Pullback signalisieren.
3. **Momentum-Bestätigung** – das Schluss-zu-Schluss-Momentum-Verhältnis muss von seinem neutralen Wert (100) um mindestens einen konfigurierbaren Schwellenwert abweichen. Das Verhältnis wird auf demselben Zeitrahmen berechnet, der für den Ichimoku-Indikator verwendet wird.
4. **Makro-Filter** – ein monatlicher MACD (12/26/9) bestätigt die dominante Langzeit-Richtung. Long-Trades erfordern die MACD-Hauptlinie über der Signallinie, Short-Trades erfordern das Gegenteil.
5. **Orderverwaltung** – die Strategie hält höchstens eine Netto-Position. Schützende Stop-Loss- und Take-Profit-Level werden in Pips gesetzt und bei jeder abgeschlossenen Kerze ausgewertet.

## Parameter

| Name | Beschreibung | Standard |
|------|--------------|----------|
| `Signal Candle Type` | Zeitrahmen für die LWMA-, Ichimoku- und Momentum-Berechnungen. | 1-Stunden-Kerzen |
| `Macro Candle Type` | Höherer Zeitrahmen für den MACD-Trendfilter. | 30-Tage-Kerzen |
| `Fast LWMA` | Periode für den schnellen linear gewichteten gleitenden Durchschnitt. | 6 |
| `Slow LWMA` | Periode für den langsamen linear gewichteten gleitenden Durchschnitt. | 85 |
| `Tenkan Period` | Ichimoku Tenkan-sen-Periode. | 9 |
| `Kijun Period` | Ichimoku Kijun-sen-Periode. | 26 |
| `Span B Period` | Ichimoku Senkou Span B-Periode. | 52 |
| `Momentum Period` | Lookback für das Schluss-zu-Schluss-Momentum-Verhältnis. | 14 |
| `Momentum Threshold` | Mindest-Absolutabweichung von 100, die das Momentum-Verhältnis erfordert. | 0.3 |
| `Take Profit (pips)` | Take-Profit-Abstand in Pips. | 50 |
| `Stop Loss (pips)` | Stop-Loss-Abstand in Pips. | 20 |

Der Basisparameter `Volume` steuert die Größe neuer Orders. Wenn ein Umkehrsignal erscheint, schließt die Strategie die aktuelle Position (falls vorhanden) und öffnet eine neue Position in entgegengesetzter Richtung mit `Volume + |Position|` Kontrakten.

## Handelsregeln

### Long-Einstiege
- Schnelle LWMA > langsame LWMA.
- MACD-Hauptlinie > MACD-Signallinie auf dem Makro-Zeitrahmen.
- Momentum-Verhältnis-Abweichung ≥ Schwellenwert.
- Das Tief der vorherigen Kerze berührte mindestens ein Ichimoku-Level und die aktuelle Kerze öffnete wieder über diesem Level.
- Netto-Position muss flach oder short sein.

### Short-Einstiege
- Schnelle LWMA < langsame LWMA.
- MACD-Hauptlinie < MACD-Signallinie auf dem Makro-Zeitrahmen.
- Momentum-Verhältnis-Abweichung ≥ Schwellenwert.
- Das Hoch der vorherigen Kerze berührte mindestens ein Ichimoku-Level und die aktuelle Kerze öffnete wieder unter diesem Level.
- Netto-Position muss flach oder long sein.

### Ausstiege
- Eine Long-Position schließt, wenn das Tief der Kerze den Stop-Loss oder das Hoch das Take-Profit-Level erreicht.
- Eine Short-Position schließt, wenn das Hoch der Kerze den Stop-Loss oder das Tief das Take-Profit-Level erreicht.

## Unterschiede vs. Original-EA

- Geldmanagement-Leitern, Break-Even-Bewegungen und Trailing-Features aus der MQL-Version werden nicht repliziert; die Risikokontrolle ist auf feste Stop-Loss- und Take-Profit-Level beschränkt.
- StockSharp arbeitet mit einer einzigen Netto-Position, sodass der Martingale-Order-Stack durch einen Eintrag pro Richtung ersetzt wird.
- Alerts, E-Mail- und Push-Benachrichtigungen aus der MetaTrader-Umgebung werden weggelassen.

## Verwendungshinweise

1. Die Strategie einem StockSharp Designer- oder Shell-Projekt hinzufügen.
2. Das gewünschte Instrument auswählen und den `Signal Candle Type` an den Zielzeitrahmen anpassen.
3. Sicherstellen, dass der `Macro Candle Type` aus den verfügbaren Daten synthetisiert werden kann (das Abonnement verwendet `allowBuildFromSmallerTimeFrame`).
4. Stop-Loss, Take-Profit und den Momentum-Schwellenwert entsprechend der Volatilität des Instruments einstellen.

Die enthaltenen Kommentare beschreiben jeden Entscheidungsschritt, sodass die Logik leicht angepasst oder erweitert werden kann.
