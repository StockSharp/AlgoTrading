# Doji-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wandelt den MQL4 „DojiTrader“ Expert Advisor in ein StockSharp C#-Beispiel um. Es sucht nach aktuellen Doji-Kerzen und handelt mit einem Ausbruch aus der Doji-Range während der wichtigsten Sitzungen in Europa und den USA.

## Handelslogik
- Die Strategie verarbeitet nur fertige Kerzen aus dem ausgewählten Zeitrahmen (standardmäßig 30-Minuten-Kerzen).
- Der Handel ist nur zwischen 08:00 und 17:00 Uhr Plattformzeit gestattet.
- Während er flach ist, blickt er auf drei abgeschlossene Kerzen zurück und merkt sich den letzten Doji (Eröffnungspreis entspricht Schlusspreis).
- Wenn die Kerze unmittelbar nach dem Doji über dem Doji-Hoch schließt, ist ein langer Ausbruch möglich. Wenn es unter dem Doji-Tief schließt, ist ein kurzer Ausbruch möglich.
- Sobald eine nachfolgende Kerze über dem Aktivierungspreis schließt, sendet die Strategie eine Marktorder in Ausbruchsrichtung.
- Nach dem Eintritt wird der Doji-Bereich zur Austrittskontrolle beibehalten. Die Position wird geschlossen, wenn:
  - Die vorherige Kerze schließt wieder innerhalb der Spanne (Long: knapp unter dem Doji-Tief, Short: knapp über dem Doji-Hoch).
  - Die Candle-Extreme erreichen die synthetischen Stop-Loss- oder Take-Profit-Niveaus, die die ursprünglichen MQL4-Fixkomma-Ausstiege nachahmen.

## Parameter
- **Auftragsvolumen** – für Marktaufträge verwendetes Volumen.
- **Take Profit (Schritte)** – Abstand zum Gewinnziel gemessen in Preisschritten.
- **Stop-Loss (Schritte)** – Abstand zum Schutzstopp in Preisschritten.
- **Kerzentyp** – Zeitrahmen der Kerzen, die zur Signalerkennung verwendet werden.

Die Stop-Loss- und Take-Profit-Berechnungen basieren auf dem Wertpapierpreisschritt und emulieren den ursprünglichen EA, der feste Pip-Abstände verwendete. Wenn innerhalb der letzten drei Kerzen kein gültiger Doji vorhanden ist, wird der Ausbruchsstatus gelöscht und die Suche beginnt erneut.
