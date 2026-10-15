# TCPivotLimit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt rund um klassische tägliche Pivot-Level. Die Pivot-Punkte werden aus dem Hoch-, Tief- und Schlusskurs des Vortages berechnet. Limit-Orders werden an ausgewählten Support- oder Resistance-Leveln platziert und Positionen werden mit vordefinierten Stop-Loss- und Take-Profit-Leveln verwaltet.

## Parameter
- **Volume** – Ordervolumen.
- **Target Variant** – wählt aus, welche Support-/Resistance-Level für Einstieg, Stop und Ziel verwendet werden:
  1. Einstieg bei S1/R1, Stop bei S2/R2, Ziel bei R1/S1.
  2. Einstieg bei S1/R1, Stop bei S2/R2, Ziel bei R2/S2.
  3. Einstieg bei S2/R2, Stop bei S3/R3, Ziel bei R1/S1.
  4. Einstieg bei S2/R2, Stop bei S3/R3, Ziel bei R2/S2.
  5. Einstieg bei S2/R2, Stop bei S3/R3, Ziel bei R3/S3.
- **Intraday Close** – alle offenen Positionen um 23:00 schließen.
- **Modify Stop Loss** – Stop-Loss auf das erste Ziellevel verschieben, sobald dieses erreicht wurde.

## Handelslogik
1. Zu Beginn jedes Tages berechnet die Strategie den Pivot sowie drei Resistance- und drei Support-Level aus den Daten des Vortages.
2. Wenn der Kurs das gewählte Support- oder Resistance-Level berührt, wird eine Limit-Order in der entgegengesetzten Richtung gesendet.
3. Die Position wird geschlossen, wenn das Stop-Loss- oder Take-Profit-Level erreicht wird. Eine optionale Stop-Loss-Anpassung kann das Risiko nach dem ersten Ziel verringern.
4. Wenn *Intraday Close* aktiviert ist, werden alle offenen Positionen am Ende der Handelssitzung geschlossen.
