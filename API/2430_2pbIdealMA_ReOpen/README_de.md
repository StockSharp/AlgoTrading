# 2pb Ideal MA ReOpen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Implementiert den MQL-Experten "Exp_2pbIdealMA_ReOpen" mit der High-Level-API von StockSharp.
- Handelt einen konträren Crossover zwischen einem einzelnen idealen gleitenden Durchschnitt und einem dreistufigen idealen gleitenden Durchschnitt.
- Fügt zu Gewinnpositionen hinzu, wenn der Preis um eine konfigurierbare Anzahl von Ticks voranschreitet, und schließt optional Positionen bei entgegengesetzten Signalen.

## Indikatoren
- **2pb Ideal 1 MA** – einzelner idealer gleitender Durchschnitt mit zwei Gewichtungsperioden. Reagiert schnell und definiert die kurzfristige Tendenz.
- **2pb Ideal 3 MA** – dreifache Kaskade desselben idealen Filters (Stufen X, Y, Z). Reagiert langsamer und repräsentiert den Hintergrundtrend.

## Handelslogik
1. Abonniert die ausgewählte Kerzenserie (Standard H4) und wertet Signale nur bei abgeschlossenen Kerzen aus.
2. Speichert Filterwerte `SignalBarShift` Bars zurück (Standard 1). Verwendet das Wertepaar bei den Offsets `SignalBarShift` und `SignalBarShift + 1`, um Kreuzungen zu erkennen.
3. **Long-Einstieg** – wenn der schnelle Filter zwei Bars zuvor über dem langsamen Filter lag und einen Bar zuvor darunter fiel (bärisches Kreuz), eine Long-Position eröffnen, wenn Long-Trades aktiviert und keine Position offen ist.
4. **Short-Einstieg** – wenn der schnelle Filter zwei Bars zuvor unter dem langsamen Filter lag und einen Bar zuvor darüber stieg (bullisches Kreuz), eine Short-Position eröffnen, wenn Short-Trades aktiviert und keine Position offen ist.
5. **Wiedereintritte** – während eine Position profitabel ist, eine weitere Order von `PositionVolume` hinzufügen, sobald der Preis sich um `PriceStepTicks * Security.PriceStep` in Handelsrichtung bewegt. Die Anzahl der Ergänzungen pro Richtung wird durch `MaxReEntries` begrenzt.
6. **Ausstiege** – wenn der entgegengesetzte Crossover erscheint und das jeweilige Ausstiegs-Flag aktiviert ist, die offene Position schließen, bevor neue Einstiege betrachtet werden.
7. Optionalen Stop Loss und Take Profit mit den konfigurierten Tick-Abständen anwenden.

## Parameter
- `CandleType` – Zeitrahmen der Arbeits-Kerzenserie.
- `PositionVolume` – Basisvolumen für Einstiege und Wiedereintritte (auch `Strategy.Volume` zugewiesen).
- `StopLossTicks` / `TakeProfitTicks` – Schutzabstände in Ticks; über `Security.PriceStep` in Preise umgerechnet.
- `PriceStepTicks` – Anzahl der Ticks, die zwischen aufeinanderfolgenden Wiedereintritts-Orders erforderlich sind.
- `MaxReEntries` – maximale Anzahl von Ergänzungs-Trades pro Richtung.
- `EnableBuyEntries` / `EnableSellEntries` – Öffnen von Long- oder Short-Positionen erlauben.
- `EnableBuyExits` / `EnableSellExits` – bestehende Positionen schließen, wenn das entgegengesetzte Signal erscheint.
- `SignalBarShift` – Anzahl der zurückliegenden Bars zur Crossover-Bewertung (ahmt das Original-`SignalBar` nach).
- `Period1`, `Period2` – Gewichtungen für den einzelnen idealen gleitenden Durchschnitt.
- `PeriodX1`, `PeriodX2`, `PeriodY1`, `PeriodY2`, `PeriodZ1`, `PeriodZ2` – Gewichtungen für jede Stufe des dreifachen idealen gleitenden Durchschnitts.

## Risikomanagement
- Stop-Loss- und Take-Profit-Schutzmaßnahmen werden über `StartProtection` aktiviert, wenn die entsprechenden Tick-Abstände größer als null sind.
- Die Strategie eröffnet keine neuen Trades, während eine entgegengesetzte Position noch offen ist, was das MQL-Verhalten widerspiegelt.

## Hinweise
- Funktioniert mit jedem Instrument, das `Security.PriceStep` bereitstellt; die Standardkonfiguration zielt auf H4-Kerzen ab.
- Es wird kein Python-Port bereitgestellt, entsprechend der ursprünglichen Anforderung.
