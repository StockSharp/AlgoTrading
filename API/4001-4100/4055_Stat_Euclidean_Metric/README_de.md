# Statistische euklidische metrische Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert das Verhalten des MetaTrader-Expertenberaters `Stat_Euclidean_Metric.mq4`. Es überwacht MACD Umkehrungen für ein einzelnes Instrument und einen einzelnen Zeitrahmen. Wenn die MACD-Linie einen lokalen Wendepunkt bildet, eröffnet die Strategie entweder sofort eine Position (Trainingsmodus) oder validiert das Setup mit einem K-NN-Klassifikator (k-Nearest Neighbors), der die aktuelle Marktstruktur mit historischen Merkmalsvektoren vergleicht, die in Binärdateien gespeichert sind.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzentyp und berechnen Sie den Indikator MACD für den typischen Preis ((Hoch + Tief + Schluss) / 3).
2. Erkennen Sie eine rückläufige Umkehr, wenn die letzten drei abgeschlossenen MACD-Werte `MACD[2] <= MACD[1]` und `MACD[1] > MACD[0]` erfüllen.
3. Erkennen Sie eine zinsbullische Umkehr, wenn `MACD[2] >= MACD[1]` und `MACD[1] < MACD[0]`.
4. Abhängig vom gewählten Modus:
   - **Trainingsmodus (`TrainingMode = true`)** – Eröffnen Sie eine Marktorder in Richtung der Umkehrung, nachdem Sie optional die aktuelle Position geschlossen haben. Dies ahmt das ursprüngliche Verhalten von EA nach, wenn neue Stichproben erfasst werden.
   - **Klassifikatormodus (`TrainingMode = false`)** – Berechnen Sie fünf Verhältnisse einfacher gleitender Durchschnitte des typischen Preises und bewerten Sie die Erfolgswahrscheinlichkeit mit einem k-NN-Modell. Geben Sie Bestellungen nur auf, wenn die Wahrscheinlichkeit die konfigurierten Schwellenwerte überschreitet.
5. Wenden Sie das integrierte `StartProtection`-Modul an, um Stop-Loss- und Take-Profit-Levels in Instrumentenschritten festzulegen.

## Merkmalsvektor zur Klassifizierung
Das k-NN-Modell verwendet die folgenden Verhältnisse, die für die gerade geschlossene Kerze berechnet wurden:
- SMA(89) / SMA(144)
- SMA(144) / SMA(233)
- SMA(21) / SMA(89)
- SMA(55) / SMA(89)
- SMA(2) / SMA(55)

Jede in den Datensatzdateien gespeicherte Stichprobe enthält sechs `double`-Werte: die fünf oben genannten Verhältnisse und eine Bezeichnung (`0` für ein ungünstiges Ergebnis, `1` für einen erfolgreichen Handel). Während der Auswertung wählt die Strategie die nächstgelegenen `NeighborCount` Stichproben aus, mittelt ihre Bezeichnungen und interpretiert das Ergebnis als Erfolgswahrscheinlichkeit.

## Datensatzdateien
- `BuyDatasetPath` – Pfad zur Binärdatei mit Vektoren, die nach bullischen Trades gesammelt wurden.
- `SellDatasetPath` – Pfad zur Binärdatei mit Vektoren, die nach rückläufigen Trades gesammelt wurden.

Wenn ein Pfad relativ ist, wird er anhand von `Environment.CurrentDirectory` aufgelöst. Fehlende Dateien werden im Protokoll gemeldet und als leerer Datensatz behandelt. Diese Implementierung liest Datensätze, aktualisiert jedoch nicht automatisch neue Beispiele oder hängt sie an. Der Export neuer Vektoren muss im Trainingsmodus extern erfolgen.

## Parameter
- **TrainingMode** – Wechseln Sie zwischen reinem MACD-Handel und klassifikatorgestütztem Handel.
- **BuyThreshold / SellThreshold** – vom Klassifikator zurückgegebene minimale Wahrscheinlichkeit, Geschäfte in der primären Richtung zu eröffnen.
- **AllowInverseEntries** – ermöglicht konträre Trades, wenn die Wahrscheinlichkeit extrem gering ist.
- **InverseBuyThreshold / InverseSellThreshold** – maximale Wahrscheinlichkeit, die immer noch einen Handel in die entgegengesetzte Richtung auslöst.
- **FastLength / SlowLength / SignalLength** – MACD EMA Längen.
- **TakeProfitPoints / StopLossPoints** – Schutzniveaus ausgedrückt in Instrumentenschritten.
- **ClosePositionsOnSignal** – Schließen Sie die aktuelle Nettoposition, bevor Sie eine neue Order senden.
- **BuyDatasetPath / SellDatasetPath** – Binärdateien, die historische Vektoren speichern.
- **NeighborCount** – Anzahl der Nachbarn, die bei der k-NN-Abstimmung verwendet werden.
- **CandleType** – Kerzenserie, die für alle Indikatoren verwendet wird.

## Nutzungsempfehlungen
- Geben Sie absolute oder arbeitsverzeichnisrelative Pfade zu den Datensatzdateien an, bevor Sie den Klassifikatormodus aktivieren.
- Sammeln Sie hochwertige Proben, indem Sie die Strategie im Trainingsmodus für historische Daten ausführen und Vektoren manuell exportieren.
- Optimieren Sie die Schwellenwerte und die Anzahl der Nachbarn, um den Klassifikator an neue Märkte oder Instrumente anzupassen.
- Halten Sie den Parameter `Volume` des Instruments an das Risikomodell angepasst, da die Strategie bei Bedarf immer `Volume + |Position|` Lots öffnet, um die Nettoposition umzukehren.

## Unterschiede zur MQL4-Version
- Die Klassifikatordatensätze werden nur gelesen; Das Original EA schreibt während der Deinitialisierung neue Samples. Hier muss der Benutzer die Dateien nach der Analyse der Handelshistorie manuell aktualisieren.
- Alle Schutzanordnungen werden über StockSharp `StartProtection` anstelle manueller `OrderSend`-Parameter angehängt.
- Beim Schließen einer Order im Klassifizierungsmodus wird immer die gesamte Position geschlossen, wenn `ClosePositionsOnSignal` aktiviert ist, während das Skript MQL4 nur profitable Orders schloss, bevor neue Signale angenommen wurden.
