# EA-Stochastic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

High-Level-StockSharp-Port des MetaTrader-Expertenberaters "EA Stochastic". Die Strategie abonniert eine Kerzenserie, liest
stochastische Oszillatorwerte und hält höchstens eine Nettoposition. Einstiege erfolgen, wenn die stochastische Hauptlinie über eine
konfigurierbare Anzahl von Balken auf der gleichen Seite der konfigurierten Schwellenwerte geblieben ist. Schutzausstiege und ein
Trailing Stop spiegeln die ursprüngliche MQL-Implementierung mit pip-basierten Abständen wider.

## Strategie-Überblick

- **Indikator**: klassischer stochastischer Oszillator (`%K`- und `%D`-Komponenten mit konfigurierbarer Glättung)
- **Richtung**: Long und Short
- **Positionierung**: jeweils nur eine Position (neue Trades werden ignoriert, solange eine Exit-Order aussteht)
- **Ordertyp**: Marktorders mit festem Volumen
- **Daten**: eine einzelne benutzerdefinierte Kerzentype (Standard: 15-Minuten-Kerzen)

## Einstiegslogik

1. Der stochastische Hauptwert wird bei jeder abgeschlossenen Kerze gespeichert.
2. Nachdem mindestens `ComparedBar` Werte zwischengespeichert wurden, wird der aktuelle `kValue` mit dem Wert vor `ComparedBar - 1` Kerzen verglichen.
3. **Long gehen**, wenn beide Werte unter `UpperLevel` liegen. Dies entspricht dem ursprünglichen EA, der nur kauft, wenn der Oszillator für die konfigurierte
   Lookback-Länge unter der oberen Schwelle geblieben ist.
4. **Short gehen**, wenn beide Werte über `LowerLevel` liegen. Der ursprüngliche EA erlaubte Shorts, wenn der Stochastik über der unteren Grenze
   blieb.
5. Neue Einstiege werden übersprungen, wenn eine Position existiert oder wenn bereits ein Schutzausstieg für die aktuelle Position angefordert wurde.

## Ausstieg und Risikomanagement

- **Stop-Loss**: optionaler fester Pip-Abstand vom Einstandspreis. Stops werden gegen Kerzentiefs (für Longs) oder -hochs
  (für Shorts) bewertet.
- **Take-Profit**: optionales festes Pip-Ziel. Hoch/Tief-Prüfungen emulieren das MetaTrader-orderbasierte Take-Profit-Verhalten.
- **Trailing Stop**: wird aktiviert, wenn der offene Trade mehr als `(TrailingStopPips + TrailingStepPips)` Pips gewinnt. Der Stop wird dann
  `TrailingStopPips` hinter dem letzten Extremum platziert, wobei der Trailing-Schritt-Abstand wie beim ursprünglichen EA eingehalten wird.
- **Exit-Orders**: Schließungen werden mit Marktorders ausgeführt (`SellMarket` / `BuyMarket`). Ein Schutz-Flag verhindert wiederholte Exit-Orders,
  bis `OnPositionChanged` den Flat-Zustand bestätigt.

## Parameter

- `StopLossPips` (Standard **50**): Pip-Abstand für den anfänglichen Schutz-Stop. Auf null setzen, um zu deaktivieren.
- `TakeProfitPips` (Standard **150**): Pip-Abstand zur Gewinnmitnahme. Auf null setzen, um zu deaktivieren.
- `TrailingStopPips` (Standard **15**): Trailing-Abstand in Pips. Muss größer als null sein, wenn Trailing aktiviert ist.
- `TrailingStepPips` (Standard **5**): mindestens erforderlicher Pip-Fortschritt, bevor der Trailing Stop aktualisiert wird. Trailing wird abgelehnt, wenn
  dieser Wert null ist.
- `Volume` (Standard **1**): Marktorder-Volumen für Long- und Short-Trades.
- `KPeriod` (Standard **5**): Lookback-Länge für die stochastische %K-Linie.
- `DPeriod` (Standard **3**): Glättungslänge für die %D-Linie.
- `Slowing` (Standard **3**): abschließende Glättung auf die %K-Berechnung.
- `UpperLevel` (Standard **80**): Schwellenwert zur Validierung von Long-Setups.
- `LowerLevel` (Standard **20**): Schwellenwert zur Validierung von Short-Setups.
- `ComparedBar` (Standard **3**): Anzahl der zurückzublickenden Balken bei der Validierung stochastischer Niveaus (Minimum 1).
- `CandleType` (Standard **15-Minuten-Kerzen**): von der Strategie abonnierte Kerzenserie.

## Implementierungshinweise

- Die Pip-Größe wird aus `Security.PriceStep` approximiert. Für Instrumente mit Bruchteil-Pips (typische FX-Paare) werden Schritte kleiner als
  `0.001` automatisch mit 10 multipliziert, was die MetaTrader-`digits_adjust`-Logik reproduziert.
- Die Trailing-Stop-Konfiguration wird beim Start validiert, um den ursprünglichen EA-Fehlerfall zu vermeiden (`TrailingStop > 0` mit null Trailing
  Schritt).
- Der StockSharp-Stochastik-Oszillator verwendet Standard-Glättungs- und Preismodi (Schlusskurs/Hoch/Tief), was den EA-Einstellungen von
  einfachem gleitenden Durchschnitt über Hoch/Tief-Bereichen entspricht.
- Der ursprüngliche EA bot sowohl feste Lot- als auch Risikoprozent-Positionsgröße. Dieser Port behält den festen `Volume`-Parameter bei und kann
  erweitert werden, wenn prozentbasierte Größenbestimmung erforderlich ist.
- Die Chartausgabe rendert die verarbeiteten Kerzen, den Stochastik-Indikator und die ausgeführten Trades für einfacheres Debugging.

## Empfehlungen zur Verwendung

- Funktioniert auf Intraday- oder höheren Zeitrahmen; `CandleType` und stochastische Perioden an das Instrument anpassen.
- `UpperLevel`, `LowerLevel` und `ComparedBar` für verschiedene Marktregimes (Range vs. Trend) anpassen.
- In der Live-Trading-Kombination mit broker-seitigen Risikokontrollen verwenden, da Ausstiege durch Marktorders nach Kerzenbestätigung simuliert werden.
