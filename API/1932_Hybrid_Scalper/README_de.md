# Hybrid-Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **Hybrid-Scalper-Strategie** ist ein kurzfristiger Handelsalgorithmus, der aus dem MQL4-Skript `hybrid_Scalper.mq4` konvertiert wurde. Er läuft auf der High-Level-API von StockSharp und ist für den 1-Minuten-Zeitrahmen ausgelegt. Die Strategie kombiniert mehrere technische Indikatoren, um schnelle Ausbruchsmöglichkeiten zu identifizieren und dabei Phasen übermäßiger oder unzureichender Volatilität zu vermeiden.

## Strategielogik

1. **Trendfilter** – Eine schnelle EMA (21) und eine langsame EMA (89) bestimmen die Marktrichtung. Long-Trades sind nur erlaubt, wenn die schnelle EMA über der langsamen EMA liegt; Short-Trades erfordern die umgekehrte Bedingung.
2. **Momentum-Filter** – Der Stochastik-Oszillator (5,3,3) generiert Einstiegssignale. Ein Kaufsignal wird ausgelöst, wenn %K unter 20 und unter %D liegt. Ein Verkaufssignal wird ausgelöst, wenn %K über 80 liegt und weiterhin über %D bleibt.
3. **RSI-Bestätigung** – Der Relative Strength Index mit Periode 7 bestätigt das Momentum. Long-Einstiege erfordern RSI unter 25, Short-Einstiege erfordern RSI über 85.
4. **Volatilitätsfilter** – Bollinger Bänder (50, Abweichung 4) messen die aktuelle Marktbreite. Die Strategie handelt nur, wenn die Differenz zwischen dem oberen und unteren Band zwischen 0.00045 und 0.00262 liegt, und vermeidet damit sowohl ruhige als auch instabile Märkte.
5. **Handelstage** – Parameter ermöglichen die individuelle Aktivierung oder Deaktivierung des Handels für jeden Wochentag (Montag–Freitag).

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `RsiPeriod` | Periode des RSI-Indikators. |
| `EmaFastPeriod` | Schnelle EMA-Periode zur Trenderkennung. |
| `EmaSlowPeriod` | Langsame EMA-Periode zur Trenderkennung. |
| `BbPeriod` | Periode für Bollinger Bänder. |
| `BbDeviation` | Abweichungsmultiplikator für Bollinger Bänder. |
| `TradeMonday`–`TradeFriday` | Handel an bestimmten Wochentagen aktivieren. |
| `CandleType` | Kerzentyp/Zeitrahmen, Standard sind 1-Minuten-Kerzen. |

## Hinweise

- Die Strategie nutzt die High-Level-API `BindEx`, um mehrere Indikatoren in einem einzigen Abonnement zu verbinden.
- `StartProtection()` wird einmalig beim Start aufgerufen, um den integrierten Positionsschutz zu aktivieren (keine expliziten Stop-Loss- oder Take-Profit-Parameter).
- Alle Kommentare im Code werden gemäß den Repository-Richtlinien auf Englisch bereitgestellt.

## Ausführung

1. Fügen Sie die Strategiedatei einem StockSharp-Projekt hinzu.
2. Konfigurieren Sie die erforderlichen Marktdaten- und Ausführungsverbindungen.
3. Kompilieren Sie die Strategie und starten Sie sie; stellen Sie sicher, dass das ausgewählte Instrument 1-Minuten-Kerzen bereitstellt.
4. Passen Sie die Parameter bei Bedarf über die `StrategyParam`-Schnittstelle an.
