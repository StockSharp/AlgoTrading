# FuzzyLogic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die FuzzyLogic-Strategie repliziert den MT5-Expertenberater **Fuzzy logic (barabashkakvn's Edition)** mithilfe der High-Level-API von StockSharp. Das System misst Trendstärke und Momentum-Erschöpfung mit Bill-Williams-Oszillatoren und Momentum-Indikatoren, wandelt diese Werte in Fuzzy-Zugehörigkeitswerte um und aggregiert sie zu einem einzelnen Entscheidungswert zwischen 0 und 1.

Handelsaktionen werden ausgelöst, wenn der Fuzzy-Score kalibrierte Schwellenwerte überschreitet:

- **Decision &gt; 0.75** – Short-Position eröffnen (starke Erschöpfung / überkaufte Bedingungen).
- **Decision &lt; 0.25** – Long-Position eröffnen (starkes bullisches Umkehrsetup).

Positionen werden mit festen Take-Profit- und Stop-Loss-Abständen in Preisschritten verwaltet. Wenn ein Trailing-Stop-Abstand angegeben wird, wird der Schutz-Stop in einen Trailing-Stop umgewandelt.

## Indikatorenstack

| Komponente | Zweck |
| --- | --- |
| **Gator-Oszillator** (aus Alligator-Linien aufgebaut) | Misst die Summe der Kiefer–Zähne- und Zähne–Lippen-Abstände, um Trendausweitung oder -kontraktion zu beurteilen. |
| **Williams %R (14)** | Erkennt überkaufte / überverkaufte Niveaus. |
| **Acceleration/Deceleration Oscillator (AC)** | Zählt aufeinanderfolgende Momentum-Verschiebungen zur Schätzung der Trendbeschleunigung. |
| **DeMarker (14)** | Bestätigt Erschöpfung durch Hoch/Tief-Vergleiche. Direkt innerhalb der Strategie implementiert. |
| **RSI (14)** | Verfolgt klassische Momentum-Schwingungen. |

Alligator-Linien werden mit geglätteten gleitenden Durchschnitten berechnet und genau wie im Original-Expertenberater vorwärts verschoben, um den Gator-Oszillator zu reproduzieren. AC-Werte werden aus dem Awesome Oscillator (5/34 SMA-Differenz) minus seinem 5-Perioden-Gleitenden Durchschnitt abgeleitet und liefern identische Werte wie MT5s `iAC`-Indikator.

## Handelslogik

1. Jeder Indikatorwert wird auf fünf Fuzzy-Zugehörigkeitsmengen abgebildet (sehr bärisch → sehr bullisch). Stückweise lineare Funktionen replizieren die ursprünglichen MT5-Arrays.
2. Die fünf Zugehörigkeitsgruppen werden gewichtet (0.133, 0.133, 0.133, 0.268, 0.333) und in vier Zusammenfassungsbins aggregiert.
3. Der Fuzzy-Entscheidungswert wird als `Σ summary[x] * (0.2 * (x + 1) - 0.1)` berechnet und ergibt Werte im Bereich `[0, 1]`.
4. Signale werden einmal pro abgeschlossener Kerze ausgewertet. Die Strategie bleibt flach, sofern die Entscheidung die Einstiegsschwellen nicht überschreitet.
5. Die Ordergröße basiert auf der Eigenschaft `Volume` (Standard 1). Schutz-Stops werden über `StartProtection` registriert.

## Risikomanagement

- **StopLossPoints** – absoluter Abstand (in Preisschritten) für den Schutz-Stop. Wird verwendet, wenn `TrailingStopPoints` null ist.
- **TrailingStopPoints** – wenn &gt; 0, wechselt der Stop-Loss-Abstand auf diesen Wert und der Trailing-Modus wird aktiviert.
- **TakeProfitPoints** – absoluter Abstand für das Gewinnziel.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen / Kerzentyp für Berechnungen. |
| `BuyThreshold` | Fuzzy-Score, unterhalb dessen ein Long-Einstieg geöffnet wird. Standard 0.25. |
| `SellThreshold` | Fuzzy-Score, oberhalb dessen ein Short-Einstieg geöffnet wird. Standard 0.75. |
| `StopLossPoints` | Stop-Loss-Abstand in Instrument-Preisschritten. Standard 60. |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten. Standard 20. |
| `TrailingStopPoints` | Trailing-Stop-Abstand in Preisschritten. Standard 0 (deaktiviert). |
| `WilliamsPeriod` | Lookback für Williams %R. Standard 14. |
| `RsiPeriod` | Lookback für RSI. Standard 14. |
| `DeMarkerPeriod` | Lookback für die eingebettete DeMarker-Berechnung. Standard 14. |

## Implementierungshinweise

- Der DeMarker-Oszillator ist manuell implementiert, da StockSharp keine integrierte Version bereitstellt. Hoch- und Tief-Deltas werden in Warteschlangen gespeichert, um MT5-Summen zu reproduzieren.
- Der AC-Verlauf speichert die fünf zuletzt abgeschlossenen Werte, damit die Fuzzy-Logik aufeinanderfolgende Beschleunigungssequenzen prüfen kann, genau wie `iAC(..., shift)` in MT5.
- Alligator-Kiefer/Zähne/Lippen-Puffer führen dieselbe Vorwärtsverschiebung (8/5/3 Balken) ein, bevor die Gator-Histogrammwerte abgeleitet werden.
- Die Strategie öffnet nur dann eine neue Position, wenn `Position == 0`, und respektiert damit das Einzelpositionsverhalten des ursprünglichen Expertenberaters.

## Verwendungsschritte

1. Hängen Sie die Strategie in Designer/Backtester an ein Portfolio und ein Wertpapier an.
2. Konfigurieren Sie die gewünschte Kerzenserie über `CandleType`.
3. Passen Sie bei Bedarf Schwellenwerte oder Stop-Abstände an.
4. Starten Sie die Strategie; sie handelt automatisch, wenn der Fuzzy-Score die konfigurierten Niveaus überschreitet.
