# Schwerpunkt-Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie baut den vom ursprünglichen MQL4-Experten verwendeten Center of Gravity-Kanal neu auf, indem sie eine polynomiale Regression für die neuesten Kerzen löst. Das Regressionszentrum wird aus dem Schnittpunkt der Anpassung der kleinsten Quadrate berechnet, während die Bandbreite aus der Standardabweichung der Schlusskurse über denselben Lookback-Horizont abgeleitet wird. Das untere Band entspricht dem Regressionszentrum abzüglich der skalierten Abweichung und reproduziert den `stdl`-Puffer, auf den im Quellroboter zugegriffen wurde.

Während der Live-Verarbeitung unterhält der Algorithmus eine fortlaufende Warteschlange von Abschlüssen mit der durch den Parameter **Bars Back** definierten Länge. Jede fertige Kerze löst eine Neuberechnung der Regressionskoeffizienten durch Gaußsche Eliminierung auf dem normalen Gleichungssystem aus. Dadurch wird das Speichern vollständiger Kerzenverläufe vermieden, es ergibt sich jedoch die gleiche Kanalgeometrie wie beim benutzerdefinierten Indikator. Wenn die Matrix schlecht konditioniert wird, wird die Aktualisierung übersprungen, wodurch instabile Handelsentscheidungen verhindert werden.

Die Handelslogik spiegelt den ursprünglichen Experten wider: Wenn das aktuelle Kerzentief über dem unteren Abweichungsband (`lowerBand < Low` in der MQL-Notation) bleibt, betrachtet die Strategie dies als einen Absprung zur Mean-Reversion. Wenn keine Long-Position offen ist, wird ein etwaiges Short-Engagement geschlossen und eine Market-Buy-Order mit dem Strategievolumen erteilt. Die neuesten unteren, oberen und mittleren Werte werden über schreibgeschützte Eigenschaften für Diagramme oder Diagnosen angezeigt.

Das Risikomanagement ist optional. **Stop-Loss-Distanz** und **Take-Profit-Distanz** werden in absoluten Preiseinheiten angegeben. Wenn der Wert ungleich Null ist, zeichnet die Strategie den Einstiegspreis der aktiven Long-Position auf und prüft die Extremwerte der Kerze, um festzustellen, ob ein Stop- oder Gewinnziel erreicht wurde. Wenn keiner der Parameter angegeben ist, kann die Position manuell oder durch externe Module verwaltet werden.

### Parameter
- **Kerzentyp** – Zeitrahmen des Kerzenabonnements, das die Regression speist.
- **Bars Back** – Anzahl der historischen Balken, die zur Berechnung des Regressionskanals verwendet werden (Standard 125).
- **Polynomgrad** – Grad der Polynomregression (Standard 2), die die Kanalkrümmung bestimmt.
- **Std-Multiplikator** – Multiplikator, der bei der Bildung des Umschlags auf die Standardabweichung angewendet wird (Standard 1).
- **Stop-Loss-Distanz** – optionaler Long-Stop-Loss-Offset in Preiseinheiten (Standard 0 deaktiviert ihn).
- **Take-Profit-Abstand** – optionaler Long-Take-Profit-Offset in Preiseinheiten (Standardeinstellung 0 deaktiviert ihn).

Die Strategie funktioniert nur bei abgeschlossenen Kerzen, verwendet Marktaufträge für Einstiege und führt keine automatischen Leerverkäufe durch, da der Verkaufszweig des ursprünglichen Experten auskommentiert wurde.
