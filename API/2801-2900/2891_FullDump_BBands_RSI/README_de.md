# FullDump BB RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein mehrstufiges Bollinger Bands- und RSI-System, konvertiert vom MT5-Experten "FullDump". Die Strategie wartet auf Impulserschöpfung, bestätigt eine Mean-Reversion-Ausrichtung mit Bollinger Bands und handelt nur, wenn der Kurs sich wieder mit der Mittellinie ausrichtet. Das Trade-Management spiegelt den Original-EA mit festen Stop-Loss-/Zielversätzen und einer Break-Even-Anpassung wider, wenn der Kurs zum gegenüberliegenden Band zurückkehrt.

## Übersicht

- **Märkte**: Jedes liquide Instrument, das Bollinger Bands und RSI unterstützt.
- **Zeitrahmen**: Konfigurierbarer Kerzentyp (Standard 15 Minuten).
- **Richtung**: Long/Short.
- **Ordertyp**: Marktaufträge mit vordefinierten Schutzniveaus.
- **Konzept**: Ausblenden kurzfristiger Extreme innerhalb der Bollinger-Hülle, während der Kurs zur Mittellinie zurückkehrt.

## Handelslogik

1. **RSI-Scan (Schritt 1)**
   - Long-Bedingung erfordert mindestens einen RSI-Wert unter 30 innerhalb des jüngsten Fensters.
   - Short-Bedingung erfordert mindestens einen RSI-Wert über 70 im gleichen Lookback.
2. **Band-Verletzung (Schritt 2)**
   - Long: Der aktuelle Schlusskurs muss unter oder gleich einem der jüngsten unteren Bandwerte liegen.
   - Short: Der aktuelle Schlusskurs muss über oder gleich einem der jüngsten oberen Bandwerte liegen.
3. **Mittelband-Ausrichtung (Schritt 3)**
   - Long-Trades werden erst ausgelöst, wenn der Kurs wieder über die Bollinger-Mittellinie schließt.
   - Short-Trades erfordern, dass der Schlusskurs unter der Mittellinie liegt.
4. **Einstiegsausführung**
   - Wenn alle Bedingungen übereinstimmen und keine Position in diese Richtung offen ist, wird eine Marktorder für das konfigurierte Volumen gesendet.

## Risikomanagement

- **Stop-Loss**: Unter (Long) oder über (Short) dem extremen Tief/Hoch des Lookback-Fensters minus/plus dem konfigurierten Einzugsversatz platziert.
- **Take-Profit**: Am aktuellen gegenüberliegenden Bollinger-Band plus demselben Einzugsversatz platziert.
- **Break-Even-Regel**: Sobald der Kurs das gegenüberliegende Band berührt, wird der Stop-Loss auf den Einstiegspreis verschoben, um die Position zu sichern.
- **Positionsausstieg**: Positionen schließen, wenn der Kurs die Stop-Loss- oder Take-Profit-Niveaus durchbricht; entgegengesetzte Signale flatten die aktuelle Position vor dem Richtungswechsel.

## Parameter

| Name | Beschreibung | Standard | Hinweise |
| --- | --- | --- | --- |
| `BandsPeriod` | Länge der Bollinger Bands-Berechnung. | 20 | Optimierbar (10 → 40 Schritt 1). |
| `RsiPeriod` | Durchschnittslänge für den RSI. | 14 | Optimierbar (7 → 21 Schritt 1). |
| `Depth` | Anzahl der für Bedingungen untersuchten jüngsten Kerzen. | 6 | Optimierbar (3 → 12 Schritt 1). |
| `IndentInPoints` | Versatz in Kursschritten zum Stop-Loss und Take-Profit hinzugefügt. | 10 | Optimierbar (5 → 30 Schritt 5). |
| `OrderVolume` | Ordergröße in Lots. | 1 | Für Einstiege und Ausstiege verwendet. |
| `CandleType` | Zeitrahmen der Eingangskerzen. | 15-Minuten-Kerzen | Ändern, um den Strategiehorizont anzupassen. |

## Filter und Tags

- **Kategorie**: Mean Reversion, Volatilitätsbänder.
- **Indikatoren**: Bollinger Bands, Relative Strength Index.
- **Stops**: Harter Stop, hartes Ziel, Break-Even-Anpassung.
- **Komplexität**: Mittel (Multi-Konditions-Logik mit statusbehafteter Verwaltung).
- **Automatisierungsgrad**: Vollautomatische Einstiege und Ausstiege.
- **Bester Einsatz**: Seitwärtsphasen, in denen Bollinger-Extreme häufig zum Median zurückkehren.

## Hinweise

- Der Einzugsversatz wird durch den Kursschritt des Instruments skaliert, um der pip-basierten Logik des Original-EAs zu entsprechen.
- Der Algorithmus hält Warteschlangen der jüngsten Indikatorwerte, um die MT5-Tiefenprüfungen exakt zu replizieren.
- Sicherstellen, dass das Instrument genügend historische Kerzen bereitstellt, um sowohl RSI als auch Bollinger Bands vor dem Live-Trading zu initialisieren.
