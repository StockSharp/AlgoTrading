# AbsolutelyNoLag Lwma Digit MMRec-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Konzept

Diese Strategie ist ein StockSharp-Port des MetaTrader-Experten *Exp_AbsolutelyNoLagLwma_Digit_NN3_MMRec*. Sie behält die ursprüngliche Multi-Timeframe-Architektur rund um den "AbsolutelyNoLagLWMA"-Indikator bei und reproduziert die Money-Management-Recovery-Regeln (`MMRec`). Drei unabhängige Module (A/B/C) überwachen 12-Stunden-, 4-Stunden- und 2-Stunden-Kerzen jeweils. Jedes Modul kann seine eigene Positionsscheibe öffnen und schließen, während die Strategie das kombinierte Exposure verfolgt.

Jedes Modul berechnet einen doppelten gewichteten gleitenden Durchschnitt (WMA eines WMA) einer konfigurierbaren Preisquelle. Der geglättete Wert wird auf die gewünschte Anzahl von Stellen gerundet, genau wie im MQL-Indikator. Eine Änderung der Steigung der geglätteten Linie (Wert steigt nach einem Fall oder umgekehrt) wird als Richtungswechsel behandelt und erzeugt Trading-Aktionen für dieses Modul.

## Trading-Regeln

1. Warten auf eine abgeschlossene Kerze des Modul-Zeitrahmens.
2. Den angewendeten Preis lesen (Schlusskurs, Eröffnung, Median, Typisch usw.).
3. Den Preis durch den primären WMA verarbeiten und das Ergebnis in einen sekundären WMA einspeisen, um "AbsolutelyNoLagLWMA" zu emulieren.
4. Den geglätteten Wert auf die konfigurierte Anzahl von Stellen runden und mit dem vorherigen Wert vergleichen.
5. **Aufwärtssteigung** (`value > previous`):
   - Den Short-Anteil des Moduls schließen, wenn Short-Exits aktiviert sind.
   - Wenn Long-Einstiege aktiviert sind und kein Long-Exposure aktiv ist, eine Long-Position mit dem aktuellen Modulvolumen öffnen.
   - Stop-Loss- und Take-Profit-Levels (in Preisschritten ausgedrückt) für den Long-Anteil neu berechnen.
6. **Abwärtssteigung** (`value < previous`):
   - Den Long-Anteil des Moduls schließen, wenn Long-Exits aktiviert sind.
   - Wenn Short-Einstiege aktiviert sind und kein Short-Exposure aktiv ist, eine Short-Position öffnen.
   - Die Schutzlevels für den Short-Anteil aktualisieren.
7. Bei jeder Kerze prüft das Modul, ob das Hoch/Tief der Kerze den aktuellen Stop-Loss- oder Take-Profit-Level durchstochen hat. Wenn berührt, wird der Positionsanteil zu diesem Preis glattgestellt und das Trade-Ergebnis wird für die Money-Management-Logik aufgezeichnet.
8. Das Money-Management führt eine Warteschlange der jüngsten Trade-Ergebnisse für jede Richtung. Wenn die letzten *N* Trades (wobei *N* dem Loss-Trigger entspricht) Verluste waren, verwendet die nächste Order das reduzierte Volumen; andernfalls wird das normale Volumen verwendet. Die Verlust-Trade-Erkennung basiert auf dem Eintrittspreis, der beim Öffnen des Anteils gespeichert wurde, und dem Austrittspreis (Stop/Ziel/Schließung), der zum Glattstellen verwendet wurde.

Die Strategie verwendet Marktorders für Ein- und Ausstiege und geht von Ausführungen zum Kerzenschlusskurs für Signale und zum Schutzpreis für Stop/Ziel-Exits aus.

## Parameter

Jedes Modul besitzt einen identischen Parametersatz. Die Standardwerte entsprechen dem MQL-Quellexperten.

| Parameter | Beschreibung |
|-----------|--------------|
| `ACandleType` / `BCandleType` / `CCandleType` | Zeitrahmen der Modulkerzen (standardmäßig 12h / 4h / 2h). |
| `ALength` / `BLength` / `CLength` | Länge der AbsolutelyNoLagLWMA-Glättung (auf beide WMAs angewendet). |
| `AAppliedPrice` / `BAppliedPrice` / `CAppliedPrice` | Im Indikator verwendete Preisquelle (close, open, high, low, median, typical, weighted, simple, quarter, TrendFollow1, TrendFollow2, Demark). |
| `ADigits` / `BDigits` / `CDigits` | Anzahl der Stellen zum Runden des geglätteten Werts. |
| `ABuyOpen`, `ASellOpen`, `ABuyClose`, `ASellClose` (und Modul-B/C-Äquivalente) | Flags, die steuern, ob das Modul Long- oder Short-Anteile öffnen/schließen darf. |
| `ASmallVolume`, `ANormalVolume` | Reduziertes und normales Ordervolumen. Dieselben Werte werden für Short-Trades wiederverwendet. |
| `ABuyLossTrigger`, `ASellLossTrigger` | Anzahl aufeinanderfolgender Verlust-Trades, die das reduzierte Volumen für Longs/Shorts aktiviert. |
| `AStopLossPoints`, `ATakeProfitPoints` | Schutzlevels in Preisschritten für den Modulanteil. Identische Parameter existieren für Module B und C. |

Die Money-Management-Warteschlangen werden zurückgesetzt, wenn der entsprechende Trigger auf null gesetzt wird. Die Preisschrittberechnungen basieren auf `Security.Step`; wenn das Instrument dies nicht bereitstellt, wird ein Schritt von `1` verwendet.

## Hinweise

- Jedes Modul verwaltet sein eigenes internes Positionsvolumen; daher können verschiedene Module gleichzeitig long oder short sein. Die Hauptstrategie-Position ist die Summe aller Modulanteile.
- Stop-Loss- und Take-Profit-Levels werden bei jeder abgeschlossenen Kerze anhand des Hochs/Tiefs der Kerze auf Durchbrüche überprüft.
- Die `AppliedPrices`-Enumeration entspricht den Optionen des ursprünglichen Indikators, einschließlich beider TrendFollow-Formeln und der DeMark-Variante.
- Die Strategie fügt dem Chart keine Indikatoren hinzu; sie verlässt sich auf die High-Level-`Bind`-API und hält Indikatorinstanzen gemäß den Richtlinien privat für jedes Modul.
- Die Logik schließt und öffnet Trades nur, wenn die Steigung die Richtung wechselt, was doppelte Orders bei aufeinanderfolgenden Balken mit demselben Trendzustand verhindert.
