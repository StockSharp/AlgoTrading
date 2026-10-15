# YenTrader051-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die YenTrader051-Strategie repliziert den ursprünglichen MetaTrader Expert Advisor, der die Beziehung zwischen drei Währungspaaren arbitriert:

- **Handeltes Kreuzpaar** – das Instrument, das die Strategieinstanz hostet (z.B. GBPJPY).
- **Hauptpaar** – typischerweise die Basiswährung des Kreuzes gegen USD (z.B. GBPUSD).
- **USDJPY** – wird zur Bestätigung des Yen-Beins des Dreiecks verwendet.

Ein Ausbruch beim Hauptpaar kombiniert mit Bestätigung von USDJPY erzeugt die Handelssignale. Optionale RSI-, CCI-, RVI- und gleitende Durchschnittsfilter verfeinern die Einstiege. Das Positionsmanagement unterstützt sowohl Averaging als auch Pyramiding, während das Risikomanagement die pip/ATR-basierte Stop-Behandlung des EA reproduziert.

## Handelslogik

1. **Ausbruchserkennung**
   - `LoopBackBars` steuert das Lookback-Fenster. Wenn es größer als 1 ist, prüft die Strategie entweder:
     - aktuelle Hochs/Tiefs (`PriceReference = HighLow`), oder
     - Schlusskurse von vor `LoopBackBars` Bars (`PriceReference = Close`).
   - `MajorDirection` definiert, wie sich das Hauptpaar und das Yen-Bein relativ zueinander bewegen sollen, wenn das Kreuz als Haupt/Yen (Left) oder Yen/Haupt (Right) notiert ist.
2. **Einstiegsfilter**
   - `UseRsiFilter` erfordert RSI über/unter 50 je nach erwartetem Trendabgleich.
   - `UseCciFilter` erzwingt positiven/negativen CCI.
   - `UseRviFilter` wartet auf das Kreuzen des RVI mit seiner Signallinie. Die Signallinie ist ein 4-Perioden-SMA der RVI-Werte, genau wie in der MT4-Implementierung.
   - `UseMovingAverageFilter` hält Einstiege mit einem konfigurierbaren gleitenden Durchschnitt ausgerichtet (`MaMode`, `MaPeriod`).
3. **Einsstiegsstil**
   - `EntryMode = Both` erlaubt jeden Ausbruch.
   - `EntryMode = Pyramiding` fügt nur bei bullischen/bärischen Kerzen in Handelsrichtung hinzu.
   - `EntryMode = Averaging` fügt nur hinzu, wenn die vorherige Kerze gegen die Position schloss, um zu mitteln.
4. **Order-Sizing**
   - `FixedLotSize` platziert ein konstantes Volumen.
   - Wenn das feste Los null ist, verwendet die Strategie `BalancePercentLotSize` und den aktuellen Portfoliowert für das Sizing.
   - `MaxOpenPositions` begrenzt die kumulative Größe (Anzahl der additiven Einstiege).
5. **Risikomanagement**
   - Pip-Abstände (`StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips`) werden über `Security.MinPriceStep` übersetzt.
   - Wenn `EnableAtrLevels` aktiv ist, ersetzen ATR-Abstände Pips unter Verwendung des täglichen ATR (`AtrCandleType`, `AtrPeriod`) und der jeweiligen Multiplikatoren.
   - Stops, Take-Profits, Break-Even, Gewinnsperre und Trailing-Levels werden von abgeschlossenen Kerzen aktualisiert, genau wie in der MQL-Implementierung.
   - `CloseOnOpposite` schließt bestehende Positionen, anstatt neue zu stapeln, wenn ein entgegengesetzter Ausbruch erscheint.
   - `AllowHedging` lässt die Strategie zu einer Position hinzufügen, auch wenn noch eine entgegengesetzte Position offen ist. Beachten Sie, dass StockSharp-Strategien Netto-Positionen verwenden, sodass simultane Long/Short-Positionen nicht unterstützt werden; das Flag steuert effektiv, ob die Strategie die Exposition erhöhen darf, wenn die aktuelle Netto-Position in die andere Richtung zeigt.

## Parameter

| Gruppe | Name | Beschreibung |
|--------|------|--------------|
| Instrumente | `MajorSecurity` | Hauptpaar für Ausbruchsbestätigung. |
| | `UsdJpySecurity` | USDJPY-Wertpapier für Yen-Bein-Bestätigung. |
| Daten | `CandleType` | Signal-Zeitrahmen für alle drei Paare. |
| Filter | `MajorDirection` | Ausrichtung zwischen Hauptpaar und gehandeltem Kreuz (Left = Haupt/Yen, Right = Yen/Haupt). |
| | `PriceReference` | Hoch/Tief-Ausbruch oder verzögerter Schlusskursvergleich. |
| | `LoopBackBars` | Anzahl historischer Bars zur Ausbruchsauswertung. |
| | `EntryMode` | Averaging, Pyramiding oder beides. |
| Indikatoren | `UseRsiFilter`, `UseCciFilter`, `UseRviFilter`, `UseMovingAverageFilter` | Zusätzliche Bestätigungsfilter aktivieren/deaktivieren. |
| | `MaPeriod`, `MaMode` | Konfiguration des gleitenden Durchschnitts. |
| Risiko | `FixedLotSize`, `BalancePercentLotSize` | Volumensteuerungen. |
| | `MaxOpenPositions` | Maximale Anzahl additiver Einstiege. |
| | `StopLossPips`, `TakeProfitPips`, `BreakEvenPips`, `ProfitLockPips`, `TrailingStopPips`, `TrailingStepPips` | Pip-basierte Risikoabstände. |
| | `EnableAtrLevels`, `AtrCandleType`, `AtrPeriod`, `AtrStopLossMultiplier`, `AtrTakeProfitMultiplier`, `AtrTrailingMultiplier`, `AtrBreakEvenMultiplier`, `AtrProfitLockMultiplier` | ATR-basierte Risikokonfiguration. |
| Verhalten | `CloseOnOpposite` | Positionen bei entgegengesetzten Signalen schließen oder umkehren. |
| | `AllowHedging` | Einstiege erlauben, wenn eine entgegengesetzte Netto-Position besteht. |

## Verwendungshinweise

- Weisen Sie das gehandelte Kreuzinstrument der `Security`-Eigenschaft der Strategie zu, dann setzen Sie `MajorSecurity` und `UsdJpySecurity` für die Supportinstrumente.
- Stellen Sie sicher, dass das Portfolio verbunden ist; variables Los-Sizing benötigt `Portfolio.CurrentValue`.
- Die Strategie erwartet synchronisierte Kerzendaten für alle drei Instrumente. Wenn verschiedene Börsen Daten mit unterschiedlichen Sitzungskalendern liefern, erwägen Sie ein Resampling auf einen gemeinsamen Zeitrahmen.
- ATR-Berechnungen abonnieren das konfigurierte `AtrCandleType`. Halten Sie es an den ursprünglichen EA-Standardwerten (täglich, 21 Perioden) ausgerichtet für vergleichbares Verhalten.
- Die RisikoLogik operiert auf geschlossenen Kerzen, sodass Schutzorders durch Market-Exits ausgeführt werden, wenn die Schwellenwerte während der folgenden Kerze durchbrochen werden.

## Unterschiede zur MT4-Version

- StockSharp verwendet aggregierte Netto-Positionen; echtes Hedging (gleichzeitig Long und Short halten) ist nicht verfügbar. `AllowHedging` steuert lediglich, ob die Strategie Positionen automatisch umkehren kann, wenn ein neues Signal erscheint.
- Stop-/Limit-Management wird mit Market-Exits implementiert, nachdem die Schwellenwerte auf Kerzendaten ausgelöst wurden. Der ursprüngliche EA modifiziert Order-Stops direkt, weil er auf Tick-Ebene operiert.
- Die RVI-Signallinie wird als ein Vier-Perioden-SMA der RVI-Werte implementiert, was dem Verhalten von `MODE_SIGNAL` in MT4 entspricht.
