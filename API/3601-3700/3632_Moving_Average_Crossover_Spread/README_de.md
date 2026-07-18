# Crossover-Spread-Strategie mit gleitendem Durchschnitt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des Expertenberaters MQL4 **"EA - Moving Average"** (Datei `EA - Moving Average.mq4`).
Es handelt mit einem einzelnen Instrument, indem es auf Überkreuzungen des gleitenden Durchschnitts reagiert, die beim Öffnen jeder neuen Kerze erkannt werden.

## Kernidee

- Verwenden Sie einen schnellen und einen langsamen exponentiellen gleitenden Durchschnitt (EMA), der für die ausgewählte Kerzenserie berechnet wird.
- Warten Sie, bis eine neue Kerze verfügbar ist, und werten Sie die EMA-Werte der beiden zuletzt abgeschlossenen Kerzen aus, indem Sie die `iMA(..., shift=1/2)`-Aufrufe aus dem Originalcode replizieren.
- Eröffnen Sie eine **Long-Position**, wenn der schnelle EMA den langsamen EMA der vorherigen Kerze überschritten hat, während bei der Kerze davor der schnelle EMA noch unter dem langsamen EMA lag.
- Eröffnen Sie eine **Short-Position**, wenn der schnelle EMA den langsamen EMA der vorherigen Kerze unterschritten hat, während die Kerze davor noch den schnellen EMA über dem langsamen EMA hatte.
- Es kann jeweils nur eine Position offen sein. Die Strategie ignoriert neue Signale, bis alle Aufträge geschlossen sind.

## Orderverwaltung

- Vor der Auftragserteilung wird der aktuelle Spread überprüft. Wenn der beste Brief und das beste Geld verfügbar sind, wird der Spread in Instrumentenpunkte umgerechnet und mit `MaxSpreadPoints` verglichen. Signale, die das Limit überschreiten, werden übersprungen, genau wie der ursprüngliche `MarketInfo(..., MODE_SPREAD)`-Guard.
- Nachdem eine Marktorder übermittelt wurde, spiegelt die Strategie Schutzniveaus um den Einstiegspreis herum wider:
  - Der Stop-Loss wird auf den langsamen EMA-Wert der vorherigen Kerze plus/minus dem konfigurierten `StopLossPoints` gesetzt.
  - Der Take-Profit wird im gleichen Abstand vom Einstiegspreis festgelegt wie der Stop-Loss, wodurch ein symmetrisches Ziel wie in der MQL-Implementierung (`Ask + (Ask - StopLoss)` / `Bid - (StopLoss - Bid)`) entsteht.
- Alle in Punkten ausgedrückten Preisabstände werden über das Instrument `PriceStep` in absolute Preise übersetzt, sodass das Verhalten der punktbasierten Konfiguration aus MetaTrader entspricht.

## Konvertierungshinweise

- Der ursprüngliche Expert erlaubt die Auswahl verschiedener Modi für den gleitenden Durchschnitt, seine Standardeinstellungen verwenden jedoch EMA (`MAMode = 1`). Die StockSharp-Version konzentriert sich auf EMA, um die Implementierung prägnant zu halten; Bei Bedarf können verschiedene Glättungsalgorithmen hinzugefügt werden.
- Das Handelsvolumen wird über den Parameter `TradeVolume` bereitgestellt und während `OnStarted` `Strategy.Volume` zugeordnet.
- Die Strategie basiert ausschließlich auf Kerzendaten, die über `CandleType` bereitgestellt werden. Außer dem zweiwertigen EMA-Verlauf, der zum Erkennen von Überschneidungen erforderlich ist, gibt es keine zusätzlichen Indikatorsammlungen oder historischen Puffer.

## Parameter

- `CandleType` – Kerzendatentyp und Zeitrahmen zum Abonnieren.
- `FastPeriod` – Länge des schnellen EMA (standardmäßig 21).
- `SlowPeriod` – Länge des langsamen EMA (Standard: 84).
- `StopLossPoints` – Stop-Loss-Distanz in Instrumentenpunkten relativ zum langsamen EMA.
- `MaxSpreadPoints` – maximal zulässiger Spread in Punkten, bevor eine neue Bestellung abgelehnt wird.
- `TradeVolume` – Losgröße, die beim Senden von Marktaufträgen verwendet wird.

## Nutzungstipps

1. Wählen Sie vor Beginn der Strategie das Symbol und den Kerzenzeitrahmen aus, damit die EMA-Werte mit dem beabsichtigten Diagramm in MetaTrader übereinstimmen.
2. Stellen Sie Level-1-Daten (bester Geld-/Briefkurs) bereit, wenn Sie möchten, dass der Spread-Filter in Echtzeit funktioniert. andernfalls geht die Strategie davon aus, dass der Spread akzeptabel ist.
3. Stellen Sie sicher, dass die Sicherheit über einen gültigen `PriceStep` verfügt. Ohne sie kann die Strategie punktbasierte Distanzen nicht in absolute Preise umwandeln und wird die Platzierung von Schutzaufträgen überspringen.
