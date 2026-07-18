# Mean-Reversion-Donchian-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine Portierung des MetaTrader-Expertenberaters `MeanReversion.mq5`. Dabei handelt es sich um ein einfaches Mean-Reversion-Muster: Immer wenn der Preis innerhalb des ausgewählten Lookback-Fensters ein neues Tief erreicht, eröffnet die Strategie eine Long-Position und zielt auf die Mitte der aktuellen Spanne. Wenn ein neues Hoch erscheint, spiegelt die Strategie die Logik auf der Short-Seite wider. Die Positionsgröße wird aus dem Risikoprozentsatz und der Stoppdistanz berechnet und ist eine weitgehende Nachbildung der Lotberechnung, die das ursprüngliche EA durchführt.

## Handelslogik
1. Erstellen Sie einen Donchian-Kanal mit dem konfigurierten Kerzentyp und Lookback-Zeitraum. Das obere Band markiert das höchste Hoch und das untere Band das niedrigste Tief über dem Fenster. Der Mittelpunkt `(upper + lower) / 2` fungiert als mittleres Umkehrziel.
2. Wenn die aktuelle fertige Kerze ein neues Tief (`Low <= LowerBand`) erreicht und keine Position offen ist, kauft die Strategie zum Markt. Der Schutzstopp spiegelt sich um den Einstiegspreis wider, sodass der Mittelpunkt zum Gewinnziel wird und der MetaTrader-Berechnung `sl = 2 * Ask - tp` entspricht.
3. Wenn die Kerze ein neues Hoch (`High >= UpperBand`) erreicht und keine Position offen ist, wird die Strategie zum Marktpreis mit einem symmetrischen Stop über dem Preis verkauft. Der Mittelpunkt fungiert wiederum als Take-Profit-Niveau.
4. Stop-Loss und Take-Profit werden bei jeder fertigen Kerze überwacht. Bei einem Ausbruch über den Stop hinaus wird die Position sofort geschlossen, während bei Erreichen des Mittelpunkts der Handel am beabsichtigten Ziel beendet wird. Der interne Zustand wird automatisch zurückgesetzt, wenn die Position flach ist.

## Positionsgrößen
* Das Risiko pro Trade beträgt `Portfolio.CurrentValue * (RiskPercent / 100)`. Liegen keine Portfoliodaten vor, greift die Strategie auf das minimal handelbare Volumen zurück.
* Das Vertragsrisiko wird mit `|EntryPrice - StopPrice|` gemessen. Das Rohvolumen beträgt `RiskAmount / perUnitRisk` und wird auf den Lautstärkeschritt des Instruments normalisiert. Die minimalen und maximalen Wechselkursbeschränkungen werden eingehalten. Wenn das normalisierte Volumen kleiner als die minimale handelbare Größe ist, wird stattdessen das Minimum verwendet.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzentyp und Zeitrahmen, die für den Aufbau des Donchian-Kanals verwendet werden. | 15-minütiger Zeitrahmen |
| `LookbackPeriod` | Anzahl der Kerzen, die zur Berechnung des höchsten Hochs und niedrigsten Tiefs verwendet werden. | 200 |
| `RiskPercent` | Prozentsatz des pro Trade riskierten Portfolio-Eigenkapitals. | 1 % |

Alle Parameter unterstützen die Optimierung durch den integrierten Optimierer.

## Zusätzliche Hinweise
* Die Strategie handelt jeweils nur eine Position und repliziert den `PositionsTotal()>0`-Guard aus der MQL-Version.
* Stop-Loss- und Take-Profit-Preise werden intern verwaltet, anstatt separate Aufträge zu senden, wodurch die Logik nah am ursprünglichen Expert Advisor bleibt und gleichzeitig mit dem High-Level-API kompatibel bleibt.
* Wenn Informationen zum Portfolio-Aktien- oder Instrumentenvolumen fehlen, handelt die Strategie immer noch mit dem kleinstmöglichen Volumen, um das Verhalten deterministisch zu halten.
