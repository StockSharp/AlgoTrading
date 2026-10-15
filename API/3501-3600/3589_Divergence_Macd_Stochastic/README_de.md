# Divergenz MACD Stochastic Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erstellt den MetaTrader 5 Expert Advisor **"Divergence EA pip sl tp"** im StockSharp-Framework neu. Der Algorithmus sucht nach klassischen Abweichungen zwischen der Preisbewegung und dem MACD-Histogramm und validiert das Signal dann mit einem überkauften/überverkauften Stochastic-Oszillatorfilter, bevor er Umkehrgeschäfte eröffnet.

## Handelslogik

1. Abonnieren Sie die durch den Parameter `CandleType` ausgewählten primären Zeitrahmenkerzen.
2. Berechnen Sie das MACD-Histogramm (`MACD line - Signal line`) und die Stochastic %K/%D-Werte für jede fertige Kerze.
3. Verfolgen Sie die letzten beiden Swing-Hochs und -Tiefs der Preis- und Histogrammwerte.
4. **Bearische Divergenz**: Ein neues höheres Preishoch, begleitet von einem niedrigeren MACD-Histogramm-Höhepunkt und Stochastic %K über `StochasticUpperLevel`, löst eine Short-Position aus oder kehrt eine bestehende Long-Position um.
5. **Bulnische Divergenz**: Ein neues niedrigeres Preistief mit einem höheren MACD-Histogrammtief und %K unter `StochasticLowerLevel` eröffnet oder kehrt sich in eine Long-Position um.
6. Optionale Schutzmaßnahmen `TakeProfitSteps` und `StopLossSteps` werden in StockSharp Schritteinheiten umgewandelt und einmalig aktiviert, wenn die Strategie startet.

## Hinweise zur Implementierung

- Gebaut mit StockSharp High-Level-API unter Verwendung eines einzelnen Kerzenabonnements, das an die Indikatoren `MovingAverageConvergenceDivergenceSignal` und `StochasticOscillator` gebunden ist.
- Behält den Divergenzstatus bei, ohne Indikator-`GetValue`-Helfer aufzurufen, und entspricht den Konvertierungsrichtlinien.
- Die Diagrammintegration zeigt Preiskerzen, MACD- und Stochastic-Linien an, wenn ein Diagrammbereich verfügbar ist.
- Positionen werden umgekehrt, indem die absolute aktuelle Positionsgröße zur Basis `Volume` addiert wird, wodurch sofortige Richtungsänderungen nach bestätigten Divergenzen sichergestellt werden.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Für Divergenzberechnungen verwendeter Zeitrahmen. | 1-Stunden-Kerzen |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD EMA Längen, die die ursprünglichen EA-Eingaben replizieren. | 26.12.9 |
| `MacdDivergenceThreshold` | Minimale Histogrammdifferenz zwischen aufeinanderfolgenden Schwankungen, die zur Bestätigung der Divergenz erforderlich ist. | 0,0005 |
| `StochasticLength` | Schnelle %K-Periode des Stochastic-Oszillators. | 50 |
| `StochasticSlowK`, `StochasticSlowD` | Zusätzliche %K/%D-Glättungslängen, die die EA-Konfiguration widerspiegeln. | 9 / 9 |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Überkauft- und Überverkauft-Filter zur Validierung bärischer/bullischer Setups. | 80/20 |
| `TakeProfitSteps`, `StopLossSteps` | Optionale Schutzabstände, ausgedrückt in Preisschritten (0 deaktiviert die Stufe). | 50 |

## Nutzung

1. Hängen Sie die Strategie an einen StockSharp-Connector mit einem Wertpapier an, das den ausgewählten Zeitrahmen unterstützt.
2. Konfigurieren Sie die Positionsgröße über die Basiseigenschaft `Volume` und passen Sie die Indikatoreinstellungen nach Bedarf an.
3. Starten Sie die Strategie – Aufträge werden automatisch generiert, wenn die Divergenz- und Stochastic-Bedingungen erfüllt sind.
