# Trendlinien-nach-Winkel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie ist eine StockSharp-Portierung des MetaTrader Expert Advisors *Trend Line By Angle*. Der ursprüngliche Roboter kombinierte manuelle Einstiege über Schaltflächen mit umfangreichen Money-Management-Werkzeugen. Diese Portierung wandelt den diskretionären Ablauf in ein automatisiertes MACD-Trendfolgesystem um und bewahrt dabei die Schutzlogik:

- Ein monatlicher MACD (12/26/9), berechnet auf dem konfigurierten Signal-Kerzentyp, definiert die Richtung. Bullische Kreuzungen eröffnen Long-Exposure, bärische Kreuzungen eröffnen Short-Exposure.
- Einstiege skalieren bis zur konfigurierten Anzahl von Blöcken und spiegeln damit die wiederholten manuellen Klicks im Quell-EA.
- Bollinger Bands (20, 2) überwachen den Ausführungszeitrahmen. Eine Berührung des oberen Bands liquidiert Long-Exposure; eine Berührung des unteren Bands liquidiert Shorts und repliziert damit die visuellen Stop-Schaltflächen aus MetaTrader.
- Klassische Risikosteuerungen - Stop-Loss, Take-Profit, Trailing Stop und Break-even-Verschiebung - arbeiten mit Pip-Distanzen, die über den `PriceStep` des Instruments umgerechnet werden.
- Kontoschutz schließt alle Orders, wenn entweder ein geldbasiertes oder prozentuales Gewinnziel erreicht ist. Ein zusätzlicher geldbasierter Trailing-Lock verfolgt den schwebenden Gewinn und steigt beim konfigurierten Drawdown aus.

## Ausführungsablauf

1. **Indikatorvorbereitung** - `MovingAverageConvergenceDivergenceSignal` läuft auf `SignalCandleType`, während `BollingerBands` auf dem Handels-`CandleType` laufen.
2. **Einstiegssignale** - Auf jeder abgeschlossenen Ausführungskerze wird die letzte MACD-Kreuzung bewertet. Eine Aufwärtskreuzung löst `BuyMarket` aus, eine Abwärtskreuzung löst `SellMarket` aus. Bestehende Gegenexposure wird vor der Umkehr geschlossen.
3. **Skalierungslogik** - Die Strategie kauft/verkauft weiter, bis die aggregierte Position `TradeVolume * MaxEntries` erreicht.
4. **Risikomanagement** - Break-even-, Trailing-Stop-, Stop-Loss- und Take-Profit-Niveaus werden auf jeder Kerze neu berechnet. Eine Bollinger-Berührung erzwingt einen Ausstieg, selbst wenn andere Niveaus nicht getroffen wurden.
5. **Kontoschutz** - Geld- und prozentuale Take-Profit-Prüfungen laufen, bevor neue Signale erzeugt werden. Das Money-Trailing-Modul verfolgt den höchsten Gesamt-PnL und schließt alles, sobald der Rückgang `MoneyTrailStop` überschreitet.

## Details zum Money Management

- **Gesamt-PnL** ist die Summe aus realisiertem Gewinn (`PnL`) und dem schwebenden PnL, der aus Kerzenschlusskurs, Preisschritt und Schrittwert berechnet wird.
- **Break-even** verschiebt den Schutz-Stop auf `Entry + BreakEvenOffsetPips` (Long) oder `Entry - BreakEvenOffsetPips` (Short), sobald die Bewegung `BreakEvenTriggerPips` überschreitet.
- **Trailing Stop** rückt näher an den Preis, sobald der Gewinn `TrailingStopPips` überschreitet. Long-Trailing-Niveaus steigen nur; Short-Trailing-Niveaus fallen nur.
- **Money-Trail** aktiviert sich, nachdem ein Gewinn von `MoneyTrailTrigger` gesehen wurde. Ab diesem Zeitpunkt wird der höchste Gewinn gespeichert; ein Verlust von mehr als `MoneyTrailStop` von diesem Hoch schließt alle Positionen.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `TradeVolume` | Volumen jedes Einstiegsblocks. |
| `MaxEntries` | Maximale Anzahl von Volumenblöcken, die angesammelt werden können. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. |
| `TrailingStopPips` | Trailing-Distanz in Pips. |
| `UseBreakEven` | Aktiviert die Verschiebung des Stops auf Break-even. |
| `BreakEvenTriggerPips` | Gewinn, der vor der Break-even-Aktivierung erforderlich ist. |
| `BreakEvenOffsetPips` | Zusätzliche Pips, die beim Verschieben auf Break-even hinzugefügt werden. |
| `UseBollingerExit` | Aktiviert Ausstiege bei Berührungen des Bollinger-Bands. |
| `BollingerPeriod` / `BollingerDeviation` | Einstellungen der Bollinger Bands. |
| `UseProfitMoneyTarget` / `ProfitMoneyTarget` | Schalter und Wert für das absolute Gewinnziel. |
| `UseProfitPercentTarget` / `ProfitPercentTarget` | Schalter und Wert für das prozentuale Gewinnziel. |
| `EnableMoneyTrail` | Aktiviert den geldbasierten Trailing Stop. |
| `MoneyTrailTrigger` | Gewinn, der erforderlich ist, bevor Money-Trail aktiv wird. |
| `MoneyTrailStop` | Zulässiger Drawdown vom Hoch vor dem Ausstieg. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | MACD-Konfiguration. |
| `CandleType` | Ausführungszeitrahmen. |
| `SignalCandleType` | Für das MACD-Signal verwendeter Zeitrahmen. |

## Nutzungshinweise

- Die Strategie ist auf korrekte `PriceStep`- und `StepPrice`-Werte des Instruments angewiesen. Konfigurieren Sie das Instrument vor dem Start.
- Wenn das Konto keinen Portfoliowert meldet (`Portfolio.CurrentValue` oder `Portfolio.BeginValue`), wird der prozentuale Take-Profit automatisch ignoriert.
- Alle Kommentare in der C#-Datei dokumentieren die Handelslogik auf Englisch, um die spätere Wartung zu vereinfachen.
