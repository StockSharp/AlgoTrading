# Explosion Range Expansion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Explosion Range Expansion Strategy ist ein Ausbruchssystem, das vom MetaTrader 5-Expertenberater "Explosion" konvertiert wurde. Der Algorithmus vergleicht die Spanne der aktuell abgeschlossenen Kerze mit der vorherigen Kerze und öffnet eine Marktposition in Richtung des Kerzenkörpers, wenn die Spannenexpansion ein konfigurierbares Verhältnis überschreitet. Die StockSharp-Version behält die ursprünglichen Money-Management-Funktionen bei und fügt praktische Parameter für Zeitplanung und Trailing-Stop-Verwaltung hinzu.

## Handelsregeln
- **Spannenexpansion:** Berechne die aktuelle Kerzenspanne (`High - Low`) und vergleiche sie mit der vorherigen Kerzenspanne. Wenn die aktuelle Spanne größer ist als die vorherige Spanne multipliziert mit `Range Ratio`, wird ein Signal erzeugt.
- **Richtungsfilter:**
  - Wenn die Kerze über ihrer Eröffnung schließt und die aktuelle Position flach oder short ist, wird eine Long-Marktorder gesendet.
  - Wenn die Kerze unter ihrer Eröffnung schließt und die aktuelle Position flach oder long ist, wird eine Short-Marktorder gesendet.
- **Handelsfenster:** Signale werden nur akzeptiert, wenn die Kerzenschlusszeit zwischen `Start Hour` und `End Hour` (einschließlich) liegt.
- **Tageslimit:** Wenn `One Trade Per Day` aktiviert ist, wird nur der erste qualifizierende Einstieg des Handelstages ausgeführt.
- **Pause zwischen Trades:** Nach einem Positionseinstieg wartet die Strategie `Pause (sec)` Sekunden, bevor ein neues Signal akzeptiert wird.
- **Maximales Exposure:** Die Nettopositionsgröße darf `Max Positions * Order Volume` nicht überschreiten.

## Ausstiege und Risikomanagement
- **Anfänglicher Schutz:** Optionale Stop-Loss- und Take-Profit-Niveaus werden in Preisschritten definiert und vom Einstiegspreis berechnet.
- **Trailing Stop:** Wenn aktiviert, wird der Stop-Loss nach Erreichen eines Mindestgewinnschwellenwerts (`Trailing Stop + Trailing Step`) näher an den Preis verschoben. Die Trailing-Logik behält dasselbe Verhalten wie im originalen EA bei.
- **Manuelles Schließen bei Zielen:** Wenn die Kerzenspanne intrabar entweder das Stop-Loss- oder Take-Profit-Niveau erreicht, wird die Position mit einer Marktorder geschlossen.

## Parameter
- `Candle Type` – Datentyp für das Kerzenabonnement.
- `Order Volume` – Größe jeder Position in Lots.
- `Range Ratio` – Multiplikator auf die vorherige Kerzenspanne zum Auslösen von Einstiegen.
- `Max Positions` – Maximale Anzahl gleichzeitig erlaubter Lots.
- `Pause (sec)` – Mindestzeit in Sekunden zwischen Einstiegen.
- `Start Hour` / `End Hour` – Handelsstunden-Filter (0–23).
- `One Trade Per Day` – Beschränkt die Strategie auf einen Einstieg pro Kalendertag.
- `Stop Loss` – Anfänglicher Stop-Loss-Abstand in Preisschritten.
- `Take Profit` – Anfänglicher Take-Profit-Abstand in Preisschritten.
- `Trailing Stop` – Trailing-Stop-Abstand in Preisschritten.
- `Trailing Step` – Zusätzlicher Abstand, der vor einem Trailing-Update erforderlich ist.

## Konvertierungshinweise
- Die Strategie verwendet die High-Level-`SubscribeCandles`- und `Bind`-API für indikatorfreie Signalverarbeitung.
- Trailing Stop, Handelsfenster, Pause und Tageslimit reproduzieren die ursprüngliche MQ5-Logik.
- Money Management wird über einen einzelnen Volumenparameter ausgedrückt; risikobasiertes Lot-Sizing aus dem Originalscript wird in dieser Version nicht unterstützt.
