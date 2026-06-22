# Ilan14-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ilan14 ist eine Hedging-Grid-Strategie, die gleichzeitig Long- und Short-Positionen eröffnet. Wenn sich der Markt um eine benutzerdefinierte Pip-Distanz gegen eine Seite bewegt, fügt die Strategie eine neue Order in diese Richtung hinzu, deren Volumen mit dem **Lot Exponent** multipliziert wird. Der Durchschnittspreis der Position wird verfolgt, und sobald der Preis um die konfigurierte **Take Profit**-Distanz zurückkehrt, werden alle Orders dieser Seite geschlossen.

Parameter:
- **Pip Step** – Abstand in Pips zwischen Grid-Orders.
- **Lot Exponent** – Multiplikator für das Volumen jeder zusätzlichen Order.
- **Max Trades** – maximale Anzahl von Orders pro Richtung.
- **Take Profit** – Gewinnziel in Pips vom gewichteten Durchschnittspreis.
- **Initial Volume** – Volumen der ersten Order.
- **Candle Type** – Zeitrahmen für die Kerzensubskription.

Die Implementierung verwendet die High-Level-StockSharp-API mit Kerzensubskriptionen und vermeidet manuelle Datensammlungen. Beide Seiten des Grids werden unabhängig verwaltet, sodass die Strategie Rebounds nach ungünstigen Bewegungen nutzen kann.
