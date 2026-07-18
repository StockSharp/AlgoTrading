# Twenty 200 Pips-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Strategie repliziert den ursprünglichen **20/200 Pips** MQL5-Experten. Sie untersucht Stundenkerzen und vergleicht zwei historische Eröffnungspreise (`Open[t1]` und `Open[t2]`). Wenn die Differenz zwischen diesen Eröffnungen während einer bestimmten Stunde ein konfigurierbares Delta übersteigt, eröffnet die Strategie einen einzelnen Trade für die Sitzung und setzt auf feste Take-Profit- und Stop-Loss-Levels.

## Handelslogik
1. Stundenkerzen abonnieren (konfigurierbar) und den Eröffnungspreis in zwei `Shift`-Indikatoren einspeisen, um die Eröffnungen bei den erforderlichen Indizes abzurufen.
2. Bei jeder abgeschlossenen Kerze das Flag "kann handeln" zurücksetzen, sobald die aktuelle Stunde größer als die konfigurierte Handelsstunde ist. Dies spiegelt den täglichen Reset im ursprünglichen Expertenberater wider.
3. Wenn die Stunde mit der konfigurierten Handelsstunde übereinstimmt und keine Position offen ist, die gespeicherten Eröffnungspreise vergleichen:
   - Wenn `Open[t1] > Open[t2] + delta`, eine Market-**Verkaufsorder** einreichen.
   - Wenn `Open[t1] + delta < Open[t2]`, eine Market-**Kauforder** einreichen.
4. Nach dem Senden einer Order verbietet die Strategie neue Einstiege bis zum nächsten täglichen Reset. Schützende Take-Profit- und Stop-Loss-Orders werden über `StartProtection` verwaltet.

## Parameter
- `TakeProfit` – Abstand in Preispunkten für die Take-Profit-Order (Standard 200 Punkte).
- `StopLoss` – Abstand in Preispunkten für die Stop-Loss-Order (Standard 2000 Punkte).
- `TradeHour` – Stunde des Tages, zu der die Einstiegsprüfung durchgeführt wird (Standard 18).
- `FirstOffset` – Index des älteren Eröffnungspreises (entspricht `Open[t1]` im MQL-Skript, Standard 7).
- `SecondOffset` – Index des neueren Eröffnungspreises (`Open[t2]`, Standard 2).
- `DeltaPoints` – Minimale Differenz in Punkten zwischen den beiden Eröffnungen, um einen Trade auszulösen (Standard 70).
- `Volume` – Ordergröße für Markteinstiege (Standard 0.1).
- `CandleType` – Zeitrahmen für Berechnungen (Standard 1-Stunden-Kerzen).

## Implementierungshinweise
- `Shift`-Indikatoren werden manuell verarbeitet, um auf historische Eröffnungspreise zuzugreifen, ohne benutzerdefinierte Sammlungen zu pflegen.
- Die Strategie ruft `StartProtection` einmal während `OnStarted` auf, um die im MQL-Experten definierten Stop-Loss/Take-Profit-Levels zu emulieren.
- Englische Kommentare sind direkt im Code enthalten, um die Wartung und Überprüfung zu erleichtern.
- Pro Tag ist nur ein Trade erlaubt, da `_canTrade` direkt nach dem Platzieren einer Order gelöscht wird und erst wiederhergestellt wird, nachdem die konfigurierte Handelsstunde vergangen ist.

## Verwendung
1. Die Strategie an ein Instrument anhängen und die Parameter entsprechend dem Zielinstrument konfigurieren.
2. Sicherstellen, dass das Instrument einen gültigen `PriceStep` hat; er wird verwendet, um punktbasierte Parameter in absolute Preisabstände umzurechnen.
3. Die Strategie starten. Sie wartet bis zur konfigurierten Stunde und handelt bei der nächsten abgeschlossenen Kerze, wenn die Eröffnungspreisbedingungen erfüllt sind.
