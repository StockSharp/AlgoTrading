# Personal Assistant MNS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Expertenberater `personal_assistant_codeBase_MNS` auf StockSharp. Sie fungiert als manueller Trading-Helfer: Anstatt autonome Signale zu generieren, stellt sie C#-Methoden bereit, die die tastaturgesteuerten Aktionen des ursprünglichen EA replizieren (Trades öffnen/schließen, Volumen anpassen oder profitable Positionen liquidieren). Der Helfer protokolliert auch informative Metriken über das Symbol, aktive Orders und aktuell konfigurierte Risikoniveaus auf jeder abgeschlossenen Kerze.

## Funktionsweise

1. Die Strategie abonniert eine konfigurierbare Kerzen-Serie (`CandleType`, standardmäßig 1 Minute).
2. Jede abgeschlossene Kerze löst ein Update aus, das druckt: aktuelle Position, PnL, Anzahl aktiver Stop/Take-Orders, Spread, Tick-Wert und die konfigurierte Magic-Nummer.
3. Manuelle Befehle (z. B. `PressBuy()` oder `PressSell()`) senden Marktorders mit dem aktuellen Helfer-Volumen. Optionale Stop-Loss- und Take-Profit-Niveaus werden aus Pip-Abständen übersetzt und intern in der Strategie gespeichert.
4. Schutzniveaus werden auf Kerzen-Daten emuliert: Wenn der Preis den gespeicherten Stop oder das Ziel berührt, gibt die Strategie Marktausstiege aus.
5. Eine optionale Regel zum Break-Even-Verschieben (`UseTrailingStop`) wird scharf geschaltet nachdem der Preis `BreakEvenTriggerPips` vorgerückt ist; einmal scharf geschaltet, liquidiert sie die Position wenn der Preis auf den Einstiegspreis plus `BreakEvenOffsetPips` zurückfällt.

## Funktionen

- Repliziert die Schaltflächen 1–8 des MQL-Assistenten via öffentlicher Methoden:
  - `PressBuy()` / `PressSell()` – Marktorders mit optionalen Schutzniveaus öffnen.
  - `PressCloseAll()` – alle Exposure glätten.
  - `IncreaseVolume()` / `DecreaseVolume()` – das Helfer-Volumen in 0,01-Los-Schritten anpassen.
  - `CloseLongPositions()` / `CloseShortPositions()` – nur eine Seite schließen.
  - `CloseProfitablePositions()` – die Position schließen wenn der schwebende PnL positiv ist.
- Protokolliert bei aktiviertem `DisplayLegend` eine detaillierte Aktionslegende beim Start.
- Konvertiert pip-basierte Risikoabstände in absolute Preise anhand des Preisschritts und der Dezimalgenauigkeit des Instruments.
- Unterstützt Break-Even-Trailing für Long- und Short-Positionen, imitierend die ursprüngliche `MOVETOBREAKEVEN()`-Routine.
- Hält unabhängige gespeicherte Stop/Take-Niveaus für Long- und Short-Trades, sodass beim Richtungswechsel obsolete Niveaus automatisch verworfen werden.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `MagicNumber` | Informationeller Identifier aus dem `MagicNo`-Input des MQL. |
| `DisplayLegend` | Aktivieren, um die Steuerungslegende und kerzenbezogene Statusmeldungen zu drucken. |
| `OrderVolume` | Basis-Marktordervolumen (Lots) das von allen manuellen Aktionen wiederverwendet wird. |
| `Slippage` | Maximaler tolerierter Slippage (in Ticks), zur Referenz gespeichert. |
| `TakeProfitPips` | Pip-Abstand für das gespeicherte Take-Profit-Niveau (0 deaktiviert es). |
| `StopLossPips` | Pip-Abstand für das gespeicherte Stop-Loss-Niveau (0 deaktiviert es). |
| `UseTrailingStop` | Break-Even-Trailing-Logik aktivieren oder deaktivieren. |
| `BreakEvenTriggerPips` | Gewinnabstand (in Pips) erforderlich bevor der Break-Even-Stop scharf geschaltet wird. |
| `BreakEvenOffsetPips` | Offset (in Pips) zum Einstiegspreis hinzugefügt wenn der Stop scharf geschaltet ist. |
| `CandleType` | Kerzen-Serie für Überwachung und Niveauemulation. |

## Verwendungstipps

- Hilfsmethoden aus Designer-Aktionen, Skripten oder UI-Steuerelementen aufrufen, um Tastendrücke aus dem ursprünglichen MetaTrader-Panel zu imitieren.
- Schutzniveaus und Break-Even-Abstände setzen voraus, dass das Instrument `PriceStep`, `StepPrice` und `Decimals` bereitstellt. Für exotische Instrumente ohne diese Metadaten die Pip-Abstände manuell anpassen oder die Funktionen durch Setzen auf `0` deaktivieren.
- Da Stop/Take-Niveaus mithilfe von Kerzen-Hochs und -Tiefs reproduziert werden, können sehr schnelle intra-bar Spitzen nicht erfasst werden, wenn der Kerzen-Zeitrahmen nicht klein genug ist. Den Zeitrahmen verkleinern wenn eine feinere Granularität erforderlich ist.
- `CloseProfitablePositions()` repliziert das „Schaltfläche 8"-Verhalten: Es prüft den schwebenden PnL und schließt die gesamte Position nur wenn der Wert strikt positiv ist.

## Unterschiede zur MetaTrader-Version

- Chart-Labels werden durch Log-Einträge ersetzt, da StockSharp nicht dieselben Zeichnungsprimitive innerhalb von Strategien freilegt.
- Stop-Loss- und Take-Profit-Orders werden durch Marktausstiege bei Kerzen-Ereignissen simuliert statt sofortiger ausstehender Orders.
- Das Break-Even-Management wird mit StockSharp-Marktorders implementiert; es modifiziert keine bestehenden Schutzorders.
- Slippage wird als informativer Parameter gehalten; die tatsächliche Ausführung wird vom StockSharp-Connector verwaltet.
