# Bullische und bärische Harami-Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Bullish & Bearish Harami Stochastic Strategy** ist die StockSharp-Portierung des MetaTrader Expert Advisors `expert_abh_bh_stoch.mq5` aus dem Ordner `MQL/310`. Der ursprüngliche Experte verwendet die Candlestick-Mustererkennung für bullische Harami- und bärische Harami-Setups und erfordert eine Bestätigung des stochastischen Oszillators. Die C#-Version behält die gleiche Logik bei und verwendet das übergeordnete StockSharp API und fügt detaillierte Protokollierung und Diagrammausgabe zur einfacheren Überwachung hinzu.

## Kernideen

- Erkennen Sie bullische Harami- und bärische Harami-Kerzenmuster anhand der beiden vorherigen abgeschlossenen Kerzen.
- Bestätigen Sie bullische Setups mit der stochastischen %D-Linie unter einem überverkauften Schwellenwert und bärische Setups mit %D über einem überkauften Schwellenwert.
- Schließen Sie Short-Positionen, wenn die stochastische %D-Linie über die untere oder obere Ausstiegsschwelle steigt, und schließen Sie Long-Positionen, wenn %D unter diese Schwellenwerte fällt.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Zeitrahmen der zur Mustererkennung verwendeten Kerzenserie. | `1 Hour` |
| `StochasticKPeriod` | Lookback-Zeitraum für die stochastische %K-Berechnung. | `47` |
| `StochasticDPeriod` | Glättungszeitraum für die %D-Linie. | `9` |
| `StochasticSlowing` | Zusätzliche Glättung auf %K angewendet (MT5 „Verlangsamung“). | `13` |
| `MovingAveragePeriod` | Anzahl der Kerzen, die zur durchschnittlichen Körpergröße zur Mustervalidierung verwendet werden. | `5` |
| `OversoldLevel` | Stochastic %D Schwelle zur Bestätigung bullischer Signale. | `30` |
| `OverboughtLevel` | Stochastic %D Schwelle zur Bestätigung rückläufiger Signale. | `70` |
| `ExitLowerLevel` | Unteres stochastisches Niveau, das Ausstiege auslöst. | `20` |
| `ExitUpperLevel` | Oberes stochastisches Niveau, das Ausstiege auslöst. | `80` |

## Handelsregeln

### Langer Eintrag
1. Bei den beiden zuletzt abgeschlossenen Kerzen wird ein bullisches Harami-Muster erkannt (eine kleine bullische Kerze, die in einem Abwärtstrend von einer längeren bärischen Kerze verschlungen wird).
2. Die stochastische %D-Linie der Bestätigungskerze liegt bei oder unter `OversoldLevel`.
3. Derzeit ist keine Long-Position offen (`Position <= 0`).
4. Die Strategie kauft zum Marktpreis für den konfigurierten `Volume` und fügt gegebenenfalls ein Short-Engagement hinzu, um die Position umzudrehen.

### Kurzer Eintrag
1. Es wurde ein bärisches Harami-Muster erkannt (kleine bärische Kerze innerhalb einer langen zinsbullischen Kerze während eines Aufwärtstrends).
2. Der stochastische %D-Wert liegt bei oder über `OverboughtLevel`.
3. Es existiert keine Kurzzeitbelichtung (`Position >= 0`).
4. Die Strategie verkauft zum Marktwert und deckt bei Bedarf zunächst alle Long-Positionen ab.

### Ausgänge
- **Shorts abdecken:** Wenn der stochastische %D entweder `ExitLowerLevel` oder `ExitUpperLevel` nach oben kreuzt, deckt der Algorithmus die gesamte Short-Position ab.
- **Long-Positionen schließen:** Wenn der stochastische %D durch `ExitUpperLevel` oder `ExitLowerLevel` nach unten kreuzt, wird die Long-Position geschlossen.

## Dateien

- `CS/BullishBearishHaramiStochasticStrategy.cs` – StockSharp Umsetzung der Strategie auf hoher Ebene.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.

> **Hinweis:** Die Python-Version ist gemäß den Konvertierungsanweisungen nicht enthalten.
