# Zwei-MA-Vier-Stufen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Experten "2MA_4Level" unter Verwendung der StockSharp High-Level-API. Sie handelt ein einzelnes Instrument mit zwei geglätteten gleitenden Durchschnitten (SMMA), die auf dem Medianpreis berechnet werden, und überwacht fünf relative Kreuzungszonen zwischen der schnellen und langsamen Kurve. Einstiege sind nur erlaubt, wenn keine Position offen ist, und jeder Handel wird durch pip-basierte Stop-Loss- und Take-Profit-Offsets geschützt.

## Logik

- Eine schnelle und eine langsame SMMA werden auf der ausgewählten Kerzenserie berechnet (standardmäßig 50 und 130 Perioden).
- Die vorherigen und aktuellen SMMA-Werte der abgeschlossenen Kerze werden ausgewertet, um einen Kreuzungspunkt zu erkennen.
- Die Kreuzung wird gegen fünf Schwellenwerte geprüft, die aus dem langsamen MA aufgebaut werden:
  - der reine langsame MA (ohne Offset),
  - langsamer MA + `MostTopLevel` Pips,
  - langsamer MA + `TopLevel` Pips,
  - langsamer MA - `LowermostLevel` Pips,
  - langsamer MA - `LowerLevel` Pips.
- Wenn der schnelle MA über einen Schwellenwert kreuzt, wird eine Long-Position eröffnet (wenn flat). Ein Kreuz unterhalb eines Schwellenwerts öffnet eine Short-Position.
- Stop-Loss- und Take-Profit-Levels werden über `StartProtection` unter Verwendung des Instrument-Pip-Werts (`Security.PriceStep`) angehängt.

Die Strategie pyramidiert keine Positionen: Ein neuer Handel kann erst nach Schließung des vorherigen durch Stop oder Ziel eröffnet werden.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `FastPeriod` | 50 | Länge des schnellen geglätteten gleitenden Durchschnitts. Muss kleiner als `SlowPeriod` sein. |
| `SlowPeriod` | 130 | Länge des langsamen geglätteten gleitenden Durchschnitts. |
| `MostTopLevel` | 500 | Oberer Offset (in Pips) für die weiteste bullische/bärische Bestätigung. Muss größer als `TopLevel` sein. |
| `TopLevel` | 250 | Oberer Offset (in Pips) für die sekundäre bullische/bärische Bestätigung. |
| `LowerLevel` | 250 | Unterer Offset (in Pips) für die sekundäre bärische/bullische Bestätigung. Muss kleiner als `LowermostLevel` sein. |
| `LowermostLevel` | 500 | Unterer Offset (in Pips) für die weiteste bärische/bullische Bestätigung. |
| `TakeProfitPips` | 55 | Abstand vom Einstieg zum Take-Profit, ausgedrückt in Pips. |
| `StopLossPips` | 260 | Abstand vom Einstieg zum Stop-Loss, ausgedrückt in Pips. |
| `CandleType` | 15-Minuten-Zeitrahmen | Kerzenserie für SMMA-Berechnungen und Signalverarbeitung. |

## Implementierungsdetails

- Der Medianpreis (`(High + Low) / 2`) speist beide SMMAs und entspricht der MT5-Konfiguration mit `PRICE_MEDIAN`.
- Der Kreuzungstest vergleicht die letzte abgeschlossene Kerze mit der vorherigen, wodurch jede Abhängigkeit von teilweise gebildeten Bars entfällt.
- `StartProtection` verbindet Stop-Loss und Take-Profit einmalig beim Start, sodass jede Order automatisch die konfigurierten Risikolimits erbt.
- Die Strategie stoppt sich während `OnStarted`, wenn ungültige Parameterkombinationen angegeben werden (z.B. `FastPeriod >= SlowPeriod`).

## Verwendungshinweise

1. Verbinden Sie die Strategie mit einem Instrument mit definiertem `PriceStep`; andernfalls fällt die Pip-Konvertierung auf den Wert `1` zurück.
2. Geeignet für Hedging-Konten in MT5; in StockSharp verhält es sich gleich, indem jeweils nur eine offene Position sichergestellt wird.
3. Optimierungs-Hooks (`SetCanOptimize`) sind für beide MA-Perioden aktiviert, sodass Sie Parameter-Sweeps direkt vom StockSharp-Optimizer ausführen können.
4. Da die Strategie ausschließlich auf Stop-Loss- und Take-Profit-Ausstiegen basiert, stellen Sie sicher, dass die konfigurierten Abstände mit der Instrumentenvolatilität übereinstimmen, um längere Exponierungszeiten zu vermeiden.

## Dateien

- `CS/TwoMaFourLevelStrategy.cs` – C#-Implementierung der Handelslogik.
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.
