# Strategie des gleitenden durchschnittlichen Geldes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie ist eine StockSharp-Umsetzung des MetaTrader-Expertenberaters „Moving Average Money“. Es wertet abgeschlossene Kerzen aus und reagiert, wenn der vorherige Balken einen verschobenen einfachen gleitenden Durchschnitt kreuzt. Das System unterstützt sowohl Long- als auch Short-Trades und synchronisiert jede Entscheidung mit dem High-Level-Candle-Abonnement API.

## Handelslogik
- Aus den Schlusskursen wird ein einfacher gleitender Durchschnitt mit konfigurierbarer Länge und visueller Verschiebung berechnet.
- Es werden nur fertige Kerzen verarbeitet, um Doppelbestellungen innerhalb einer Bar zu vermeiden.
- **Short-Einstieg:** wenn die vorherige Kerze über dem verschobenen gleitenden Durchschnitt öffnet und darunter schließt.
- **Long-Einstieg:** wenn die vorherige Kerze unterhalb des verschobenen gleitenden Durchschnitts öffnet und darüber schließt.
- Bei der Strategie handelt es sich nicht um eine Pyramidenposition; Jedes offene Exposure in die entgegengesetzte Richtung wird geschlossen, bevor ein neuer Handel eingerichtet wird.

## Risikomanagement
- Die Stop-Loss-Distanz in Preiseinheiten wird aus `MaximumRiskPercent` abgeleitet. Der aktuelle Portfoliowert, die Instrumentenpreisstufe und der Stufenpreis werden verwendet, um den gewählten Risikoprozentsatz in Preisstufen umzurechnen.
- Die Geld-Brief-Spanne wird von der risikobasierten Distanz abgezogen, wenn die besten Kurse verfügbar sind.
- Take-Profit-Level sind als `stopDistance * ProfitLossFactor` definiert.
- Sowohl Stop- als auch Zielwerte werden bei abgeschlossenen Kerzen überwacht. Wenn eines dieser Niveaus erreicht ist, wird die Position mit einer Marktorder abgeflacht.

## Parameter
- `CandleType` – Zeitrahmen, der für die Signalerkennung verwendet wird.
- `MovingPeriod` – Länge des einfachen gleitenden Durchschnitts.
- `MovingShift` – Anzahl der vollständig geformten Kerzen, die verwendet werden, um den gleitenden Durchschnitt nach rechts zu verschieben.
- `MaximumRiskPercent` – Prozentsatz des aktuellen Portfoliowerts, der den maximalen Verlust pro Trade definiert.
- `ProfitLossFactor` – Multiplikator, der auf die Stoppdistanz angewendet wird, um die Take-Profit-Distanz zu berechnen.
- `TradeVolume` – Basisauftragsvolumen für neue Einträge (Volumenschrittbeschränkungen werden automatisch berücksichtigt).

## Implementierungshinweise
- Die Strategie verfolgt offene Positionen über High-Level-Event-Handler (`OnOwnTradeReceived`), um Stopps und Ziele nach Füllungen neu zu initialisieren.
- Wenn in den Marktdaten Kurse oder Portfoliobewertungen fehlen, werden neue Eingaben übersprungen, um Aufträge ohne angemessene Risikokontrolle zu vermeiden.
- Die Verschiebung des gleitenden Durchschnitts wird mit einem internen Puffer emuliert, sodass die Logik mit der MetaTrader-Version übereinstimmt.
