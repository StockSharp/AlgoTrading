# Beschreibung der SimpleHighBreak-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Strategieübersicht

Die „SimpleHighBreak"-Strategie ist darauf ausgelegt, von Kursausbrüchen über ein vordefiniertes Hoch im [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) zu profitieren. Die Strategie konzentriert sich auf die Identifizierung von Chancen, bei denen der Kurs über das 15-Perioden-Hoch ausbricht und damit eine mögliche Fortsetzung des Aufwärtstrends signalisiert.

![schema](schema.png)

## Strategiedetails

### Komponenten

- **Kerzenbildung**: Nutzt einen 5-Minuten-Zeitrahmen zur Erzeugung von [Kerzen](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) und überwacht den Markt auf signifikante Kursbewegungen.
- **Hoch-Indikator**: Berechnet den [höchsten Kurs](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) über die letzten 15 Perioden, um Ausbruchsniveaus zu bestimmen.
- **Ausbruchserkennung**: Die Strategie löst eine Kauforder aus, wenn der aktuelle Kurs [über](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) das jüngste 15-Perioden-Hoch ausbricht.

### Trade-Ausführung

- **Ordertyp**: Markt-[Order](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html).
- **Einstieg**: Eine Kauforder wird platziert, wenn der Kurs das 15-Perioden-Hoch überschreitet.
- **Ausstiegsstrategie**: Die Position wird auf Basis spezifischer Bedingungen, wie einem festgelegten Zeitrahmen oder einem Umkehrmuster, geschlossen, die von der Strategie dynamisch verwaltet werden.

### Risikomanagement

- **Positionsgröße**: Passt die Positionsgröße anhand vordefinierter Risikomanagementregeln und der aktuellen Marktvolatilität an.
- **Stop Loss und Take Profit**: Konfigurierbare [Stop Loss- und Take Profit](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)-Niveaus werden unmittelbar nach dem Einstieg gesetzt, um das Risiko zu steuern und Gewinne zu sichern.

## Implementierungsdetails

- **Plattform**: Implementiert auf der StockSharp-Plattform unter Nutzung ihrer umfangreichen Funktionen für Echtzeit-Datenverarbeitung und automatisiertes Ordermanagement.
- **Indikatoren**: Verwendet primär den Höchstkurs-Indikator über eine festgelegte Anzahl von Perioden zur Bestimmung von Einstiegspunkten.

## Fazit

Die „SimpleHighBreak"-Strategie bietet einen unkomplizierten, aber effektiven Ansatz für den Handel mit Kursausbrüchen, ideal für Trader, die Chancen in volatilen Märkten suchen. Sie kombiniert technische Indikatoren mit einem detaillierten Risikomanagement, um potenzielle Renditen zu maximieren und gleichzeitig Risiken zu minimieren.
