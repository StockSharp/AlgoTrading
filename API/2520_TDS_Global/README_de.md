# TDS Global Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den originalen MetaTrader "TDSGlobal"-Experten basierend auf Alexander Elders Triple Screen-Konzept. Sie wertet Tageskerzen aus und kombiniert die Steigung des MACD (12, 23, 9) mit einem 24-Perioden Williams %R-Filter. Das System versucht zu kaufen, wenn der Trend nach oben dreht und %R überverkaufte Bedingungen zeigt, und zu verkaufen, wenn der Trend nach unten dreht und %R Überkauf signalisiert.

Immer wenn ein gültiges Setup erkannt wird, platziert die Strategie Stop-Orders jenseits des Hochs oder Tiefs der vorherigen Session. Einstiege werden durch einen konfigurierbaren Puffer vom aktuellen Markt weggeschoben, um ein Einsteigen zu nah am Preis zu vermeiden, was die ursprüngliche "16-Punkte"-Offset-Logik widerspiegelt. Einmal in einer Position verwaltet die Strategie einen Schutzstop, optionalen Take-Profit und einen Trailing Stop in Preisschritten.

## Handelslogik

- **Daten**: Arbeitet standardmäßig mit Tageskerzen (konfigurierbar).
- **Trendfilter**: Vergleicht die zwei aktuellsten MACD-Hauptlinienwerte. Steigender MACD impliziert Long-Bias, fallender MACD impliziert Short-Bias.
- **Oszillatorfilter**: Verwendet den vorherigen Williams %R-Wert. Unter `WilliamsBuyLevel` (Standard -75) erlaubt Long-Setups, über `WilliamsSellLevel` (Standard -25) erlaubt Short-Setups.
- **Einstieg**:
  - Long: Buy-Stop über dem vorherigen Hoch plus einen Preisschritt platzieren. Der Einstieg wird auf mindestens `EntryBufferSteps` Preisschritte über dem letzten Schlusskurs angehoben, um eine Mindestdistanz vom Markt zu wahren.
  - Short: Sell-Stop unter dem vorherigen Tief minus einen Preisschritt platzieren. Die Order wird auf höchstens den letzten Schlusskurs minus `EntryBufferSteps` Schritte gesenkt.
- **Risikomanagement**:
  - Der anfängliche Stop ist am entgegengesetzten Extrem der vorherigen Kerze verankert (Hoch für Shorts, Tief für Longs).
  - Die Take-Profit-Distanz entspricht `TakeProfitSteps` Preisschritten. Der Standardwert (999) hält das Verhalten nahe an der MQL-Version, die ein sehr weites Ziel verwendete.
  - Der Trailing Stop wird aktiviert, wenn `TrailingStopSteps` > 0 ist. Er folgt dem Schlusskurs um diese Anzahl von Schritten und zieht sich nur in Richtung des Trades an.
- **Order-Handling**:
  - Bestehende Stop-Orders werden storniert und erneuert, wenn der Einstiegspreis oder die Schutzniveaus aktualisiert werden müssen.
  - Entgegengesetzte Trendsignale entfernen ausstehende Orders, die nicht mehr mit der MACD-Richtung übereinstimmen.
  - Wenn eine Position eröffnet wird, werden die gespeicherten ausstehenden Niveaus wiederverwendet, um die Live-Stop/Take-Preise zu initialisieren.
- **Optionale Staffelung**: Der ursprüngliche EA staffelte die Order-Platzierung über FX-Paare hinweg, um simultane ausstehende Orders zu vermeiden. Das Setzen von `UseSymbolStagger` auf `true` erzwingt dieselben Minute-Fenster für EURUSD, GBPUSD, USDCHF und USDJPY.

## Parameter

- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – MACD-Perioden für die Trendsteigungsprüfung.
- `WilliamsLength` – Lookback für Williams %R.
- `WilliamsBuyLevel`, `WilliamsSellLevel` – Überverkauft/Überkauft-Schwellenwerte (negative Werte, näher an -100/-0 entsprechend).
- `EntryBufferSteps` – Mindestabstand vom aktuellen Markt beim Platzieren von Stop-Einstiegen (Anzahl der Preisschritte).
- `TakeProfitSteps` – Zieldistanz in Preisschritten (kleinen Wert setzen, um ein hartes Ziel zu aktivieren).
- `TrailingStopSteps` – Trailing-Stop-Distanz in Schritten; auf null setzen zum Deaktivieren des Trailings.
- `UseSymbolStagger` – Aktiviert die symbolspezifischen Minute-Fenster.
- `CandleType` – Zeitrahmen für Kerzen (standardmäßig täglich).

## Hinweise

- Das Strategie-Volumen verwenden, um die Lot-Größe zu steuern; standardmäßig 1, wenn kein Volumen angegeben.
- Ausstehende Orders und Trailing-Ausstiege operieren auf abgeschlossenen Kerzen, daher werden Füllungen zwischen Kerzenabschlüssen durch den gespeicherten Einstiegspreis angenähert.
- Der Standard-Take-Profit-Wert ist groß, um dem ursprünglichen EA-Verhalten zu entsprechen; anpassen, wenn ein endliches Ziel benötigt wird.
