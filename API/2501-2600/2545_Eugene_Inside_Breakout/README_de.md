# Eugene Inside-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Eugene Inside-Ausbruch-Strategie ist eine direkte Portierung des ursprünglichen MetaTrader-Expertenberaters von barabashkakvn. Sie konzentriert sich auf reine Price-Action: eine Inside-Kerzen-Sequenz gefolgt von einem Bereichsausbruch. Bestätigungsniveaus aus dem Körper der vorherigen Kerze stellen sicher, dass der Ausbruch Momentum entwickelt, bevor die Strategie eine Position eingeht.

## Übersicht

Die Strategie überwacht auf ein frisches Hoch oder Tief relativ zur vorherigen Kerze. Long-Setups erfordern, dass die vorherige Kerze ein Tief unterhalb des Hochs der Kerze davor hat, was die Kompression vor dem Ausbruch betont. Short-Setups lehnen den Handel ab, wenn die vorherige Kerze eine Inside-Bar ist, und spiegeln die Sicherheitsvorkehrungen in der Quell-MQL-Logik wider. Orders werden immer zum Markt mit einem festen Volumen ausgeführt.

## Marktlogik

- Betont Ausbrüche des jüngsten Hochs/Tiefs, um Richtungsbewegungen früh zu erfassen.
- Verwendet den Körper der vorherigen Kerze zur Berechnung von zwei Ein-Drittel-Retracement-Niveaus (`zigLevelBuy` und `zigLevelSell`). Der Preis muss diese Niveaus berühren, oder die Sitzung muss über die konfigurierte Aktivierungsstunde hinaus sein, bevor ein Einstieg erlaubt wird.
- Verhindert neue Positionen, wenn ein Ausbruch mit einer Inside-Kerze gegen die Trade-Richtung zusammenfällt.
- Schließt offene Positionen, sobald das entgegengesetzte Ausbruchssignal bestätigt wird, und stellt sicher, dass die Strategie immer flach oder am letzten Signal ausgerichtet ist.

## Einstiegsregeln

### Long

1. Das aktuelle Kerzenhoch ist höher als das vorherige Kerzenhoch.
2. Bestätigung wird erhalten, wenn das aktuelle Tief das Ein-Drittel-Retracement des vorherigen Kerzenkörpers durchbricht, oder die aktuelle Stunde über dem Aktivierungsstunden-Parameter liegt.
3. Das aktuelle Tief muss über dem vorherigen Tief bleiben, während das vorherige Tief unter dem Hoch von zwei Kerzen zuvor liegt.
4. Keine bestehende Position ist offen.

### Short

1. Das aktuelle Kerzentief liegt unter dem vorherigen Kerzentief.
2. Bestätigung wird erhalten, wenn das aktuelle Hoch das obere Ein-Drittel-Retracement des vorherigen Kerzenkörpers testet, oder die aktuelle Stunde über dem Aktivierungsstunden-Parameter liegt.
3. Die vorherige Kerze darf keine Inside-Bar sein.
4. Das aktuelle Hoch muss unter dem vorherigen Hoch liegen.
5. Keine bestehende Position ist offen.

## Ausstiegsregeln

- Long-Positionen schließen, wenn ein validierter Short-Ausbruch entsteht (Bedingungen 1–3 der Short-Einstiegslogik).
- Short-Positionen schließen, wenn ein validierter Long-Ausbruch entsteht (Bedingungen 1–3 der Long-Einstiegslogik).

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Zeitrahmen der von der Strategie verarbeiteten Kerzen. | 1-Stunden-Kerzen |
| `Volume` | Ordergröße, die mit jeder Marktorder gesendet wird. | 0.1 |
| `ActivationHour` | Tagesstunde, nach der Bestätigungen automatisch akzeptiert werden, und der `TimeCurrent()`-Filter aus dem MQL-Code repliziert wird. | 8 |

## Hinweise

- Die Bestätigungsprüfungen, die im Originalskript als "white bird" und "black bird" bezeichnet werden, werten aufgrund der Quellbedingungen immer als falsch aus; sie werden für Parität erhalten, beeinflussen aber keine Handelsentscheidungen.
- Keine zusätzlichen Indikatoren oder Trailing-Stops werden verwendet—der Ansatz ist rein preisbasiert und kehrt Positionen bei jedem entgegengesetzten Ausbruch um.
