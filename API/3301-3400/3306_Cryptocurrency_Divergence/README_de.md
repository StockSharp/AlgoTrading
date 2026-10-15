# Cryptocurrency-Divergence-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Cryptocurrency-Divergence-Strategie** sucht nach klassischen Momentum-Divergenzen zwischen Kursverlauf und Relative Strength Index (RSI) und bestätigt die Trendrichtung mit gleitenden Durchschnitten und MACD. Der ursprüngliche MetaTrader-Expert-Advisor setzte auf Multi-Zeitrahmen-Momentum-Prüfungen, Geldmanagement und umfangreiche Trailing-Logik. Dieser StockSharp-Port bewahrt den Kern des Systems durch:

- Erkennen bullischer Divergenzen, wenn der Preis ein tieferes Tief bildet, der RSI aber ein höheres Tief.
- Erkennen bärischer Divergenzen, wenn der Preis ein höheres Hoch bildet, der RSI aber ein tieferes Hoch.
- Validierung von Setups mit schneller/langsamer gleitender Durchschnittslinie und MACD-Linie gegenüber Signallinie.
- Positionsverwaltung über konfigurierbaren Stop Loss, Take Profit, Break-even und Trailing Stop in Preisschritten.

Die Strategie ist für Spot-Kryptowährungen ausgelegt, kann aber auf jedes Instrument angewendet werden, das genügend Volatilität und klare Swing-Punkte liefert.

## Indikatoren
- **Einfacher gleitender Durchschnitt (SMA)**: Eine schnelle und eine langsame SMA bilden den primären Trendfilter.
- **Relative Strength Index (RSI)**: Liefert die Momentum-Pivotwerte zur Messung der Divergenzstärke.
- **Moving Average Convergence Divergence (MACD)**: Bestätigt, dass das Momentum zur erkannten Divergenzrichtung passt.

Alle Indikatoren sind über die High-Level-API gebunden, sodass kein manuelles Puffern erforderlich ist.

## Handelslogik
1. Den konfigurierten Kerzentyp abonnieren und SMA-, RSI- und MACD-Werte auf jeder abgeschlossenen Bar berechnen.
2. Die jüngsten Swing-Hochs und -Tiefs zusammen mit ihren RSI-Werten verfolgen. Nur monotone Erweiterungen (neue höhere Hochs oder tiefere Tiefs) aktualisieren die Swing-Daten.
3. Eine **bullische Divergenz** erscheint, wenn ein frisches tieferes Tief im Preis mit einem höheren RSI-Tief gekoppelt ist. Der Trade erfordert außerdem, dass die schnelle SMA über der langsamen SMA liegt, die MACD-Linie die Signallinie übersteigt und der RSI unter dem neutralen Niveau (Standard 45) bleibt, um überverkaufte Bedingungen sicherzustellen.
4. Eine **bärische Divergenz** erfordert ein neues höheres Hoch im Preis mit einem niedrigeren RSI-Hoch, schnelle SMA unter langsamer SMA, MACD-Linie unter ihrem Signal und RSI über dem neutralen bärischen Niveau (Standard 55).
5. Die Strategie öffnet nur eine Nettoposition gleichzeitig. Umkehrungen schließen die bestehende Position und eröffnen sofort in Gegenrichtung, wenn die Signale übereinstimmen.

## Risikomanagement
- **Volumen**: Benutzerdefinierte Tradegröße für alle Marktorders.
- **Stop Loss / Take Profit**: In Preisschritten angegeben und nach jeder Ausführung mit dem tatsächlichen Ausführungspreis angefügt.
- **Break-even-Verschiebung**: Ersetzt den Stop Loss optional durch einen Offset über/unter dem Einstieg, sobald der Preis eine konfigurierbare Distanz zurückgelegt hat.
- **Trailing Stop**: Zieht optional hinter dem Schlusskurs in fester Schrittweite nach. Nach Aktivierung hat der Trailing Stop Vorrang vor dem ursprünglichen Stop Loss.

Stops und Ziele werden auf jeder abgeschlossenen Kerze ausgewertet, wodurch ein deterministisches Verhalten entsteht, das Backtests und Echtzeitausführung angleicht.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Kerzenserie für die Analyse (standardmäßig 15-Minuten-Zeitrahmen). |
| `TradeVolume` | Ordervolumen für alle Einstiege. |
| `FastMaLength` / `SlowMaLength` | Perioden der schnellen und langsamen SMAs. |
| `RsiLength` | RSI-Berechnungslänge. |
| `RsiBullishLevel` / `RsiBearishLevel` | RSI-Schwellen, die überverkaufte und überkaufte Zonen zur Divergenzbestätigung definieren. |
| `MacdShortLength` / `MacdLongLength` / `MacdSignalLength` | MACD-Konfiguration. |
| `StopLossPoints` / `TakeProfitPoints` | Distanzen in Preisschritten für Risiko- und Gewinnziele. |
| `EnableBreakEven`, `BreakEvenTrigger`, `BreakEvenOffset` | Steuerung der Break-even-Verschiebung. |
| `EnableTrailing`, `TrailDistance` | Aktivierung und Abstand des Trailing Stops. |

Jeder Parameter wird über `StrategyParam<T>` bereitgestellt, sodass er im StockSharp Designer optimiert werden kann.

## Nutzungshinweise
1. Binden Sie die Strategie an ein Kryptowährungssymbol und stellen Sie sicher, dass das Instrument `PriceStep` und `Board` definiert hat. Ohne Preisschritt kann die Strategie keine Stops berechnen.
2. Stimmen Sie den Kerzentyp auf den gehandelten Markt ab (z. B. 15m, 1h). Divergenzerkennung ist zeitrahmensensitiv.
3. Passen Sie Stop- und Zielabstände an die Volatilität des Instruments an. Kryptopaare mit fünf Dezimalstellen benötigen oft größere Schrittzahlen.
4. Aktivieren Sie Break-even oder Trailing erst, nachdem historische Tests genügend Gewinnpuffer gezeigt haben; aggressives Trailing kann Trades zu früh beenden.
5. Überwachen Sie die Strategie im StockSharp Designer oder Marktdatenfenster, um Indikatorausrichtung und ausgeführte Trades zu visualisieren.

## Unterschiede zur MQL-Version
- Geldbasiertes Trailing und Equity-Stop-Schutz wurden in eine preisschrittbasierte Stop-Verwaltung vereinfacht.
- Multi-Zeitrahmen-Momentum-Prüfungen wurden aus Klarheitsgründen durch MACD-Bestätigung auf einem Zeitrahmen ersetzt.
- E-Mail-/Benachrichtigungs-Nebeneffekte werden ausgelassen, weil sie in StockSharp-Ökosystemen extern behandelt werden.

Trotz dieser Anpassungen bleiben die zentrale Divergenzerkennung und Schutzlogik der Absicht des ursprünglichen Expert-Advisors treu.
