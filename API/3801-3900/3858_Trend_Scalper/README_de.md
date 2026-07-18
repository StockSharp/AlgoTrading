# Trend-Scalper-Strategie (API/3858)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **TrendScalperStrategy** ist eine C#-Konvertierung des MetaTrader 4 Expert Advisors `Currencyprofits_01_1.mq4`. Der ursprüngliche Roboter ist ein leichter Trendfolge-Scalper, der einen kurzfristigen EMA/SMA-Crossover-Filter mit Breakout-Einstiegen rund um die jüngsten Swing-Hochs und -Tiefs kombiniert. Der StockSharp-Port behält die gleichen Entscheidungsregeln bei und umfasst gleichzeitig das High-Level-Kerzenabonnement und die Indikatorpipeline des Frameworks.

## Handelslogik
1. **Indikatoren**
   - Schneller EMA (Standard 6) bei Schlusskursen.
   - Langsames SMA (Standard 12) bei Schlusskursen.
   - Höchstes Hoch (Standardfenster 6) und Tiefstes Tief (Standardfenster 6), berechnet aus den Kerzenhochs und -tiefs.
2. **Eintrittsbedingungen**
   - **Long**: Der Preis bewegt sich in das aktuelle Tiefstband (`Lowest Low`), während der schnelle EMA über dem langsamen SMA liegt. Die Strategie sendet eine Market-Buy-Order mit dem durch die Money-Management-Regel definierten Volumen.
   - **Short**: Der Preis berührt das jüngste Hochband (`Highest High`), während der schnelle EMA unter dem langsamen SMA liegt. Eine Market-Sell-Order wird nach der gleichen Volumenberechnung erteilt.
   - Das System bleibt flach, während eine Position offen ist, und spiegelt das Single-Order-Verhalten der MQL-Version wider.
3. **Exit Conditions**
   - **Long Exit**: Wenn bei einer offenen Long-Position das Kerzenhoch den aufgezeichneten `Highest High` durchbricht, wird die Position zum Marktwert geschlossen.
   - **Short-Ausstieg**: Wenn eine offene Short-Position beobachtet, dass das Kerzentief durch `Lowest Low` fällt, wird der Short zum Marktwert gedeckt.
   - Ein von `StartProtection` verwalteter schützender Stop-Loss wird jedem Trade beigefügt, wenn `StopLossPoints` größer als Null ist.

## Money-Management
Die Losgrößenlogik reproduziert die drei Modi, die im Skript MQL verfügbar gemacht werden:

| Modus | Beschreibung | Verhalten im Hafen |
|------|-------------|-----------------------|
| `0`  | Feste Lose (`LotsIfNoMM`). | Gibt den konfigurierten `FixedVolume` zurück. |
| `<0` | Bruchteile, die aus dem Kontostand und dem Risikofaktor berechnet werden. | Berechnet `ceil(balance * risk / 10000) / 10`, begrenzt auf 100 Lots. |
| `>0` | Skalierung des Gesamtpakets aus Gleichgewicht und Risikofaktor. | Verwendet dieselbe Grundformel, aber das Ergebnis wird auf die nächste ganze Zahl aufgerundet, auf 1 Los begrenzt und auf 100 begrenzt. |

Der Saldo wird von `Portfolio.CurrentValue` übernommen (und fällt zurück auf `BeginValue`). Wenn der Portfoliowert nicht verfügbar ist, kehrt die Strategie zum festen Volumen zurück, sodass während der Backtests weiterhin Aufträge ausgegeben werden.

## Risikomanagement
- **Stop-Loss**: Der Parameter `StopLossPoints` wird in Preispunkten (Pips) ausgedrückt. Während `OnStarted` wird die Entfernung mit `Security.PriceStep` multipliziert und an `StartProtection` übergeben, sodass StockSharp die Schutzanordnung aufrechterhalten kann.
- **Einzelne Position**: Die Logik erzwingt `Position == 0`, bevor ein neuer Trade eröffnet wird, und verhindert so überlappende Positionen genau wie beim MT4-Experten.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `CandleType` | 15-minütiger Zeitrahmen | Kerzenserien, die für Indikatorberechnungen und Signale verwendet werden. |
| `FastLength` | 6 | Periode des schnellen EMA. |
| `SlowLength` | 12 | Zeitraum der langsamen SMA. |
| `BreakoutWindow` | 6 | Anzahl der Kerzen, die für den höchsten Hoch-/Tiefst-Tief-Breakout-Filter überprüft wurden. |
| `FixedVolume` | 0,1 Lose | Volumen, wenn die Geldverwaltung deaktiviert ist oder ein Fallback erforderlich ist. |
| `MoneyManagementMode` | 0 | Wählt zwischen fester, gebrochener oder gerundeter Losgröße. |
| `MoneyManagementRisk` | 40 | Risikofaktormultiplikator, der bei der saldobasierten Losgrößenbestimmung verwendet wird. |
| `StopLossPoints` | 50 | Stop-Loss-Distanz in Preispunkten (vor dem Aufruf von `StartProtection` in den absoluten Preis umgerechnet). |

## Implementierungshinweise
- Die Indikatorverkettung basiert auf dem übergeordneten `SubscribeCandles().Bind(...)`-Workflow. Es ist keine manuelle Serienpufferung erforderlich.
- Kommentare im Code wurden in englischer Sprache hinzugefügt, um den Repository-Richtlinien zu entsprechen.
- Es wurden keine Unit-Tests geändert; Im Mittelpunkt dieser Konvertierung steht die Strategie und die dazugehörige Dokumentation.

## Nutzungstipps
- Wählen Sie ein Kerzenintervall, das der ursprünglichen Handelsumgebung entspricht (z. B. kurze Intraday-Zeitrahmen für Scalping).
- Stellen Sie sicher, dass das Portfolio über einen gültigen `PriceStep` verfügt, damit die Stop-Loss-Umrechnung in den absoluten Preis korrekt funktioniert.
- Passen Sie `MoneyManagementRisk` sorgfältig an: Höhere Werte führen aufgrund der vom MQL-Experten übernommenen `ceil(balance * risk / 10000)`-Berechnung zu größeren Positionen.
