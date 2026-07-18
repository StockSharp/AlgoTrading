# Sie Kanskigor tägliche Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
She Kanskigor Daily Strategy ist ein einmal pro Tag stattfindendes Breakout-System, das den ursprünglichen MetaTrader-Expertenberater `SHE_kanskigor.mq4` widerspiegelt. Die Strategie wertet die Richtung der vorherigen Tageskerze aus und eröffnet innerhalb eines engen Zeitfensters zu Beginn des neuen Handelstages eine einzelne Marktposition. Es überwacht automatisch die Position und schließt sie anhand einer konfigurierbaren Take-Profit- oder Stop-Loss-Distanz, ausgedrückt in Wertpapierpreisschritten.

## Handelslogik
1. Abonnieren Sie sowohl Intraday-Kerzen (Standard: 1 Minute) als auch Tageskerzen für das ausgewählte Wertpapier.
2. Aktualisieren Sie die gespeicherten täglichen Öffnungs- und Schlusswerte, sobald eine fertige Tageskerze eintrifft.
3. Bei jeder fertigen Intraday-Kerze:
   - Setzen Sie die Markierung „Heute gehandelt“ zurück, wenn ein neues Kalenderdatum beginnt.
   - Verwalten Sie die aktive Position, indem Sie prüfen, ob der Schlusskurs die Stop-Loss- oder Take-Profit-Schwellenwerte erreicht.
   - Überprüfen Sie, ob die aktuelle Zeit innerhalb des konfigurierten Handelsfensters liegt (Standardstart: 00:05, Fensterlänge: 5 Minuten).
   - Wenn heute noch keine Position eröffnet wurde und eine gültige vorherige Tageskerze verfügbar ist:
     - Gehen Sie long, wenn der vorherige tägliche Eröffnungskurs höher ist als der Schlusskurs (bärische Kerze).
     - Gehen Sie short, wenn der vorherige tägliche Eröffnungskurs niedriger ist als der Schlusskurs (bullische Kerze).
   - Überspringen Sie den Handel, wenn der Vortag unverändert geschlossen wurde.
4. Die Strategie führt Schutzausstiege mithilfe von Marktaufträgen durch, sobald der Schlusskurs die konfigurierten Schwellenwerte erreicht.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| **Volumen** | Auftragsvolumen, das für Einträge verwendet wird. | `0.1` |
| **Gewinn mitnehmen** | Gewinnziel ausgedrückt in Preisschritten. Ein Wert von `0` deaktiviert das Ziel. | `35` |
| **Stop-Loss** | Verlustschwelle ausgedrückt in Preisschritten. Ein Wert von `0` deaktiviert den Stopp. | `55` |
| **Startzeit** | Uhrzeit (Börsenzeitzone), zu der das Eingabefenster beginnt. | `00:05` |
| **Fenster (min.)** | Dauer des Eingabefensters in Minuten. | `5` |
| **Intraday-Kerze** | Kerzendatentyp, der für die Intraday-Verarbeitung verwendet wird (Standard: 1-Minuten-Kerzen). | `TimeFrameCandleMessage(1m)` |

## Notizen
- Die Strategie erlaubt nur einen Eintrag pro Handelstag.
- Tägliche Kerzendaten müssen verfügbar sein; andernfalls wartet die Strategie, bis eine fertige Kerze eintrifft.
- Schutzausgänge basieren auf dem Schlusskurs fertiger Intraday-Kerzen.
- Der Code verwendet StockSharp auf hoher Ebene API (`SubscribeCandles` mit `Bind`) und entspricht den Kodierungsstandards des Projekts (Tabulatoren, englische Kommentare und Parametermetadaten).
