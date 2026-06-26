# Demarker Martingale-Strategie (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Demarker Martingale-Strategie** re­kreiert den MetaTrader Expert Advisor „Demarker Martingale" mithilfe der High-Level-API von StockSharp. Das System kombiniert ein mittelfristiges DeMarker-Oszillatorsignal mit einem höheren Zeitrahmen-MACD-Trendfilter. Einträge werden durch Martingale-ähnliche Positionsgrößen, feste Stop-Loss- und Take-Profit-Niveaus, Break-Even-Schutz und einen Trailing Stop gefolgt, der das Geldverwaltungs-Toolkit des Original-Experten imitiert.

## Kern-Handelslogik
1. **Datenfeeds** – die Strategie abonniert einen benutzerdefinierten Handelszeitrahmen (standardmäßig 15-Minuten-Kerzen) für die Signalerzeugung und eine höheren Zeitrahmen-Serie (standardmäßig monatliche Kerzen) zur Berechnung des MACD-Filters.
2. **DeMarker-Auslöser** – wenn der DeMarker-Wert den neutralen `DemarkerThreshold` (Standard 0.5) überschreitet und die jüngste Preisaktion eine bullische Überlappung bildet (`Low[2] < High[1]`), wird ein Long-Setup in Betracht gezogen. Umgekehrt bereitet eine bärische Überlappung mit DeMarker unter dem Schwellenwert ein Short vor.
3. **MACD-Bestätigung** – der höhere Zeitrahmen-MACD muss mit der Richtung übereinstimmen. Ein bullisches Signal erfordert, dass die MACD-Hauptlinie über ihrer Signallinie liegt, während ein bärisches Signal die entgegengesetzte Beziehung erwartet. Dies reproduziert den monatlichen MACD-Filter des MQL-Experten.
4. **Orderausführung** – gültige Signale platzieren Market Orders mit dem aktuellen Martingale-angepassten Volumen. Es wird jeweils nur eine gerichtete Position gehalten.
5. **Positions-Monitoring** – während eine Position offen ist, bewertet die Strategie jede abgeschlossene Kerze auf Stop-Loss-, Take-Profit-, Break-Even- oder Trailing-Stop-Auslöser. Verletzungsereignisse schließen die gesamte Position über Market Orders.

## Geldverwaltung
- **Anfangsgröße** – Orders beginnen mit `InitialVolume`, ausgerichtet am `VolumeStep` des Instruments und begrenzt durch `VolumeMin`/`VolumeMax`.
- **Martingale-Eskalation** – nach einem verlorenen Trade wird das nächste Volumen entweder mit `MartingaleMultiplier` multipliziert (`DoubleLotSize = true`) oder um `LotIncrement` erhöht. Profitable Trades setzen die Leiter auf das Basisvolumen zurück. Die Eskalationstiefe ist durch `MaxMartingaleSteps` begrenzt, um unkontrollierte Exposition zu verhindern.
- **Stop-Loss und Take-Profit** – Abstände werden in MetaTrader-Pips ausgedrückt. Die Pip-Größe passt sich automatisch an 3/5-stellige Forex-Kurse an und entspricht der originalen `ticksize`-Logik.
- **Break-Even** – sobald der unrealisierte Gewinn `BreakEvenTriggerPips` erreicht, wird der Stop-Loss auf den Einstieg plus `BreakEvenOffsetPips` (Long) oder minus den Offset (Short) verschoben.
- **Trailing Stop** – Gewinne jenseits von `TrailingStopPips` verschieben eine interne Trailing-Schwelle, die sich mit jeder Kerze strafft und das `TrailingStop`-Verhalten des EA repliziert.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Handelszeitrahmen für DeMarker-Signale. |
| `MacdCandleType` | Höherer Zeitrahmen zur Berechnung des MACD-Trendfilters. |
| `DemarkerPeriod` | DeMarker-Lookback-Periode. |
| `DemarkerThreshold` | Neutrale Grenze zwischen bullischen und bärischen Setups. |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD-EMA-Längen. |
| `InitialVolume` | Basis-Ordergröße vor Martingale-Anpassungen. |
| `MartingaleMultiplier` | Multiplikationsfaktor bei aktiviertem `DoubleLotSize`. |
| `LotIncrement` | Additiver Anstieg bei deaktiviertem Verdoppeln. |
| `DoubleLotSize` | Umschalten zwischen multiplikativem und additivem Martingale. |
| `MaxMartingaleSteps` | Maximale Anzahl aufeinanderfolgender Eskalationen. |
| `StopLossPips` | Stop-Loss-Abstand in Pips. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. |
| `UseBreakEven` | Break-Even-Logik aktivieren oder deaktivieren. |
| `BreakEvenTriggerPips` | Gewinnschwelle (in Pips) vor dem Wechsel zu Break-Even. |
| `BreakEvenOffsetPips` | Puffer, der auf den Break-Even-Stop angewendet wird. |

## Konvertierungshinweise
- Die Pip-Konvertierung spiegelt den MQL-EA wider (`ticksize == 0.00001` oder `0.001` impliziert eine 10x-Pip-Skala). Dies bewahrt konsistente Risikoabstände bei 3/5-stelligen Kursen.
- Der MACD-Trendfilter verwendet `MovingAverageConvergenceDivergenceSignal` mit den originalen EMA-Längen und verarbeitet eine separate Kerzenserie, um die monatliche Diagrammlogik zu emulieren.
- Die Martingale-Buchführung verfolgt gewichtete Durchschnittseinstiegspreise und realisierten PnL, um zu entscheiden, ob der nächste Trade eskalieren oder zurücksetzen soll.
- Alle Schutzmaßnahmen (Stop-Loss, Take-Profit, Break-Even, Trailing) werden über Market Exits ausgeführt, da die High-Level-API direkte Ordermodifikationen unter der `StartProtection`-Wache nicht empfiehlt.

## Verwendungshinweise
- Stellen Sie sicher, dass das zugewiesene Instrument `PriceStep`, `VolumeStep`, `VolumeMin` und `VolumeMax` bereitstellt, um Pip-Berechnungen und Volumenrundung mit Exchange-Einschränkungen abzustimmen.
- Experimentieren Sie mit `MacdCandleType` (z.B. wöchentliche Kerzen), um den Trendfilter für schnellere Märkte feinabzustimmen.
- Passen Sie beim Optimieren gemeinsam `DemarkerThreshold`, `TrailingStopPips` und Martingale-Parameter an, um Drawdowns im Zaum zu halten.
- Kombinieren Sie die Strategie mit Portfolio-Level-Risikokontrollen oder Handelssitzungsfiltern beim Live-Einsatz, da Martingale-Sequenzen nach Verlusten inhärent die Exposition erhöhen.
