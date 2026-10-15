# Bull vs Medved-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Bull vs Medved ist eine Limit-Order-Ausbruchsstrategie, die ursprünglich für MetaTrader 5 veröffentlicht wurde. Sie überwacht die letzten drei abgeschlossenen Kerzen und erlaubt Trades nur während sechs gleichmäßig über den Tag verteilten Fünf-Minuten-Fenstern. Wenn eine spezifische bullische oder bearische Kerzensequenz erscheint, platziert die Strategie eine ausstehende Limitorder, die vom aktuellen Spread versetzt ist, und schützt die Position mit symmetrischen Stop-Loss- und Take-Profit-Zielen.

## Handelslogik

1. **Handelsfenster** – Orders werden nur bewertet, wenn die aktuelle Tageszeit innerhalb eines der sechs konfigurierbaren Fenster liegt (standardmäßig 00:05, 04:05, 08:05, 12:05, 16:05, 20:05) und innerhalb der konfigurierten Dauer (standardmäßig 5 Minuten). Das Verlassen des Fensters setzt die Ein-Order-pro-Fenster-Sperre zurück.
2. **Erforderliche Kerzendaten** – die Strategie wartet auf drei abgeschlossene Kerzen, bevor Signale generiert werden. Berechnungen verwenden immer die drei zuletzt abgeschlossenen Kerzen.
3. **Bullische Setups**:
   - **Regular Bull**: die Kerze vor drei Perioden schließt über der Eröffnung der zweiten Kerze, die zweite Kerze hat mindestens einen 1-Pip bullischen Körper, und die jüngste Kerze hat einen bullischen Körper größer als den konfigurierten `CandleSizePips`-Schwellenwert.
   - **Bad-Bull-Filter**: wenn alle drei Kerzen große bullische Körper haben, wird das Signal ignoriert, um parabolische Bewegungen zu vermeiden.
   - **Cool Bull**: nach einem starken bearischen Rücksetzer (zweite Kerze schließt mindestens 2 Pips unter ihrer Eröffnung) muss die jüngste Kerze den Rücksetzer einhüllen und mindestens 40% des normalen `CandleSizePips`-Körpers drucken. Entweder ein Regular Bull (ohne Bad-Bull-Filter) oder ein Cool-Bull-Muster löst ein Long-Setup aus.
   - Bei einem gültigen Long-Signal platziert die Strategie eine **Buy-Limit**-Order unterhalb des besten Ask um `IndentUpPips` (in Instrumentpreiseinheiten umgerechnet).
4. **Bearisches Setup**:
   - Wenn die jüngste Kerze einen bearischen Körper größer als `CandleSizePips` hat, platziert die Strategie eine **Sell-Limit**-Order oberhalb des besten Bid um `IndentDownPips`.
5. **Risikomanagement** – sobald eine Position eröffnet wird, hängt die Strategie automatisch absolute Stop-Loss- und Take-Profit-Ziele mit den konfigurierten Pip-Distanzen an.
6. **Orderverwaltung** – pro Fenster kann nur eine Order gesendet werden und es wird keine neue Order platziert, während eine weitere Limitorder für dasselbe Symbol aktiv ist.

## Parameter

- `OrderVolume` – Handelsvolumen für Limitorders.
- `CandleSizePips` – Mindest-Körpergröße bullisch/bearisch für die neueste Kerze.
- `StopLossPips` – Schutz-Stop-Distanz vom Einstiegspreis.
- `TakeProfitPips` – Gewinnziel-Distanz vom Einstiegspreis.
- `IndentUpPips` – Buy-Limit-Versatz unterhalb des besten Ask.
- `IndentDownPips` – Sell-Limit-Versatz oberhalb des besten Bid.
- `EntryWindowMinutes` – Dauer jedes erlaubten Handelsfensters.
- `CandleType` – Kerzenzeitrahmen zur Musterbewertung.
- `StartTime0` … `StartTime5` – Startzeiten der sechs Handelsfenster.

## Zusätzliche Hinweise

- Die Strategie abonniert das Orderbuch, um die neuesten Bid/Ask-Preise für präzise Limitplatzierung zu pflegen. Wenn keine Buchdaten verfügbar sind, greift sie auf den letzten Kerzenschluss zurück.
- Preisoffsets werden in pip-großen Einheiten berechnet, die sich automatisch an 3- und 5-stellige Kurse anpassen.
- Stop-Loss und Take-Profit werden über `StartProtection` angewendet, damit die Ziele dem tatsächlichen Ausführungspreis der Limitorder folgen.
