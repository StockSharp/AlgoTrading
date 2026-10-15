# Einfache Nachrichten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert ausstehende Stop-Orders rund um einen festgelegten Nachrichtenzeitpunkt, um scharfe Bewegungen durch Nachrichtenveröffentlichungen zu erfassen.

## So funktioniert es

- Fünf Minuten vor `NewsTime` beginnt die Strategie, Paare aus Buy-Stop- und Sell-Stop-Orders einzureichen.
- Das erste Paar wird `Distance` Pips vom aktuellen Ask- und Bid-Preis entfernt platziert.
- Weitere Paare werden jeweils um `Delta` Pips vom vorherigen versetzt, insgesamt `Deals` Paare.
- Zehn Minuten nach der Nachrichtenveröffentlichung storniert die Strategie alle nicht ausgelösten Orders.
- Wenn eine Position eröffnet wird, überwacht die Strategie Stop-Loss-, Take-Profit- und Trailing-Stop-Levels. Wird ein Level erreicht, wird die Position geschlossen.

## Parameter

- `NewsTime` – Zeitpunkt der Nachrichtenveröffentlichung.
- `Deals` – Anzahl der Buy/Sell-Stop-Paare.
- `Delta` – Abstand zwischen Orders in Pips.
- `Distance` – Abstand vom aktuellen Preis für das erste Paar in Pips.
- `StopLoss` – anfänglicher Stop-Loss in Pips.
- `Trail` – Trailing Stop in Pips.
- `TakeProfit` – Take-Profit in Pips.
- `Volume` – Ordervolumen.

## Hinweise

Die Strategie stützt sich nicht auf Indikatoren und arbeitet ausschließlich mit Level-1-Daten. Sie dient Demonstrationszwecken und erfordert möglicherweise Anpassungen für den realen Handel.
