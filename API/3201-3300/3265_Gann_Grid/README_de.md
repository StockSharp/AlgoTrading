# Gann Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den ursprünglichen **Gann Grid**-Expertenberater aus `MQL/25065/Gann Grid.mq4` auf die StockSharp High-Level-API. Das ursprüngliche Skript vermischte manuelle Chart-Objekte mit Mehrfachzeitrahmen-Filtern; die C#-Version behält den allgemeinen Arbeitsablauf bei und ersetzt dabei Chart-abgeleitete Daten durch indikatorgesteuerte Logik, die unbeaufsichtigt laufen kann.

## Handelslogik

1. **Synthetisches Gann-Grid** – Das höchste Hoch und tiefste Tief über `AnchorPeriod` Kerzen approximieren die Preisniveaus, die in MetaTrader manuell gezeichnet wurden. Ein Ausbruch über das Hoch löst Long-Setups aus, ein Zusammenbruch unter das Tief löst Shorts aus.
2. **Trendbestätigung** – Schnelle und langsame linear gewichtete gleitende Durchschnitte auf dem höheren Zeitrahmen (`TrendCandleType`) müssen mit der Ausbruchsrichtung übereinstimmen.
3. **Momentum-Filter** – Der prozentuale Abstand zwischen dem Momentum-Indikator und dem aktuellen Preis (ebenfalls auf dem höheren Zeitrahmen) muss `MomentumThreshold` überschreiten, um ausreichende Beschleunigung sicherzustellen.
4. **MACD-Bestätigung** – Ein separater Kerzenstrom (`MacdCandleType`) treibt einen MACD (standardmäßig 12/26/9). Die MACD-Linie muss auf derselben Seite von Null und der Signallinie liegen wie die Trade-Richtung.
5. **Risikomanagement** – Symmetrische Stop-Loss- und Take-Profit-Abstände werden vom Einstiegspreis angewendet. Optionale Break-Even- und Trailing-Module reproduzieren die Eigenkapitalschutzblöcke der MQL-Implementierung.

Nur fertige Kerzen werden verarbeitet, um den ursprünglichen "neue Balken"-Prüfungen zu entsprechen.

## Unterschiede zur MQL-Version

- Der MetaTrader-Code erwartete ein manuell gezeichnetes `GANNGRID`-Objekt. Der Port ersetzt es durch rollende Höchst-/Tiefstwert-Indikatoren, was die Logik für automatisiertes Testen deterministisch macht.
- Momentum in MetaTrader ist um 100 zentriert. Das `Momentum` von StockSharp gibt eine Preisdifferenz aus, daher konvertiert die Strategie es in einen Prozentsatz des aktuellen Schlusskurses vor dem Vergleich mit `MomentumThreshold`.
- Benachrichtigungen (E-Mail, Push) und grafische Operationen des MQL-Skripts werden weggelassen.
- Risikomanagement verwendet Marktausstiege statt Modifikation bestehender Orders, da StockSharp-Strategien Positionen statt Terminal-Aufträge verwalten.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 5-Minuten-Zeitrahmen | Primäre Kerzen, die Ausbrüche definieren. |
| `TrendCandleType` | `DataType` | 15-Minuten-Zeitrahmen | Höherer Zeitrahmen für LWMA- und Momentum-Filter. |
| `MacdCandleType` | `DataType` | 1-Tag-Zeitrahmen | Kerzenstrom für den MACD-Bestätigungsfilter. |
| `FastMaPeriod` | `int` | 6 | Schnelle LWMA-Länge auf dem höheren Zeitrahmen. |
| `SlowMaPeriod` | `int` | 85 | Langsame LWMA-Länge auf dem höheren Zeitrahmen. |
| `MomentumPeriod` | `int` | 14 | Momentum-Rückblicklänge. |
| `MomentumThreshold` | `decimal` | 0.3 | Minimale Momentum-Abweichung in Prozent für den Handel. |
| `AnchorPeriod` | `int` | 100 | Anzahl primärer Kerzen für das synthetische Gann-Grid. |
| `TakeProfitOffset` | `decimal` | 0.005 | Absoluter Take-Profit-Abstand vom Einstiegspreis. |
| `StopLossOffset` | `decimal` | 0.002 | Absoluter Stop-Loss-Abstand vom Einstiegspreis. |
| `EnableTrailing` | `bool` | `true` | Aktiviert Trailing-Stop-Verwaltung. |
| `TrailingActivation` | `decimal` | 0.003 | Gewinn erforderlich, bevor der Trailing Stop dem Preis folgt. |
| `TrailingStep` | `decimal` | 0.0015 | Abstand zwischen dem lokalen Hoch und dem Trailing Stop. |
| `EnableBreakEven` | `bool` | `true` | Aktiviert Move-to-Break-Even-Logik. |
| `BreakEvenTrigger` | `decimal` | 0.0025 | Gewinn, bevor Break-Even aktiviert wird. |
| `BreakEvenOffset` | `decimal` | 0.0 | Offset am Einstiegspreis beim Break-Even-Schließen. |
| `MacdFastPeriod` | `int` | 12 | Schnelle EMA-Länge im MACD. |
| `MacdSlowPeriod` | `int` | 26 | Langsame EMA-Länge im MACD. |
| `MacdSignalPeriod` | `int` | 9 | Signal-EMA-Länge im MACD. |

Alle Abstände sind absolute Preisabstände. Passen Sie sie an die Tick-Größe des Symbols an (z.B. 0.001 ≈ 10 Punkte bei einem 5-stelligen FX-Kurs).

## Verwendung

1. Fügen Sie die Strategie einem Wertpapier hinzu und setzen Sie die Kerzentypen. Es ist möglich, denselben Kerzentyp für mehrere Filter zu verwenden, wenn ein einzelner Zeitrahmen gewünscht wird.
2. Passen Sie `AnchorPeriod` und Preisabstände an die Volatilität des Instruments an.
3. Aktivieren oder deaktivieren Sie Break-Even/Trailing gemäß Ihrer Risikorichtlinie.
4. Starten Sie die Strategie; sie abonniert automatisch die notwendigen Kerzenströme und verwaltet Positionen mit Marktorders.

## Dateien

- `CS/GannGridStrategy.cs` – Strategieimplementierung.
- `README.md` – diese Dokumentation.
- `README_ru.md` – russische Beschreibung.
- `README_zh.md` – chinesische Beschreibung.
