# Strategie TakeProfitTimeGuardStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

`TakeProfitTimeGuardStrategy` emuliert das Verhalten des MetaTrader-Experten `Exp_GTakeProfit_Tm`, indem es den Gewinn auf Kontoebene überwacht und außerhalb eines konfigurierbaren Handelszeitplans eine flache Positionierung erzwingt. Die Strategie öffnet selbst keine Positionen. Stattdessen dient sie als Risikomanagement-Überlagerung, die automatisch alle bestehenden Positionen schließt, sobald das Gewinnziel erreicht wird oder wenn der Handel außerhalb des erlaubten Zeitfensters gestoppt werden muss.

## Kernlogik

- Abonniert einen konfigurierbaren Kerzen-Stream (Standard 1 Minute), um realisierten und unrealisierten PnL anhand des neuesten Schlusskurses zu bewerten.
- Berechnet den **Gesamtgewinn** als Summe aus realisiertem PnL (`Strategy.PnL`) und dem schwebenden PnL, der aus dem aktuellen durchschnittlichen Positionspreis abgeleitet wird.
- Ignoriert Verluste, während das Handelsfenster offen ist, entsprechend dem Originalverhalten des Expert Advisors.
- Sobald das **Take-Profit-Ziel** erreicht ist, setzt es ein internes Stop-Flag und liquidiert wiederholt alle verbleibenden Positionen, bis das Konto flach ist. Das Stop-Flag wird zurückgesetzt, nachdem das Portfolio zur Null-Position zurückgekehrt ist.
- Wenn das optionale **Handelsfenster** aktiviert ist, schließt die Strategie alle Positionen, wenn die aktuelle Zeit außerhalb des erlaubten Bereichs liegt, und wartet ebenfalls, bis das Buch flach ist, bevor der Handel wieder aktiviert wird.

## Parameter

| Parameter | Typ | Standard | Beschreibung |
|-----------|-----|----------|--------------|
| `CandleType` | `DataType` | 1-Minuten-Zeitrahmen | Kerzenserie zur Bewertung der Gewinn- und Zeitplanlogik. |
| `TargetMode` | `ProfitTargetModes` (`Percent`/`Currency`) | `Percent` | Legt fest, ob `TakeProfitValue` als Prozentsatz des Kontokapitals oder als absoluter Währungsbetrag interpretiert wird. |
| `TakeProfitValue` | `decimal` | `100` | Gewinnzielschwellenwert. Wird gemäß `TargetMode` interpretiert. Muss größer als null sein. |
| `UseTradingWindow` | `bool` | `true` | Aktiviert oder deaktiviert den Zeitfilter. |
| `StartTime` | `TimeSpan` | `00:00:00` | Beginn des erlaubten Handelsfensters (inklusiv). |
| `EndTime` | `TimeSpan` | `23:59:00` | Ende des erlaubten Handelsfensters. Wenn die Startzeit größer als die Endzeit ist, erstreckt sich das Fenster über Mitternacht. |

## Verhaltenshinweise

1. Der anfängliche Portfoliowert wird beim Start der Strategie erfasst (oder bei der ersten Aktualisierung, wenn der Wert null war) und als Referenz für das Prozentziel verwendet.
2. Die Strategie berechnet den schwebenden PnL anhand des neuesten Kerzenschlusskurses; die Ergebnisse hängen von der ausgewählten Kerzen-Granularität ab.
3. Wenn das Gewinnziel erreicht ist, sendet die Strategie weiterhin Marktorders, um die Position zu glätten, bis das Buch leer ist. Sie protokolliert den Grund für die Schließung des Buches.
4. Wenn `UseTradingWindow` aktiviert ist und die Uhr außerhalb des Fensters liegt, wird dieselbe Glättungsroutine ausgeführt, auch wenn das Gewinnziel nicht erreicht wurde.
5. Das Stop-Flag (`_stop`) wird erst gelöscht, nachdem die Position auf null zurückgekehrt ist, sodass der Handel wieder aufgenommen werden kann, wenn die Bedingungen es erlauben.

## Unterschiede zur originalen MQL-Strategie

- Verwendet die StockSharp High-Level-API (`SubscribeCandles`) statt Tick-Handler.
- Berechnet schwebenden Gewinn aus dem durchschnittlichen Positionspreis, der durch `Strategy.PositionPrice` bereitgestellt wird.
- Protokolliert Take-Profit-Ereignisse für einfacheres Monitoring.
- Der Zeitvergleich basiert auf `DateTimeOffset.CloseTime` der abonnierten Kerzen.

## Verwendungshinweise

- Hängen Sie die Strategie an ein Portfolio, das bereits eine andere Handelsstrategie ausführt, um als Guard-Schicht zu fungieren.
- Wählen Sie einen Kerzen-Zeitrahmen, der der für die Gewinnbewertung erforderlichen Reaktionsfähigkeit entspricht (z.B. 1 Minute für schnelle Kontrolle).
- Stellen Sie sicher, dass die Portfolio-Informationen (insbesondere `CurrentValue`) verfügbar sind; andernfalls setzen Sie vor dem Ausführen von Prozentzielen ein explizites Anfangsguthaben.
- Die Strategie kann mit `StartProtection()` in einer anderen primären Strategie kombiniert werden, um weitere Risikokontrollen hinzuzufügen.
