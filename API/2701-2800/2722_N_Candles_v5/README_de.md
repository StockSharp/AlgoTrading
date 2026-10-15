# N Candles v5-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die N Candles v5-Strategie sucht nach Serien identischer Kerzen und eröffnet einen
Trade in dieselbe Richtung, sobald die erforderliche Serie erscheint. Die ursprüngliche
MQL-Implementierung von Vladimir Karputov wurde in die StockSharp High-Level-API
übertragen. Die Strategie arbeitet ausschließlich mit geschlossenen Kerzen und kann
auf jedem Zeitrahmen ausgeführt werden, wobei Stunden-Kerzen als Standard für die
StockSharp-Version verwendet werden.

## Handelslogik
1. Wenn eine Kerze schließt, klassifiziert die Strategie sie als bullisch (Schluss über
   Eröffnung), bärisch (Schluss unter Eröffnung) oder neutral (Schluss gleich Eröffnung).
2. Aufeinanderfolgende bullische Kerzen erhöhen den Bullish-Streak-Zähler und setzen
   den Bearish-Zähler zurück, und umgekehrt für bärische Kerzen. Neutrale Kerzen
   setzen beide Zähler zurück.
3. Wenn der Bullish-Streak-Zähler den konfigurierten `CandlesCount`-Wert erreicht und
   die aktuelle Nettoposition flat oder short ist, sendet die Strategie einen Market-Buy.
   Die Short-Exposure wird zuerst gedeckt und dann wird das konfigurierte `TradeVolume`
   hinzugefügt, um eine Long-Position aufzubauen.
4. Wenn der Bearish-Streak-Zähler `CandlesCount` erreicht und die Position flat oder
   long ist, verkauft die Strategie zu Marktpreisen, deckt zunächst jede Long-Exposure
   bevor sie Short geht.
5. Trades werden nur innerhalb des optionalen Trading-Session-Fensters geöffnet, das
   durch `StartHour` und `EndHour` definiert wird. Schutzmaßnahmen (Take Profit, Stop
   Loss und Trailing) arbeiten weiterhin außerhalb der Session, um sicherzustellen,
   dass Positionen sicher behandelt werden.
6. Die Strategie verweigert die Erhöhung der Exposure über `MaxNetVolume` hinaus,
   entsprechend der Volumen-Schutzmaßnahme aus der MQL-Version.

## Risikomanagement
- **Take Profit / Stop Loss** – in Pips ausgedrückt und in absolute Preisabstände
  umgerechnet, unter Verwendung des Sicherheits-Preisschritts. Beide Niveaus sind
  optional und können durch Setzen des entsprechenden Werts auf null deaktiviert werden.
- **Trailing Stop** – aktiviert sich, nachdem der Preis `TrailingStopPips` vom
  Einstiegspreis vorgegangen ist. Einmal aktiv, wird der Stop enger gezogen, wenn sich
  der Preis um weitere `TrailingStepPips` in Handelsrichtung bewegt.
- **Session-Filter** – `UseTradingHours` aktiviert den Start- und Endstunden-Filter,
  verhindert neue Einstiege außerhalb des gewählten Fensters, lässt aber das
  Risikomanagement weiterhin Positionen schließen.
- **Maximales Nettovolumen** – die absolute Position (Long oder Short) darf niemals
  `MaxNetVolume` überschreiten.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `TradeVolume` | Ordergröße für neue Einstiege. | `1` |
| `CandlesCount` | Anzahl aufeinanderfolgender identischer Kerzen für ein Signal. | `3` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips (0 deaktiviert). | `50` |
| `StopLossPips` | Stop-Loss-Distanz in Pips (0 deaktiviert). | `50` |
| `TrailingStopPips` | Distanz, die den Trailing Stop aktiviert (0 deaktiviert). | `10` |
| `TrailingStepPips` | Zusätzlicher Fortschritt, bevor der Trailing Stop enger gezogen wird. | `4` |
| `UseTradingHours` | Aktiviert den Trading-Stunden-Filter. | `true` |
| `StartHour` | Erste Stunde (0–23), in der neue Positionen erlaubt sind. | `11` |
| `EndHour` | Letzte Stunde (0–23), in der neue Positionen erlaubt sind. | `18` |
| `MaxNetVolume` | Maximal erlaubte absolute Positionsgröße. | `2` |
| `CandleType` | Zu analysierende Kerzendaten. Standard sind 1-Stunden-Kerzen. | `TimeSpan.FromHours(1)` |

## Verwendungshinweise
- Die Strategie abonniert Kerzendaten über die High-Level-API `SubscribeCandles`
  und funktioniert mit jedem Instrument, das Kerzenserien bereitstellt.
- Da die Logik auf abgeschlossenen Bars basiert, ist sie am besten für Intraday-
  oder höhere Zeitrahmen geeignet, wo das Marktgeräusch zwischen den Schlusskursen
  weniger relevant ist.
- Passen Sie die pip-basierten Risikoeinstellungen entsprechend der Tick-Größe des
  Instruments an.
- Beim Einsatz auf Instrumenten mit erheblichen Spread-Unterschieden überprüfen Sie
  die Trailing-Stop-Parameter, damit der Stop nicht durch normale Spread-Ausweitung
  ausgelöst wird.
