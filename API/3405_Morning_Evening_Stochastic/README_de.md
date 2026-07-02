# Morgen Abend Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader 5 Fachberater **Expert_AMS_ES_Stoch** (Morning/Evening Star mit Stochastic Bestätigung) in StockSharp. Die Implementierung behält die ursprünglichen Regeln für die Erkennung von Kerzenmustern und die stochastische Bestätigung bei und verwendet gleichzeitig das High-Level-Kerzenabonnement API, sodass jede Entscheidung anhand fertiger Balken getroffen wird.

## Strategielogik
- **Indikatoren**
  - Standard-Stochastic-Oszillator mit konfigurierbaren `%K`-, `%D`- und Verlangsamungsperioden.
  - Einfacher gleitender Durchschnitt der Kerzenkörpergröße (absoluter `open-close`), um Kerzen wie bei der MQL-Version als „lang“ oder „klein“ zu klassifizieren.
- **Long Entry**
  - Morning Star-Muster über die letzten drei abgeschlossenen Kerzen:
    1. Vor zwei Balken: langer bärischer Körper, dessen Größe den Körperdurchschnitt übersteigt.
    2. Vorheriger Balken: Kerze mit kleinem Körper, die unterhalb der vorherigen Kerze schließt und öffnet.
    3. Aktueller Balken: bullischer Schlusskurs über dem Mittelpunkt der ersten Kerze.
  - Die Signallinie Stochastic (`%D`) liegt unter dem Überverkaufsschwellenwert (Standardwert `30`).
  - Das bestehende Short-Engagement wird abgeflacht, bevor die Long-Position eröffnet wird.
- **Short Entry**
  - Evening Star-Muster, das die oben genannten Regeln widerspiegelt.
  - Stochastic `%D` liegt über dem Überkaufschwellenwert (Standardwert `70`).
  - Bestehende Long-Positionen werden geschlossen, bevor der Short-Trade eröffnet wird.
- **Positionsausgang**
  - Shorts werden geschlossen, wenn `%D` entweder den Wert für die schnelle Erholung (`20`) oder den Wert für die extreme Erholung (`80`) überschreitet.
  - Long-Positionen werden geschlossen, wenn `%D` entweder `80` oder `20` unterschreitet.
  - Diese Kreuzungen reproduzieren die „Schließbedingungen“ aus dem Signalmodul MQL.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen (oder ein anderer `DataType`), der für die Mustererkennung und alle Indikatoren verwendet wird. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | `%K`, `%D` und Verlangsamungsperioden des stochastischen Oszillators. |
| `StochasticOverbought`, `StochasticOversold` | Signalleitungsschwellenwerte, die zur Bestätigung von Evening/Morning Star-Einträgen verwendet werden. |
| `PatternAveragePeriod` | Anzahl der fertigen Kerzen zur durchschnittlichen Körpergröße (`|öffnen-schließen|`). |
| `ShortExitLevel`, `LongExitLevel` | `%D`-Ebenen, die kurze/lange Ausgänge erzwingen, wenn sie in die entgegengesetzte Richtung überquert werden. |

## Implementierungshinweise
- Kerzen werden über `SubscribeCandles().BindEx(...)` verarbeitet; Der Code funktioniert nur mit fertigen Kerzen und ruft niemals `GetValue()` für Indikatoren auf.
- Die Mittelung der Körpergröße basiert auf `SimpleMovingAverage`, das mit absoluten Kerzenkörpern gefüttert wird, um den `AvgBody()`-Helfer aus der MQL-Bibliothek zu reproduzieren.
- Musterprüfungen werden mit speziellen Hilfsmethoden implementiert, um die Entscheidungslogik lesbar zu halten und die ursprünglichen `CCandlePattern`-Regeln widerzuspiegeln.
- Bevor die Strategie in die entgegengesetzte Richtung eintritt, schließt sie alle bestehenden Risiken, um dem Verhalten des Expert Advisors zu entsprechen, der jeweils eine Nettoposition betreibt.

## Unterschiede zum MQL5 Expert
- Money-Management, Trailing Stop und feste Lot-Einstellungen aus dem MetaTrader-Framework werden nicht reproduziert; Das Auftragsvolumen von StockSharp wird durch die Strategieeigenschaft `Volume` gesteuert.
- Der Stochastic-Oszillator verwendet die Indikatorimplementierung von StockSharp; Schwellenwerte bleiben konfigurierbar, sodass Sie das Verhalten optimieren können, wenn der ursprüngliche Broker-Feed leicht unterschiedliche Werte lieferte.
- Die Protokollierung bietet detaillierte Erklärungen (auf Englisch) für jeden Ein- und Ausgang, um das Debuggen und Backtesting zu erleichtern.
