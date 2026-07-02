# Early-Bird-Range-Break-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Early Bird Range Break Strategy** ist eine C#-Portierung des Expert Advisors MetaTrader „earlyBird3“. Es zielt auf Bereichsausbrüche ab, die kurz nach Eröffnung der europäischen Handelssitzung stattfinden. Der Algorithmus beobachtet einen Konsolidierungsbereich am frühen Morgen, filtert potenzielle Ausbrüche mit einem 14-Perioden-RSI und gibt bis zu drei Marktaufträge in Richtung des Ausbruchs ein. Jede Order verwendet vordefinierte Take-Profit-Levels, einen gemeinsamen Stop-Loss und einen optionalen Trailing-Mechanismus, der nur aktiviert wird, wenn die Volatilität über ihren aktuellen Durchschnitt hinaus ansteigt.

## Datenanforderungen
- Ein einzelner Zeitrahmen-Kerzenstrom (Standard: 5-Minuten-Kerzen) für das gehandelte Instrument.
- Das Instrument muss einen gültigen `PriceStep` liefern, da alle Stop-Loss- und Take-Profit-Abstände in Punkten definiert sind.
- Handelszeiten werden anhand der Zeitstempel eingehender Kerzen (Serverzeit der Datenquelle) ausgewertet.

## Handelssitzung
1. **Bereichskonstruktion** – Zwischen `RangeStartHour` und `RangeEndHour` verzeichnet die Strategie das höchste Hoch und das niedrigste Tief.
2. **Handelsfenster** – Nach `TradingStartHour:TradingStartMinute` und vor `TradingEndHour` wird die Breakout-Logik aktiv.
3. **Zwangsschließung** – Bei `ClosingHour` werden alle verbleibenden Positionen unabhängig von Gewinn oder Verlust liquidiert.
4. **Nur werktags** – Signale werden von Montag bis Freitag verarbeitet.

## Eingabelogik
1. Ein Long-Breakout-Level ist auf `range high + EntryBufferPoints` festgelegt, während ein Short-Breakout-Level auf `range low - EntryBufferPoints` festgelegt ist. Der Puffer wird in Preispunkten ausgedrückt.
2. Der RSI-Filter muss für eine lange Einrichtung größer als 50 und für eine kurze Einrichtung kleiner oder gleich 50 sein.
3. An jedem Handelstag ist nur ein Ausbruch pro Richtung zulässig. Bei Auslösung werden sofort drei Marktaufträge (Standardvolumen `0.1`) übermittelt.
4. Wenn bereits eine Gegenposition offen ist und `HedgeTrading` deaktiviert ist, wird das neue Signal ignoriert. Wenn `HedgeTrading` aktiviert ist, schließt die Strategie zunächst die bestehende Position und geht dann in die neue Richtung. Dies spiegelt die Absicht des ursprünglichen EA wider, verwendet jedoch eine Positionsumkehr, da StockSharp-Konten saldiert werden.

## Exit-Management
1. **Stop-Loss** – Ein gemeinsamer Stop-Loss (`StopLossPoints`) wird auf die Gesamtposition angewendet. Wenn der Preis das Niveau überschreitet, wird die verbleibende Größe sofort geschlossen.
2. **Take-Profit-Leiter** – Drei Ziele (`TakeProfit1Points`, `TakeProfit2Points`, `TakeProfit3Points`) schließen jeweils einen Positionsteil. Der verbleibende Teil bleibt geöffnet, bis er am Ende der Sitzung gestoppt, nachgezogen oder geschlossen wird.
3. **Trailing Stop** – Wenn nur noch ein Teil übrig ist, muss der aktuelle Kerzenbereich `ATR * TrailingRiskMultiplier` überschreiten. Wenn der Preis um mindestens `TrailingStopPoints` gestiegen ist, wird der Stop-Loss unter Beibehaltung der anfänglichen Stop-Distanz in Handelsrichtung erhöht.
4. **Sitzungsabschluss** – Jede offene Belichtung wird vollständig reduziert, sobald die aktuelle Zeit `ClosingHour` erreicht.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `AutoTrading` | Aktiviert/deaktiviert die Auftragsausführung. | `true` |
| `HedgeTrading` | Ermöglicht die Positionsumkehr bei entgegengesetzten Signalen (implementiert als Flat-and-Reverse). | `true` |
| `OrderType` | `0` – beide Richtungen, `1` – nur lang, `2` – nur kurz. | `0` |
| `TradeVolume` | Volumen pro übermittelter Marktorder. | `0.1` |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten. | `60` |
| `TakeProfit1Points` | Take-Profit-Distanz für den ersten Teil. | `10` |
| `TakeProfit2Points` | Take-Profit-Distanz für den zweiten Teil. | `20` |
| `TakeProfit3Points` | Take-Profit-Distanz für den dritten Teil. | `30` |
| `TrailingStopPoints` | Minimale günstige Bewegung, bevor der Trailing Stop aktiviert wird. | `15` |
| `TrailingRiskMultiplier` | Bei der Validierung der Volatilitätsausweitung wird der Multiplikator auf ATR angewendet. | `1.0` |
| `EntryBufferPoints` | Zusätzliche Distanz zu den Ausbruchsniveaus hinzugefügt. | `2` |
| `RangeStartHour` | Stunde, zu der der Referenzbereich beginnt. | `3` |
| `RangeEndHour` | Stunde, in der der Referenzbereich endet. | `7` |
| `TradingStartHour` | Stunde, in der Breakout-Einträge zulässig sind. | `7` |
| `TradingStartMinute` | Minute, in der Breakout-Einträge zulässig sind. | `15` |
| `TradingEndHour` | Stunde, nach der keine neuen Einträge mehr vorgenommen werden. | `15` |
| `ClosingHour` | Stunde, in der alle Geschäfte geschlossen sind. | `17` |
| `RsiPeriod` | RSI-Lookback, der zum Filtern verwendet wird. | `14` |
| `VolatilityPeriod` | ATR Rückblick auf das Volatilitätstor. | `16` |
| `CandleType` | Für die Analyse verwendete Kerzenserie (Standard 5 Minuten). | `TimeSpan.FromMinutes(5)` |

## Hinweise zur Implementierung
- Die Strategie abonniert Kerzen über die StockSharp-Hochebene API und bindet die Indikatoren RSI und ATR direkt an das Abonnement.
- Indikatorwerte werden innerhalb des `ProcessCandle`-Rückrufs verbraucht, ohne `GetValue` aufzurufen oder benutzerdefinierte Puffer zu speichern, gemäß den Projektrichtlinien.
- Es werden nur fertige Kerzen verarbeitet; Teilaktualisierungen werden ignoriert.
- Alle Preisabstände werden mit dem Instrument `PriceStep` von Punkten in absolute Preise umgerechnet. Stellen Sie sicher, dass die Sicherheitsdefinition die richtige Tick-Größe offenlegt.
- Der ursprüngliche Fachberater hielt separate MQL-Aufträge zur Absicherung bereit. StockSharp verwendet Nettopositionen, daher führt dieser Port einen Schließ- und Umkehrvorgang aus, wenn `HedgeTrading` aktiviert ist.

## Anwendungstipps
- Passen Sie den Zeitrahmen der Kerze an den im ursprünglichen EA verwendeten Handelsplatz an (M5 bis H1 in MetaTrader). Passen Sie `RangeStartHour`, `RangeEndHour` und das Handelsfenster an, um den lokalen Marktplan Ihres Datenfeeds widerzuspiegeln.
- Konzentrieren Sie sich bei der Optimierung auf den Breakout-Puffer, die Take-Profit-Leiter und den Volatilitätsfilter, da diese das Gleichgewicht zwischen falschen Breakouts und verpassten Bewegungen definieren.
- Das Trailing ist bewusst konservativ. Wenn Sie engere Ausstiege benötigen, sollten Sie `TrailingRiskMultiplier` oder `StopLossPoints` reduzieren, damit die Trailing-Anpassungen häufiger erfolgen.
