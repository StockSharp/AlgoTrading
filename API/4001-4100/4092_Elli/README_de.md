# Elli-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Elli-Strategie portiert den MetaTrader 4-Expertenberater „Elli“ auf das StockSharp-Hochniveau API. Der ursprüngliche Roboter kombinierte die Ichimoku-Kinko-Hyo-Struktur im H1-Zeitrahmen mit einem Filter für den niedrigeren Zeitrahmen ADX und strengen Risikoparametern. Die Konvertierung behält die gleiche Richtungslogik bei, ersetzt die manuelle Auftragsverwaltung durch `StartProtection` und stellt jeden Einstellknopf als optimierbaren `StrategyParam<T>` bereit, sodass das Verhalten an verschiedene Märkte angepasst werden kann.

## Handelslogik
1. **Ichimoku Trendstruktur**
   - Die Strategie abonniert den durch `CandleType` definierten Zeitrahmen (standardmäßig H1) und berechnet Tenkan-sen-, Kijun-sen- und Senkou-Spannen unter Verwendung der ursprünglichen Zeiträume (19, 60, 120).
   - Ein bullisches Setup erfordert Tenkan > Kijun > Senkou Span A > Senkou Span B, wobei die Kerze über Kijun schließt. Bärische Setups spiegeln diesen Zustand wider.
   - Die absolute Entfernung zwischen Tenkan und Kijun muss mehr als `TenkanKijunGapPips` Pips betragen, um flache oder weitläufige Wolken zu vermeiden.
2. **Bestätigung der Richtungsbewegung**
   - Ein zweites Kerzenabonnement führt den Average Directional Index in dem durch `AdxCandleType` angegebenen Zeitrahmen aus (standardmäßig M1).
   - Lange Signale sind nur zulässig, wenn der vorherige +DI-Wert unter `ConvertLow` liegt und der aktuelle +DI über `ConvertHigh` liegt. Shorts erfordern die gleiche Beziehung für die −DI-Komponente und replizieren den im MT4-Code vorhandenen Beschleunigungsfilter.
3. **Eintrittsausführung**
   - Wenn alle Filter übereinstimmen, gibt die Strategie eine Marktorder mit dem Volumen `OrderVolume + |Position|` aus. Dadurch wird jedes entgegengesetzte Exposure automatisch geschlossen, bevor Sie sich dem Trend anschließen.
   - Es wird jeweils nur eine Richtungsbelichtung beibehalten, die dem ursprünglichen `OrdersTotal() < 1`-Schutz folgt.
4. **Risikomanagement**
   - `StartProtection` fügt symmetrische Stop-Loss- und Take-Profit-Orders hinzu, die aus Pip-Abständen unter Verwendung der Pip-Größe des Instruments konvertiert werden.
   - Die Position wird ansonsten passiv verwaltet, sodass die Schutzaufträge genau wie der MT4-Expertenberater Exits abwickeln können.

## Indikatoren und Datenabonnements
- Primäre Kerzen: `CandleType` (Standard-1-Stunden-Kerzen) für die Ichimoku-Verarbeitung.
- ADX Kerzen: `AdxCandleType` (Standardkerzen von 1 Minute) für DI-Beschleunigungsprüfungen.
- Indikatoren: `Ichimoku` (Tenkan, Kijun, Senkou Span B) und `AverageDirectionalIndex` (liefert +DI/−DI).
- Beide Abonnements unterstützen das Rendern von Diagrammen über `DrawCandles`, `DrawIndicator` und `DrawOwnTrades`, sofern ein Diagrammbereich verfügbar ist.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | `1` | Basis-Market-Order-Volumen. |
| `TakeProfitPips` | `60` | Take-Profit-Distanz, ausgedrückt in Pips. |
| `StopLossPips` | `30` | Stop-Loss-Distanz, ausgedrückt in Pips. |
| `TenkanPeriod` | `19` | Tenkan-sen-Periode für den Ichimoku-Indikator. |
| `KijunPeriod` | `60` | Kijun-Sen-Periode für den Ichimoku-Indikator. |
| `SenkouSpanBPeriod` | `120` | Senkou Span B-Periode für die Ichimoku-Wolke. |
| `TenkanKijunGapPips` | `20` | Mindestabstand zwischen Tenkan und Kijun (in Pips), der vor dem Handel erforderlich ist. |
| `ConvertHigh` | `13` | DI-Schwellenwert, den der aktuelle Wert überschreiten muss, um den Impuls zu bestätigen. |
| `ConvertLow` | `6` | DI-Schwellenwert, unter dem der vorherige Wert bleiben muss, bevor ein neuer Handel erfolgen kann. |
| `AdxPeriod` | `10` | Zeitraum, der für die ADX-Berechnung verwendet wird. |
| `CandleType` | `H1` | Zeitrahmen, der die Ichimoku-Berechnung steuert. |
| `AdxCandleType` | `M1` | Zeitrahmen für ADX und DI-Überwachung. |

Alle Parameter werden mit `StrategyParam<T>`-Helfern implementiert, was Optimierungen und Laufzeitanpassungen innerhalb von StockSharp Designer ermöglicht.

## Implementierungshinweise
- Die Pip-Umrechnung folgt der Standard-Forex-Konvention (0,0001 für 5-stellige Kurse und 0,01 für 3-stellige Instrumente), um die ursprünglichen Pip-basierten Schwellenwerte beizubehalten.
- ADX-Werte werden in `_latestPlusDi`, `_previousPlusDi`, `_latestMinusDi` und `_previousMinusDi` zwischengespeichert, um sicherzustellen, dass die DI-Beschleunigungsprüfung mit den MQL `iADX`-Aufrufen mit den Schichten 0 und 1 übereinstimmt.
- `IsFormedAndOnlineAndAllowTrading()` blockiert Signale, bis die Strategie, Indikatoren und Datenfeeds bereit sind, und verhindert so vorzeitige Trades während der Aufwärmphase.
- Markteintritte basieren auf `Volume + Math.Abs(Position)`, sodass Richtungsänderungen bestehende Trades sofort abflachen und das Einzelpositionsverhalten des MT4-Skripts nachahmen.
