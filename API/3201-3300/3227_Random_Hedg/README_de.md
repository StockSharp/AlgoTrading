# Random Hedg-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Random Hedg-Strategie** ist eine High-Level-Portierung des MetaTrader-Expert-Advisors „Random Hedg" für StockSharp. Der ursprüngliche EA öffnet gleichzeitig eine Market-Buy- und eine Market-Sell-Order und verwaltet dann beide Seiten mit einer Kombination aus festem Stop-Loss, Take-Profit, Break-Even und Trailing-Logik. Die Konvertierung behält dieses Kernverhalten bei, während jede Einstellung als Strategieparameter verfügbar gemacht wird, sodass der Bot direkt im StockSharp Designer angepasst oder optimiert werden kann.

## Handelslogik
1. **Initiale Absicherung** – wenn die Strategie ohne Position ist, sendet sie sofort zwei Market-Orders (Kauf und Verkauf) mit demselben konfigurierbaren Volumen. Beide Seiten erhalten einen Stop-Loss und einen Take-Profit ausgedrückt in Pips.
2. **Break-Even-Schutz** – sobald sich der Preis um die konfigurierte Anzahl von Pips zugunsten einer Seite bewegt, wird das Stop-Niveau auf Break-Even plus einem optionalen Offset (Long-Positionen) oder Break-Even minus dem Offset (Short-Positionen) verschoben. Dies spiegelt den „Bewegen zu kein Verlust"-Schalter des EA wider.
3. **Trailing-Stop** – sobald der Gewinn die Trailing-Distanz überschreitet, folgt der Stop dem Preis. Bei Longs folgt der Stop dem höchsten Preis minus der Trailing-Distanz; bei Shorts folgt er dem niedrigsten Preis plus der Distanz.
4. **Schutzausstiege** – jede Seite wird geschlossen, wenn ihr Take-Profit oder Stop-Loss berührt wird. Optional kann die Strategie beide Seiten liquidieren, wenn eine Kerze unterhalb des unteren Bollinger-Bandes schließt, was den Exit-Filter aus dem Originalcode nachbildet.
5. **Zyklusneustart** – sobald beide Seiten geschlossen sind, setzt die Strategie ihre internen Tracker zurück und wartet auf die nächste Kerze, um ein neues abgesichertes Paar zu öffnen.

## Parameter
- `HedgeVolume` – Volumen zum Öffnen beider Absicherungspositionen (Standard 0,1 Kontrakte).
- `StopLossPips` – Distanz des schützenden Stop-Loss (Standard 200 Pips).
- `TakeProfitPips` – Distanz des Take-Profit (Standard 200 Pips).
- `TrailingStopPips` – Trailing-Schritt, der angewendet wird, wenn eine Position rentabel wird (Standard 40 Pips).
- `BreakEvenTriggerPips` – erforderlicher Gewinn, bevor der Stop auf Break-Even verschoben wird (Standard 10 Pips).
- `BreakEvenOffsetPips` – zusätzlicher gesicherter Gewinn beim Break-Even-Verschiebungsvorgang (Standard 5 Pips).
- `EnableTrailing` – aktiviert oder deaktiviert die Trailing-Stop-Verwaltung.
- `EnableBreakEven` – aktiviert oder deaktiviert die Break-Even-Funktion.
- `EnableExitStrategy` – aktiviert den auf Bollinger-Bändern basierenden Liquidierungsfilter.
- `BollingerPeriod` – Periode der Bollinger-Bänder für den optionalen Ausstieg (Standard 20 Kerzen).
- `BollingerWidth` – Breitenmultiplikator der Bollinger-Bänder (Standard 2).
- `CandleType` – Kerzendatenserie für die Logik (Standard 30-Minuten-Zeitrahmen).

## Implementierungshinweise
- Die Konvertierung verwendet die High-Level-`Strategy`-API mit Kerzenabonnements und den `BindEx`-Mechanismus zur Berechnung der Bollinger-Bänder im Handumdrehen.
- Der interne Zustand verfolgt den Einstiegspreis, das Volumen und die dynamischen Schutzniveaus für jede Seite. Dies ermöglicht es der C#-Version, die Geldverwaltungshelfer des originalen EA nachzuahmen, ohne sich auf plattformspezifische Order-Handles zu verlassen.
- Ausstehende Order-Volumen werden separat verfolgt, sodass Ausführungen als Einstiege oder Ausstiege klassifiziert werden können, auch wenn Kauf- und Verkaufsgeschäfte nacheinander erfolgen.
- Die Strategie erwartet ein absicherungsfähiges Konto, da sie gleichzeitig Long- und Short-Engagements hält, genau wie der ursprüngliche Expert-Advisor.
- Geldbasierte Trailing- und Prozentgewinn-Funktionen aus dem MQL-Code werden absichtlich weggelassen. Sie hängen von brokerspezifischen Bilanzdaten ab und wurden in der Praxis selten genutzt; die StockSharp-Version konzentriert sich auf das Kernverhalten der Preisaktionsverwaltung.

## Dateien
- `CS/RandomHedgStrategy.cs` – C#-Hauptimplementierung mit detaillierten englischen Inline-Kommentaren.
- `README.md` – diese Dokumentation (Englisch).
- `README_ru.md` – russische Übersetzung.
- `README_zh.md` – vereinfachte chinesische Übersetzung.
