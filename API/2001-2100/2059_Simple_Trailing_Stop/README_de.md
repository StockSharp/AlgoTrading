# Einfache Trailing-Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel zeigt, wie man eine offene Position mit einem Trailing Stop mithilfe der High-Level-API von StockSharp verwaltet.

## Überblick
- Eröffnet eine einzelne Long-Position nach Erhalt der ersten abgeschlossenen Kerze.
- Aktiviert den Positionsschutz mit einem Trailing Stop.
- Der Stop-Preis folgt dem aktuellen Preis in einem festen Abstand.

## Parameter
- `TrailPoints` – Abstand in Kurseinheiten, der für den Trailing Stop verwendet wird.
- `CandleType` – Art der von der Strategie verarbeiteten Kerzen.

## Logik
1. Beim Start abonniert die Strategie Kerzen und aktiviert `StartProtection` mit Trailing.
2. Nach der ersten abgeschlossenen Kerze kauft die Strategie zum Marktpreis.
3. Wenn sich der Preis zugunsten der Position bewegt, wird das Stop-Level verschoben, um den durch `TrailPoints` definierten Abstand beizubehalten.
4. Wenn der Preis umkehrt und den Trailing Stop erreicht, wird die Position automatisch geschlossen.

Die Strategie ist vereinfacht und soll die grundlegende Verwendung des Trailing Stops zeigen.
