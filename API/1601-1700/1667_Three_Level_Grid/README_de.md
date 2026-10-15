# Drei-Ebenen-Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein symmetrisches Grid-Handelssystem mit bis zu drei Take-Profit-Rängen.
Limit-Orders werden oberhalb und unterhalb des aktuellen Preises in festen Abständen platziert. Wenn eine
Einstiegsorder ausgeführt wird, wird eine entgegengesetzte Limit-Order eingereicht, um Gewinn in einem
konfigurierbaren Abstand zu sichern. Die Methode eignet sich für seitwärts tendierende Märkte, in denen
der Preis innerhalb einer Bandbreite oszilliert.

## Parameter

- `Grid Size` – Abstand zwischen Grid-Levels.
- `Levels` – Anzahl der Grid-Levels auf jeder Seite des aktuellen Preises.
- `Base Take Profit` – Basisgewinnabstand für den ersten Rang.
- `Order Volume` – Volumen für jede Grid-Order.
- `Enable Rank1` – Orders mit Basis-Take-Profit platzieren.
- `Enable Rank2` – Orders mit Basis plus einem Grid-Size-Take-Profit platzieren.
- `Enable Rank3` – Orders mit Basis plus zwei Grid-Sizes Take-Profit platzieren.
- `Allow Longs` – die Long-Seite des Grids aktivieren.
- `Allow Shorts` – die Short-Seite des Grids aktivieren.
- `Candle Type` – Kerzentyp zum Ermitteln des anfänglichen Referenzpreises.

## Handelslogik

1. Beim Start abonniert die Strategie Kerzen und wartet auf die erste abgeschlossene Kerze.
2. Anhand des Schlusskurses dieser Kerze wird das Grid mit der konfigurierten Anzahl von Levels aufgebaut.
3. Für jedes Level werden Kauf- und/oder Verkaufs-Limit-Orders je nach erlaubten Seiten platziert.
4. Wenn eine Einstiegsorder ausgeführt wird, wird eine entgegengesetzte Limit-Order zum berechneten
   Take-Profit-Preis des gewählten Rangs registriert.
5. Orders verbleiben im Markt, bis sie ausgeführt oder manuell storniert werden.

Diese Implementierung ist eine vereinfachte Konvertierung des ursprünglichen MQL-Grid-Systems und soll
die Kernmechanik in StockSharp's High-Level-API hervorheben.
