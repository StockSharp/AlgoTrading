# Fibo Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Fibo Stop-Strategie bewegt den Schutz-Stop entlang der Fibonacci-Retracement-Niveaus, die durch zwei Preise definiert werden: Start und Ende. Die Strategie eröffnet eine Position in der Richtung vom Startniveau zum Endniveau und verschiebt den Stop auf jedes neue Fibonacci-Niveau, sobald der Preis es kreuzt.

## Algorithmus
1. Richtung vom Startpreis zum Endpreis bestimmen. Wenn das Ende höher als der Start ist, wird eine Long-Position eröffnet; andernfalls eine Short-Position.
2. Fibonacci-Niveaus berechnen: 0%, 23.6%, 38.6%, 50%, 61.8%, 78.6%, 100%, 127% basierend auf dem Bereich.
3. Der anfängliche Stop wird hinter dem Startniveau platziert, unter Verwendung des angegebenen Versatzes in Preisschritten.
4. Wenn der Marktpreis sich bewegt und das nächste Fibonacci-Niveau kreuzt, wird der Stop auf dieses Niveau minus/plus dem Versatz verschoben.
5. Die Position wird geschlossen, wenn der Preis den Trailing-Stop erreicht.

## Parameter
- `FiboStart` – Basispreis, bei dem die Fibonacci-Berechnung beginnt.
- `FiboEnd` – Endpreis, der den Fibonacci-Bereich definiert.
- `OffsetPoints` – Anzahl der Preisschritte, die hinter jedem Fibonacci-Niveau für den Stop hinzugefügt werden.
- `CandleType` – Kerzenserie zur Preisüberwachung.

## Hinweise
Die Strategie verwendet nur abgeschlossene Kerzen und verlässt sich nicht auf den Indikatorwertverlauf. Sie ist als Beispiel für die Verwaltung eines Trailing-Stops mit der High-Level-API von StockSharp gedacht.
