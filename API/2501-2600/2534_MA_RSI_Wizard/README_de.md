# MA + RSI Wizard-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist der StockSharp-Port des MetaTrader 5-Experten "MQL5 Wizard MA RSI" aus dem Ordner `MQL/17489`. Der ursprüngliche Roboter kombiniert einen gleitenden Durchschnittsfilter mit einem RSI-Filter und öffnet Trades, wenn die gewichtete Summe der Filter konfigurierbare Schwellenwerte kreuzt. Die C#-Version behält dieselbe Struktur bei und drückt die Logik mit der High-Level-API von StockSharp und modernen Risikomanagement-Helfern aus.

Der Bot arbeitet auf jedem Instrument, das OHLCV-Kerzen bereitstellt. Er wertet einen gleitenden Durchschnitt aus, der um eine benutzerdefinierte Anzahl von Bars verschoben werden kann, und einen RSI, der mit verschiedenen Preisquellen gespeist werden kann. Beide Indikatoren tragen zu einer zusammengesetzten Punktzahl bei. Eine Position wird geöffnet, sobald die Punktzahl den Öffnungsschwellenwert überschreitet, und geschlossen, wenn die entgegengesetzte Punktzahl den Schließschwellenwert erreicht. Optionale Abstands-, Stop-Loss- und Take-Profit-Einstellungen replizieren die Geldmanagement-Parameter des ursprünglichen Expert Advisors.

## Indikatoren und Scoring

* **Gleitender Durchschnitt** – konfigurierbarer Zeitraum, Methode (einfach, exponentiell, geglättet, linear gewichtet), Preisquelle und Vorwärtsverschiebung. Wenn der Schlusskurs über dem verschobenen Durchschnitt liegt, entspricht der MA-Score 100, andernfalls 0.
* **Relative Strength Index (RSI)** – konfigurierbarer Zeitraum und Preisquelle. Der RSI-Beitrag wächst linear von 0 bei RSI = 50 auf 100 bei RSI = 100 für Long-Signale und spiegelt dasselbe Verhalten für Short-Signale wider.
* **Zusammengesetzte Punktzahl** – die MA- und RSI-Scores werden durch `MaWeight` und `RsiWeight` gewichtet. Der Endwert ist der gewichtete Durchschnitt `score = (maScore * MaWeight + rsiScore * RsiWeight) / (MaWeight + RsiWeight)`, der im Intervall [0;100] bleibt, genau wie in der MetaTrader-Version.
* **Preisabstandsfilter** – `PriceLevelPoints` definiert den Mindestabstand zwischen dem Kerzenschlusskurs und dem verschobenen gleitenden Durchschnitt (in Preis umgerechnet über den Instrument-Schritt). Signale, die näher als der Schwellenwert liegen, werden ignoriert.

## Handelsregeln

1. Jede abgeschlossene Kerze aktualisiert die Indikatoren und Scores.
2. Wenn der entgegengesetzte Score `ThresholdClose` verletzt, wird die aktuelle Position zum Markt geschlossen.
3. Long-Einstieg – erlaubt, wenn kein Long-Exposure besteht, der Long-Score mindestens `ThresholdOpen` ist, die Abklingzeit (`ExpirationBars`) vergangen ist und der Preisabstandsfilter erfüllt ist. Die Ordergröße entspricht `Volume + |Position|`, was automatisch eine Short-Position dreht, wenn nötig.
4. Short-Einstieg – symmetrisch zur Long-Logik.
5. Optionale `StartProtection` wendet Stop-Loss und Take-Profit unter Verwendung absoluter Preispunkte an.

## Risikomanagement

Die Strategie aktiviert `StartProtection` einmal beim Start. Abstände werden in Preispunkten definiert (`StopLevelPoints`, `TakeLevelPoints`) und mit dem aktuellen `Security.PriceStep` übersetzt. Beide Werte können auf null gesetzt werden, um den entsprechenden Schutz zu deaktivieren. Der Abklingzeitparameter verhindert sofortige Wiedereinstiege in dieselbe Richtung und emuliert die Einstellung für das Auslaufen von Pending Orders des ursprünglichen EA.

## Parameter

| Parameter | Beschreibung | Standardwerte |
|-----------|-------------|---------|
| `CandleType` | Für die Analyse verwendete Datenserie. | 15-Minuten-Zeitrahmen |
| `ThresholdOpen` | Minimale gewichtete Punktzahl zum Öffnen einer Position. | 55 |
| `ThresholdClose` | Minimale entgegengesetzte Punktzahl zum Schließen einer Position. | 100 |
| `PriceLevelPoints` | Erforderlicher Abstand zwischen Preis und verschobenem MA (in Punkten). | 0 |
| `StopLevelPoints` | Stop-Loss-Abstand (Punkte). | 50 |
| `TakeLevelPoints` | Take-Profit-Abstand (Punkte). | 50 |
| `ExpirationBars` | Abklingzeit in Bars vor dem Wiedereinstieg in dieselbe Richtung. | 4 |
| `MaPeriod` | Gleitender Durchschnittszeitraum. | 20 |
| `MaShift` | Auf den MA-Ausgang angewendete Vorwärtsverschiebung (Bars). | 3 |
| `MaMethods` | Gleitende Durchschnittsmethode (Simple, Exponential, Smoothed, LinearWeighted). | Simple |
| `MaAppliedPrice` | Preisquelle für den MA. | Close |
| `MaWeight` | Dem MA-Score zugewiesenes Gewicht. | 0.8 |
| `RsiPeriod` | RSI-Zeitraum. | 3 |
| `RsiAppliedPrice` | Preisquelle für den RSI. | Close |
| `RsiWeight` | Dem RSI-Score zugewiesenes Gewicht. | 0.5 |

## Hinweise

* Die Strategie läuft strikt auf abgeschlossenen Kerzen und ignoriert Teilaktualisierungen.
* Das Setzen beider Indikatorgewichte auf null deaktiviert den Handel, da die kombinierte Punktzahl die Schwellenwerte nicht mehr erreichen kann.
* Abklingzeit (`ExpirationBars`) gleich null erlaubt mehrere Einträge in dieselbe Richtung ohne Wartezeit.
* Da StockSharp standardmäßig Marktorders ausführt, wird das Auslaufen von Pending Orders des ursprünglichen EA durch den Abklingzeitmechanismus anstelle der tatsächlichen Orderstornierung dargestellt.
