# BrakeExp-Kanal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des **BrakeExp**-Indikators, der einen exponentiellen Kanal um Kursbewegungen aufbaut. Der Indikator wechselt zwischen Long- und Short-Regimen und erzeugt Kauf- oder Verkaufssignale, wenn der Preis die dynamischen Kanalgrenzen kreuzt.

## Funktionsweise

- Der Indikator pflegt eine Exponentialkurve, die dem Preis folgt.
- Wenn die Kurve unter dem Preis liegt (Aufwärtstrend), sucht die Strategie nach Kaufsignalen.
- Wenn die Kurve über dem Preis liegt (Abwärtstrend), sucht die Strategie nach Verkaufssignalen.
- Ein Wechsel von einer Seite zur anderen erzeugt ein Einstiegssignal in die neue Richtung und schließt die entgegengesetzte Position.

## Parameter

- `Candle Type` – Zeitrahmen der verarbeiteten Kerzen.
- `Volume` – Ordervolumen für Markteintritte.
- `A`, `B` – Parameter, die die Form der BrakeExp-Kurve definieren.
- `Buy Open` / `Sell Open` – Erlaubnis zum Öffnen von Long- oder Short-Positionen.
- `Buy Close` / `Sell Close` – Erlaubnis zum Schließen von Short- oder Long-Positionen.

## Hinweise

Diese Implementierung konzentriert sich auf die Kernlogik des BrakeExp-Indikators und enthält kein Stop-Loss- oder Take-Profit-Management. Zusätzliche Risikokontrollen können bei Bedarf hinzugefügt werden.
