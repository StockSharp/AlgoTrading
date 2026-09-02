# Renko Line Break vs RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie recreiert den MetaTrader-Expertenberater "RenkoLineBreak vs RSI" mit der StockSharp-High-Level-API. Sie kombiniert Renko-Trenderkennung mit einem RSI-Rücksetzerfilter und führt Trades über ausstehende Stop-Orders rund um eine Drei-Kerzen-Preisstruktur aus. Die Renko-Ziegel werden in der Strategie selbst aus den Schlusskursen der Zeitkerzen berechnet, sodass ein einziges Kerzen-Abonnement alles antreibt.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Renko-Trend bleibt bullisch und der RSI fällt auf `50 - RsiShift` oder darunter. Eine Kauf-Stop-Order wird beim Hoch der Kerze von drei Balken zuvor plus `IndentFromHighLow` platziert.
  - **Short**: Der Renko-Trend bleibt bärisch und der RSI steigt auf `50 + RsiShift` oder darüber. Eine Verkauf-Stop-Order wird beim Tief der Kerze von drei Balken zuvor minus `IndentFromHighLow` platziert.
  - Ausstehende Orders werden storniert, wenn der Renko-Trend die Richtung wechselt (`ToUp` / `ToDown`).
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Marktausstiege, wenn der entgegengesetzte Renko-Übergang erscheint (`ToDown` für Longs, `ToUp` für Shorts).
  - RSI kreuzt zurück durch den Mittelpunkt (`50 ± RsiShift`).
  - Kerzenbereiche, die die geplanten Stop-Loss- oder Take-Profit-Level erreichen.
- **Stops**:
  - Der Stop-Loss ist am Extrempunkt der letzten drei Kerzen plus `IndentFromHighLow` verankert.
  - Take-Profit liegt `TakeProfit` Preiseinheiten vom geplanten Einstieg entfernt (optional wenn auf null gesetzt).
- **Standardwerte**:
  - `BoxSize` = 500m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 20m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = 5-Minuten-Zeitrahmen.
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

1. Die Renko-Ziegel werden innerhalb der Strategie aus den Schlusskursen der Zeitkerzen gebildet: Sobald sich der Schlusskurs um eine volle `BoxSize` vom aktuellen Ankerpreis entfernt, entstehen ein oder mehrere Ziegel und der Anker wandert mit. Wenn ein Ziegel die Richtung wechselt, wird der Trendzustand für einen Schritt auf `ToUp` oder `ToDown` gesetzt, um das ursprüngliche Indikatorverhalten nachzuahmen.
2. Derselbe Kerzenstrom speist den RSI-Indikator und liefert die letzten drei Hochs/Tiefs, die für Ausbruchniveaus verwendet werden, sodass die Strategie genau ein Marktdaten-Abonnement öffnet.
3. Wenn sowohl Renko-Trend als auch RSI-Bedingungen übereinstimmen, registriert die Strategie eine Stop-Order (Kauf oder Verkauf). Geplante Stop-Loss- und Take-Profit-Level werden gespeichert und nach dem Auslösen der Order überwacht.
4. Nach der Orderausführung werden die gespeicherten Schutzlevel aktiv. Nachfolgende Kerzen prüfen, ob der Preis den Stop- oder Zielbereich erreicht; wenn ja, wird die Position zum Markt geschlossen.
5. Wenn die Dynamik nachlässt (RSI kreuzt zurück durch den Mittelpunkt) oder der Renko-Trend sich ändert, wird die Position frühzeitig geschlossen.

## Verwendete Indikatoren

- **Renko-Ziegel**, die mit der Schrittweite `BoxSize` aus den Schlusskursen der Zeitkerzen abgeleitet werden, zur Ableitung des Richtungsbiases und Erkennung von Übergängen zwischen Auf- und Abwärtszuständen.
- **Relative Strength Index (RSI)** zur Qualifizierung von Einstiegen durch Forderung von Rücksetzern gegen den Trend.

## Zusätzliche Hinweise

- `IndentFromHighLow` modelliert den Puffer des ursprünglichen Expertenberaters, der Einstiegs- und Stop-Orders von aktuellen Hochs und Tiefs fernhält.
- `TakeProfit` kann auf null gesetzt werden, um das Gewinnziel zu deaktivieren, während die Stop-Loss-Logik intakt bleibt.
- Die Strategie hält jeweils nur eine ausstehende Order und storniert sie automatisch, wenn die Marktbedingungen das Setup ungültig machen.
