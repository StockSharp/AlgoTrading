# Simple MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Simple MACD repliziert die Logik des MQL5-Beraters `Simple_MACD.mq5` in StockSharp. Die Strategie folgt der Steigung der MACD-Hauptlinie, die auf abgeschlossenen Kerzen berechnet wird, und fügt der Position weiterhin hinzu, solange die Steigung in dieselbe Richtung geht.

## Überblick

- **Markt**: Jedes Instrument mit Kerzendate und durchgehenden Handelszeiten.
- **Kernindikator**: Moving Average Convergence Divergence (MACD) mit exponentiellen gleitenden Durchschnitten 12/26 und Signal 9.
- **Ansatz**: Momentum-Following. Die Strategie vergleicht die beiden aktuellsten abgeschlossenen MACD-Lesewerte und geht long, wenn die Linie steigt, oder short, wenn die Linie fällt.
- **Ordertyp**: Nur Marktorders. Jedes Signal aggregiert den Betrag, der zum Schließen der entgegengesetzten Position erforderlich ist, und fügt das konfigurierte Handelsvolumen hinzu, was den Original-Expertenberater widerspiegelt.

## Konvertierungshinweise

- Der MQL5-Bot löste einmal pro neuer Bar aus, indem er `MACD(1)` und `MACD(2)` (die vorherigen zwei abgeschlossenen Bars) verglich. In StockSharp wird derselbe Vergleich ausgeführt, wenn eine Kerze endet, bevor die nächste Bar beginnt.
- Die MQL-Version stützte sich auf explizite Positions-Enumeration und manuelle Volumenprüfungen. Die StockSharp-Version aggregiert das Volumen automatisch mit `BuyMarket`/`SellMarket`-Aufrufen und dem `TradeVolume`-Parameter der Strategie.
- Hedging-Prüfungen aus dem MQL-Code sind nicht erforderlich, da StockSharp die Nettoposition direkt verfolgt.

## Handelsregeln

### Einstieg und Skalierung

1. MACD-Hauptlinie auf jeder fertigen Kerze berechnen.
2. Die letzten zwei MACD-Werte speichern und vergleichen:
   - Wenn `MACD(1) > MACD(2)` ist die Steigung bullisch. Die Strategie kauft ein Volumen gleich `TradeVolume + max(0, -Position)`, um Shorts zu schließen und neue Longs hinzuzufügen.
   - Wenn `MACD(1) < MACD(2)` ist die Steigung bärisch. Die Strategie verkauft `TradeVolume + max(0, Position)`, um Longs zu schließen und neue Shorts hinzuzufügen.
3. Wenn beide Werte gleich sind, werden keine neuen Orders eingereicht.

### Positionsmanagement

- Die Strategie stapelt weiterhin Orders in der aktuellen Richtung, solange das MACD-Gefälle kein Vorzeichen ändert, genau wie der ursprüngliche Berater, der auf jeder qualifizierenden Bar einen Kauf oder Verkauf sendete.
- Entgegengesetzte Signale schließen jede offene Exposure, bevor die neue Position aufgebaut wird.
- Keine Stop-Loss- oder Take-Profit-Niveaus sind eingebettet; die Risikokontrolle basiert auf externen Money-Management-Regeln oder manueller Überwachung.

### Zusätzliche Sicherheitsvorkehrungen

- Das Trading wird übersprungen, bis der MACD-Indikator vollständig gebildet ist.
- Nur abgeschlossene Kerzen (`CandleStates.Finished`) werden verarbeitet, um vorzeitige Aktionen bei unvollständigen Daten zu verhindern.
- Log-Nachrichten verfolgen jeden Trade und zeigen die beiden MACD-Werte, die für die Entscheidung verwendet wurden, für einfachere Backtesting-Analyse.

## Parameter

| Parameter | Standardwert | Beschreibung |
|-----------|--------------|--------------|
| `FastPeriod` | 12 | Schnelle EMA-Länge für die MACD-Berechnung. |
| `SlowPeriod` | 26 | Langsame EMA-Länge für die MACD-Berechnung. |
| `SignalPeriod` | 9 | Signal-EMA-Periode, die aus Kompatibilitätsgründen mit den ursprünglichen Einstellungen beibehalten wird. |
| `TradeVolume` | 0.1 | Volumen, das bei jedem Signal vor Berücksichtigung der Positionsumkehr hinzugefügt wird. |
| `CandleType` | 1-Minuten-Zeitrahmen | Kerzentyp zur Speisung des Indikators. Auf jeden gewünschten Zeitrahmen anpassbar. |

Alle Parameter sind als Strategieparameter exponiert und wo sinnvoll als optimierbar markiert.

## Visualisierung

- Die Strategie erstellt automatisch einen Chartbereich (wenn verfügbar) mit den Preiskerzen und überlagert den MACD-Indikatorausgang.
- Eigene Trades werden auf dem Chart gezeichnet, um zu zeigen, wie häufig die Strategie Positionen in Trendbedingungen skaliert.

## Empfohlene Verwendung

- Auf Trendinstrumenten anwenden, bei denen Momentum mehrere Bars anhält; seitwärts laufende Märkte werden häufige Umkehrungen und Whipsaw-Trades verursachen.
- Mit Portfolio-Level-Risikomanagement kombinieren, da die Basislogik keinen intrinsischen Stop-Mechanismus hat.
- In Betracht ziehen, `TradeVolume` und MACD-Perioden für das Zielinstrument und den Zeitrahmen zu optimieren.

## Dateien

- `CS/SimpleMacdStrategy.cs` – StockSharp-Implementierung der Strategielogik.
- `README.md`, `README_ru.md`, `README_zh.md` – detaillierte Dokumentation in drei Sprachen.
