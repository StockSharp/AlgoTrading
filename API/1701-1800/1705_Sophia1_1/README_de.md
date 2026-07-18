# Sophia 1_1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sophia 1_1 ist eine Grid-basierte Martingale-Handelsstrategie.
Die Strategie eröffnet eine Position nach vier aufeinanderfolgenden Kerzen in dieselbe Richtung:
- Vier steigende Kerzen lösen einen Short-Einstieg aus.
- Vier fallende Kerzen lösen einen Long-Einstieg aus.

Sobald der Markt betreten wurde, fügt der Algorithmus jedes Mal Positionen hinzu, wenn sich der Kurs um eine feste Anzahl von Kursschritten (`Pip Step`) gegen die aktuelle Position bewegt.
Das Volumen jedes zusätzlichen Trades wird mit `Lot Exponent` multipliziert, wodurch ein klassisches Martingale-Grid entsteht.

Das Risikomanagement erfolgt durch `Take Profit`, `Stop Loss` und einen optionalen Trailing-Stop.
Der Trailing-Mechanismus startet, sobald der Gewinn `Trail Start` erreicht, und verfolgt das Stop-Level um `Trail Stop` Kursschritte.

## Parameter
- **Volume** – Basisvolumen für den ersten Trade.
- **Pip Step** – Distanz in Kursschritten vor dem Hinzufügen einer neuen Position.
- **Lot Exponent** – Multiplikator für das Volumen jedes zusätzlichen Trades.
- **Max Trades** – maximale Anzahl von Positionen im Grid.
- **Take Profit** – Gewinnziel in Kursschritten vom durchschnittlichen Einstiegspreis.
- **Stop Loss** – Verlustschwelle in Kursschritten vom durchschnittlichen Einstiegspreis.
- **Use Trailing** – Trailing-Stop aktivieren oder deaktivieren.
- **Trail Start** – erforderlicher Gewinn, bevor der Trailing-Stop aktiv wird.
- **Trail Stop** – Distanz des Trailing-Stops in Kursschritten.
- **Candle Type** – Zeitrahmen der Kerzen für Berechnungen.
