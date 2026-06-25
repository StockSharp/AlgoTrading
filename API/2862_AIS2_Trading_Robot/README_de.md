# AIS2 Trading-Roboter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der AIS2 Trading-Roboter ist ein Multi-Timeframe-Ausbruchssystem, das aus dem ursprünglichen MetaTrader 5-Expertenberater konvertiert wurde. Er scannt einen höheren Zeitrahmen (Standard 15-Minuten-Kerzen) zur Erkennung direktionaler Ausbrüche, während ein schnellerer Zeitrahmen (Standard 1-Minuten-Kerzen) adaptive Trailing-Stops bereitstellt. Auftragsplatzierung, Risikobudgetierung und Trailing-Logik folgen den im Legacy-MQ5-Version codierten Regeln, sind aber auf der High-Level-Strategie-API von StockSharp implementiert.

## Handelslogik
- **Primäre Signalkerze**: Für jede abgeschlossene Kerze auf dem primären Zeitrahmen erfasst die Strategie Hoch, Tief, Schluss, Mittelpunkt und Bereich.
- **Long-Setup**:
  - Der vorherige Schluss muss über dem Kerzenmittelpunkt liegen und bullischen Druck signalisieren.
  - Der aktuelle Ask-Preis muss über dem vorherigen Hoch plus dem gemessenen Spread handeln (Ausbruchsbestätigung).
  - Einstiegspreis ist der aktuelle Ask. Stop-Loss gleich `high + spread - (range × StopFactor)`. Take-Profit gleich `ask + (range × TakeFactor)`.
  - Zusätzliche Broker-Sicherheitsprüfungen stellen sicher, dass sowohl Risiko als auch Belohnung größer als der konfigurierte Stop-Puffer-Abstand sind.
- **Short-Setup**:
  - Der vorherige Schluss muss unter dem Mittelpunkt liegen und bärischen Druck signalisieren.
  - Der aktuelle Bid muss unter dem vorherigen Tief drucken (Abwärtsausbruch).
  - Einstiegspreis ist der aktuelle Bid. Stop-Loss gleich `low + (range × StopFactor)`. Take-Profit gleich `bid - (range × TakeFactor)`.
- **Konfliktlösung**: Neue Trades werden nur genommen, wenn die Strategie flat ist oder in der entgegengesetzten Richtung positioniert ist (das Einstiegsvolumen gleicht automatisch die bestehende Exposition aus, bevor die neue Position eröffnet wird).

## Auftragsmanagement
- **Trailing-Stop**: Der sekundäre Zeitrahmen-Bereich wird mit `TrailFactor` multipliziert, um einen dynamischen Trail aufzubauen. Bei Long-Positionen wird der Stop auf `bid - trailDistance` gezogen; bei Shorts wird er auf `ask + trailDistance` geschoben. Trailing-Updates werden übersprungen, wenn der Preis nicht im Profit ist oder wenn die angeforderte Änderung kleiner als der konfigurierte Trail-Schritt und die Einfrierungspuffer ist.
- **Gewinnmitnahme und Stop-Ausstieg**: Sowohl Long- als auch Short-Positionen werden mit Marktaufträgen liquidiert, wenn Bid/Ask-Preise die gespeicherten Stop-Loss- oder Take-Profit-Niveaus überkreuzen.
- **Order-Buch-Feed**: Ein Live-Orderbuch-Abonnement verfolgt die aktuellen besten Bid/Ask-Preise, damit die Strategie die MQ5-Logik reproduzieren kann, die auf `SymbolInfo.Ask/Bid`-Werten basierte.

## Positionsgröße und Risikokontrollen
- **Kontoreserve**: Ein konfigurierbarer Anteil des Portfolio-Eigenkapitals ist gesperrt und kann nicht für den Handel verwendet werden. Dies repliziert den `Inp_aed_AccountReserve`-Parameter des ursprünglichen EA.
- **Auftragsreserve**: Das verbleibende Kapital wird weiter durch eine Auftragszuteilungsfraktion begrenzt, die das maximale Risikobudget pro Trade begrenzt.
- **Risikoprüfungen**:
  - Wenn das reservierte Eigenkapital kleiner als das Zuteilungslimit (`Equity × OrderReserve`) ist, lehnt die Strategie neue Trades ab.
  - Positionsgröße wird als `riskBudget / |entry - stop|` berechnet, ausgerichtet am Sicherheitsvolumenschritt. Wenn keine Portfolio-Informationen verfügbar sind, wird der Fallback-Parameter `BaseVolume` verwendet.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `AccountReserve` | Anteil des Eigenkapitals, der vom Handel zurückgehalten wird (0–0.95).
| `OrderReserve` | Anteil des handelbaren Eigenkapitals, der das Risikobudget pro Trade definiert (0–1).
| `PrimaryCandleType` | Arbeitszeitrahmen für die Ausbruchserkennung (Standard 15 Minuten).
| `SecondaryCandleType` | Schnellerer Zeitrahmen, der Trailing-Stop-Updates antreibt (Standard 1 Minute).
| `TakeFactor` | Multiplikator des primären Bereichs für die Take-Profit-Distanz.
| `StopFactor` | Multiplikator des primären Bereichs für die Stop-Loss-Distanz.
| `TrailFactor` | Multiplikator des sekundären Bereichs für die Trailing-Distanz.
| `BaseVolume` | Fallback-Auftragsgröße, wenn Portfolio-Metriken nicht verfügbar sind.
| `StopBufferTicks` | Zusätzlicher Abstand (in Ticks) über Exchange-Stop-Beschränkungen hinaus.
| `FreezeBufferTicks` | Zusätzlicher Puffer, der geringfügige Trailing-Anpassungen nahe dem Einfrierungsniveau verhindert.
| `TrailStepMultiplier` | Spread-Multiplikator, der das minimale Inkrement zwischen Trailing-Updates definiert.

## Hinweise
- Füttern Sie die Strategie immer mit beiden primären und sekundären Kerzenserien plus einem Live-Orderbuch-Stream, um alle Logikzweige freizuschalten.
- Die Ausbruchsprüfungen basieren auf Bid/Ask-Preisen, daher kann Paper-Trading mit ausschließlich letzten Handelspreisen unterschiedliches Verhalten im Vergleich zu einer realen Umgebung liefern.
- Der Positionsschutz wird automatisch gestartet, sobald die Strategie läuft, und spiegelt die Sicherheitsroutinen der MQ5-Version wider.
