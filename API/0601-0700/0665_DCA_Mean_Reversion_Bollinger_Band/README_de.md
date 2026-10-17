# DCA Mean Reversion Bollinger Band-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kauft einen festen Dollarbetrag, wenn der Preis unter das untere Bollinger Band fällt oder am ersten Tag jedes Monats. Alle Positionen werden an einem festgelegten Datum geschlossen.

### Exakte C#/Python-Handelsregeln

Standard: `InvestmentAmount=200` Einheiten der **Notierungswährung**, `OpenDate=DateTime.MinValue`, `CloseDate=DateTime.MaxValue`, `StrategyMode=Combined`, `BollingerPeriod=20`, `BollingerMultiplier=2`, `CandleType=5 Minuten`. Auf **abgeschlossenen Kerzen** ist das untere Bollinger-Band SMA20 minus zwei Standardabweichungen. Bollinger-Kauf nur beim Wechsel von `voriger Close >= voriges unteres Band` zu `aktueller Close < aktuelles unteres Band`. Monthly kauft **einmal pro Kalendermonat**, auf der ersten zulässigen abgeschlossenen Kerze nach Indikatoraufwärmung. Modi: `Bollinger` (nur Kreuzen), `Monthly` (monatlich) und `Combined` (beides, nur eine Order bei gleichzeitigem Signal). Für `OpenDate <= Datum < CloseDate` wird `floor((InvestmentAmount / Close) / VolumeStep) × VolumeStep` gekauft, unter Beachtung der Volumengrenzen. Ausschließlich **Long-Aufbau**, keine Shorts. Ab CloseDate wird auf der ersten verfügbaren Kerze die **gesamte tatsächliche Restposition** geschlossen, ohne neue Käufe. Kein Stop, Short- oder EMA-Kreuzsignal; Slippage kann den tatsächlichen Wert verändern. Zahlen-Defaults fehlten im ursprünglichen README und sind hier explizit definiert.



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
