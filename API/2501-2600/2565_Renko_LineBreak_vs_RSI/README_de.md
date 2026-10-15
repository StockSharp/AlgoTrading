# Renko Line Break vs RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert den MetaTrader-Expertenberater "RenkoLineBreak vs RSI" mit der StockSharp-High-Level-API. Sie kombiniert Renko-Trenderkennung mit einem RSI-Rücksetzerfilter und steigt zum Marktpreis ein, sobald eine Drei-Kerzen-Preisstruktur das Setup bestätigt. Die Renko-Ziegel werden in der Strategie selbst aus den Schlusskursen der Zeitkerzen berechnet, sodass ein einziges Kerzen-Abonnement alles antreibt.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Renko-Trend bleibt bullisch und der RSI fällt auf `50 - RsiShift` oder darunter. Das Setup wird gegen ein Referenzniveau aus dem Hoch der Kerze von drei Balken zuvor plus `IndentFromHighLow` geprüft, und beim Schluss der Signalkerze wird eine Kauf-Market-Order gesendet.
  - **Short**: Der Renko-Trend bleibt bärisch und der RSI steigt auf `50 + RsiShift` oder darüber. Das Setup wird gegen ein Referenzniveau aus dem Tief der Kerze von drei Balken zuvor minus `IndentFromHighLow` geprüft, und beim Schluss der Signalkerze wird eine Verkauf-Market-Order gesendet.
  - Solange der Renko-Trend in einem Übergangszustand steht (`ToUp` / `ToDown`), wird kein neuer Einstieg vorgenommen; das gespeicherte Setup wird verworfen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Marktausstiege, wenn der entgegengesetzte Renko-Übergang erscheint (`ToDown` für Longs, `ToUp` für Shorts).
  - RSI kreuzt zurück durch den Mittelpunkt (`50 ± RsiShift`).
  - Kerzenbereiche, die die geplanten Stop-Loss- oder Take-Profit-Level erreichen.
- **Stops**:
  - Der Stop-Loss ist am Extrempunkt der letzten drei Kerzen plus `IndentFromHighLow` verankert.
  - Take-Profit liegt `TakeProfit` Preiseinheiten vom Referenz-Ausbruchsniveau entfernt (optional wenn auf null gesetzt).
- **Standardwerte**:
  - `BoxSize` = 100m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 10m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = 2-Stunden-Zeitrahmen.
- **Filter**:
  - Kategorie: Trendfolge.
  - Richtung: Beide.
  - Indikatoren: Renko, RSI.
  - Stops: Harter Stop & Take Profit.
  - Komplexität: Mittel.
  - Zeitrahmen: Ein einziger Zeitrahmen (Renko-Ziegel aus den Kerzenschlusskursen abgeleitet).
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Moderat.

## Funktionsweise

1. Die Renko-Ziegel werden innerhalb der Strategie aus den Schlusskursen der Zeitkerzen gebildet: Ein Ziegel, der die aktuelle Richtung fortsetzt, entsteht, sobald sich der Schlusskurs um eine volle `BoxSize` vom aktuellen Ankerpreis entfernt, während ein Ziegel, der die Richtung umkehrt, zwei `BoxSize` benötigt. Bevor der erste Ziegel eine Richtung festlegt, genügt eine Box in beide Richtungen. Es entstehen so viele Ziegel, wie die Bewegung abdeckt, und der Anker wandert mit. Wenn ein Ziegel die Richtung wechselt, wird der Trendzustand für einen Schritt auf `ToUp` oder `ToDown` gesetzt, um das ursprüngliche Indikatorverhalten nachzuahmen.
2. Derselbe Kerzenstrom speist den RSI-Indikator und liefert die letzten drei Hochs/Tiefs, die für Ausbruchniveaus verwendet werden, sodass die Strategie genau ein Marktdaten-Abonnement öffnet.
3. Wenn sowohl Renko-Trend als auch RSI-Bedingungen übereinstimmen, sendet die Strategie eine Market-Order (Kauf oder Verkauf). Geplante Stop-Loss- und Take-Profit-Level werden gespeichert und überwacht, sobald die Position offen ist.
4. Sobald die Position offen ist, werden die gespeicherten Schutzlevel aktiv. Nachfolgende Kerzen prüfen, ob der Preis den Stop- oder Zielbereich erreicht; wenn ja, wird die Position zum Markt geschlossen.
5. Wenn die Dynamik nachlässt (RSI kreuzt zurück durch den Mittelpunkt) oder der Renko-Trend sich ändert, wird die Position frühzeitig geschlossen.

## Verwendete Indikatoren

- **Renko-Ziegel**, die mit der Schrittweite `BoxSize` aus den Schlusskursen der Zeitkerzen abgeleitet werden, zur Ableitung des Richtungsbiases und Erkennung von Übergängen zwischen Auf- und Abwärtszuständen.
- **Relative Strength Index (RSI)** zur Qualifizierung von Einstiegen durch Forderung von Rücksetzern gegen den Trend.

## Zusätzliche Hinweise

- `IndentFromHighLow` modelliert den Puffer des ursprünglichen Expertenberaters, der das Referenz-Ausbruchsniveau und den Stop-Loss von aktuellen Hochs und Tiefs fernhält.
- `TakeProfit` kann auf null gesetzt werden, um das Gewinnziel zu deaktivieren, während die Stop-Loss-Logik intakt bleibt.
- Die Strategie hält jeweils nur eine Position: Ein neuer Einstieg kommt nur infrage, solange keine Position offen ist, und das gespeicherte Setup wird verworfen, sobald die Marktbedingungen es ungültig machen.
