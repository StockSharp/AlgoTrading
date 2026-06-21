# DCA Mean Reversion Bollinger Band-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kauft einen festen Dollarbetrag, wenn der Preis unter das untere Bollinger Band fällt oder am ersten Tag jedes Monats. Alle Positionen werden an einem festgelegten Datum geschlossen.

## Parameter
- `InvestmentAmount` - investierter Betrag je Kauf
- `OpenDate` - Startdatum für Käufe
- `CloseDate` - Datum zum Schließen aller Positionen
- `StrategyMode` - BB Mean Reversion, monatliches DCA oder kombiniert
- `BollingerPeriod` - Bollinger Bands-Periode
- `BollingerMultiplier` - Standardabweichungsmultiplikator
- `CandleType` - Zeitrahmen für die Bollinger-Berechnung

## Indikatoren
- Bollinger Bands
