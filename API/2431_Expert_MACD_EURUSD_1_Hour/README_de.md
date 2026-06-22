# Expert MACD EURUSD 1 Stunde-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine C#-Übersetzung des MetaTrader 5-Expertenberaters **Expert MACD EURUSD 1 Hour**. Sie handelt auf Stunden-Kerzen mit dem MACD-Indikator mit kurzen, langen und Signalperioden von **5 / 15 / 3**. Die Strategie sucht nach einer starken Momentum-Verschiebung, bei der die MACD-Hauptlinie den Nullpegel nach oben oder unten kreuzt, während die Signallinie die Bewegung bestätigt. Ein Trailing Stop wird verwendet, um offene Positionen zu schützen, und Trades werden geschlossen, wenn die MACD-Steigung gegen die aktuelle Position dreht.

## Parameter

- `FastLength` – schnelle EMA-Periode für MACD (Standard: 5).
- `SlowLength` – langsame EMA-Periode für MACD (Standard: 15).
- `SignalLength` – Signallinen-Periode für MACD (Standard: 3).
- `TrailingPoints` – Trailing-Stop-Abstand in Kurseinheiten (Standard: 25).
- `CandleType` – Zeitrahmen der Kerzen (Standard: 1 Stunde).
- Die `Volume`-Eigenschaft der Strategie steuert die Ordergröße.

## Handelslogik

### Long-Einstieg
1. Signallinien-Werte: `mac8 > mac7 > mac6` und `mac6 < mac5` (Signallinie steigt).
2. Hauptlinien-Werte: `mac4 > mac3 < mac2 < mac1` (Hauptlinie steigt nach einem Rückgang).
3. `mac2 < -0.00020`, `mac4 < 0` und `mac1 > 0.00020` – Hauptlinie kreuzt den Nullpegel nach oben.
4. Wenn alle Bedingungen erfüllt sind und keine Long-Position offen ist, zu Marktpreisen kaufen.

### Short-Einstieg
1. Signallinien-Werte: `mac8 < mac7 < mac6` und `mac6 > mac5` (Signallinie fällt).
2. Hauptlinien-Werte: `mac4 < mac3 > mac2 > mac1` (Hauptlinie fällt nach einem Hoch).
3. `mac2 > 0.00020`, `mac4 > 0` und `mac1 < -0.00035` – Hauptlinie kreuzt den Nullpegel nach unten.
4. Wenn alle Bedingungen erfüllt sind und keine Short-Position offen ist, zu Marktpreisen verkaufen.

### Ausstiegsregeln
- Ein Long schließen, wenn der aktuelle Hauptwert unter dem vorherigen liegt.
- Ein Short schließen, wenn der aktuelle Hauptwert über dem vorherigen liegt.
- Der Trailing Stop aktualisiert sich auf jeder Kerze und steigt aus, wenn der Preis das Stop-Niveau kreuzt.

## Hinweise

Dieses Beispiel demonstriert die Verwendung der High-Level-API von StockSharp mit Indikatorbindung und manueller Trailing-Stop-Verwaltung. Es ist für Bildungszwecke gedacht und beinhaltet kein Geldmanagement über den festen `Volume`-Parameter hinaus.
