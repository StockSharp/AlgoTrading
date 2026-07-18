# JBrainUltraRSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Beispielstrategie kombiniert den Relative Strength Index (RSI) und den Stochastik-Oszillator zur Generierung von Handelssignalen.
Die Idee leitet sich vom ursprünglichen MetaTrader Expert Advisor ab, der die Indikatoren *JBrainTrendSig1* und *UltraRSI* verwendete. In dieser Anpassung dient der Stochastik-Oszillator als Trendfilter, während der RSI Einstiegssignale liefert.

## Funktionsweise

1. **Indikatoren**
   - **RSI**: Misst den Momentum durch Vergleich aktueller Gewinne und Verluste. Ein Kreuz über das Niveau 50 zeigt bullischen Momentum an, während ein Kreuz unter 50 bärischen Momentum anzeigt.
   - **Stochastik-Oszillator**: Bewertet die Position des Schlusskurses relativ zur jüngsten Handelsspanne. Kreuzungen der %K- und %D-Linien bestätigen die Trendrichtung.
2. **Modi**
   - **JBrainSig1Filter** – RSI generiert Signale und der Stochastik-Oszillator bestätigt die Richtung.
   - **UltraRsiFilter** – Der Stochastik-Oszillator liefert Signale, gefiltert durch den RSI.
   - **Composition** – Signale werden nur genommen, wenn beide Indikatoren in der Richtung übereinstimmen.
3. **Handelsregeln**
   - Eine Long-Position wird eröffnet, wenn ein Kaufsignal erscheint und keine Short-Position vorhanden oder offen ist.
   - Eine Short-Position wird eröffnet, wenn ein Verkaufssignal erscheint und keine Long-Position vorhanden oder offen ist.
   - Umkehrsignale schließen bestehende Positionen, wenn erlaubt.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `RsiPeriod` | RSI-Berechnungszeitraum. |
| `StochLength` | %K-Zeitraum für den Stochastik-Oszillator. |
| `SignalLength` | %D-Zeitraum für den Stochastik-Oszillator. |
| `Mode` | Modus zur Kombination der Indikatoren. |
| `AllowLongEntry` / `AllowShortEntry` | Berechtigungen zum Öffnen von Long- oder Short-Positionen. |
| `AllowLongExit` / `AllowShortExit` | Berechtigungen zum Schließen von Long- oder Short-Positionen. |
| `CandleType` | Kerzen-Zeitrahmen, der von der Strategie verwendet wird. |

## Hinweise

- Die Strategie verwendet die High-Level-API von StockSharp mit `Bind` / `BindEx` für die Indikatorverarbeitung.
- Stops und Ziele können mit dem eingebauten Schutzmechanismus `StartProtection()` konfiguriert werden.
- Die Beispielvisualisierung zeichnet Kerzen, Indikatoren und eigene Trades, wenn ein Diagrammbereich verfügbar ist.
