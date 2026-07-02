# Für Max V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Für Max V2 handelt es sich um eine Portierung des MetaTrader 4 Expert Advisors `for_max_v2.mq4`. Die Strategie wartet auf spezifische Zwei-Kerzen-Verschlingungsmuster und platziert dann ein symmetrisches Paar von Kauf-Stopp- und Verkaufs-Stopp-Aufträgen um die jüngste Kerze herum. Sobald eine Breakout-Order ausgeführt wird, wird die entgegengesetzte Pending-Order entfernt und die Position wird mit festen Stopps, optionalen Take-Profit-Levels und einer Trailing-Routine verwaltet, die zunächst einen kleinen Gewinn bei Break-Even sichert und dann dem Preis folgt.

## Strategielogik
### Erkennung von Verschlingungsmustern
Der ursprüngliche Fachberater legt zwei Einstiegsblöcke offen und beide bleiben erhalten:
* **Setup Typ 1** – scannt die vorherigen `Max Search`-Kerzen (überspringt den aktuellen Balken) und wartet darauf, dass das niedrigste Tief innerhalb dieses Bereichs vor zwei Balken auftritt **oder** darauf, dass das höchste Hoch vor zwei Balken auftritt. Wenn das passiert, muss die zwei Balken zurückliegende Kerze die vorherige Kerze (höheres Hoch und niedrigeres Tief) überdecken. Das Setup stellt einen Straddle um die zuletzt fertige Kerze herum her.
* **Setup vom Typ 2** – scannt auch die vorherigen `Max Search`-Kerzen, sucht aber nach dem Extrem, das einen Balken zurückliegt. Darüber hinaus muss die Kerze vor einem Balken die Kerze vor zwei Balken umhüllen. Anschließend wird ein Straddle um die jüngste Kerze gelegt. Beide Setups können koexistieren; Jeder verwaltet seine eigenen ausstehenden Aufträge und seine Ablaufuhr.

### Ausstehende Auftragserteilung
* **Einstiegspreise** – Kauf-Stopp-Aufträge werden beim vorherigen Kerzenhoch plus `Gap Points` platziert, Verkaufsstopp-Aufträge beim vorherigen Kerzentief minus `Gap Points`.
* **Stop-Loss** – für Typ 1 ist der Long-Stop auf dem Tiefpunkt der Kerze zwei Balken zurück (abzüglich der Lücke) und der Short-Stop auf dem Hoch dieser Kerze (plus der Lücke) verankert. Typ 2 verwendet die vorherige Kerze für beide Seiten.
* **Take-Profit** – optional. Long-Ziele addieren `Gap Points + Buy Take Profit Points` zum vorherigen Hoch und Short-Ziele subtrahieren `Gap Points + Sell Take Profit Points` vom vorherigen Tief. Wenn Sie die Take-Profit-Eingaben auf `0` setzen, werden die entsprechenden Ziele deaktiviert.
* **Ablauf** – jeder Straddle trägt einen Gültigkeitszeitstempel, der als `Order Expiry (bars)` multipliziert mit dem konfigurierten Kerzenzeitrahmen berechnet wird. Wenn die ausstehenden Bestellungen bei Erreichen des Zeitstempels noch funktionieren, werden beide Seiten storniert.

### Positionsmanagement
* Sobald ein Buy-Stop erfüllt ist, werden alle verbleibenden Sell-Stop-Orders aus beiden Setups storniert; Nach einem kurzen Einstieg gilt die Symmetrieregel.
* Stopps und Ziele werden bei abgeschlossenen Kerzen überwacht. Wenn das Tief einer Kerze den Long-Stop erreicht (oder das Hoch den Short-Stop erreicht), wird die Position mit einer Marktorder geschlossen. Der gleiche Ansatz wird für die Take-Profit-Ebenen verwendet.
* Die Break-Even-Routine (`Break-even Trigger` und `Break-even Offset`) verschiebt den Stop auf den Einstiegspreis plus/minus dem konfigurierten Offset, sobald die Position um den Triggerbetrag steigt.
* Der Trailing-Block hält den Stop `Long/Short Trailing Buffer` Punkte von der besten Exkursion entfernt, aber erst, nachdem der Preis weit genug gestiegen ist (und optional erst, nachdem der Handel bereits profitabel ist). `Trailing Step` verhindert zu häufige Anpassungen, indem eine Mindestverbesserung erforderlich ist, bevor der Anschlag wieder festgezogen wird.

## Parameter
* **Volumen** – Ordervolumen für jede ausstehende Stop-Order.
* **Buy Take Profit (Punkte)** – Distanz in Punkten, die zur Berechnung des Long Take Profit verwendet wird (zum Deaktivieren auf `0` setzen).
* **Verkaufs-Take-Profit (Punkte)** – Distanz in Punkten, die zur Berechnung des Short-Take-Profits verwendet wird (zum Deaktivieren auf `0` setzen).
* **Gap (Punkte)** – Puffer, der den Hochs/Tiefs hinzugefügt wird, bevor Stop-Einträge platziert werden, und in die Take-Profit-Distanz gefaltet wird.
* **Suchtiefe** – Anzahl der fertigen Kerzen, die bei der Überprüfung auf Engulfing-Setups vom Typ 1 und Typ 2 gescannt wurden.
* **Auftragsablauf (Balken)** – Anzahl der Kerzenlängen, die ein ausstehender Straddle aktiv bleibt, bevor beide Seiten storniert werden.
* **Break-Even-Trigger (Punkte)** – Gewinnschwelle, die die Break-Even-Stopp-Anpassung aktiviert.
* **Break-Even-Offset (Punkte)** – zusätzlicher Puffer, der dem Einstiegspreis hinzugefügt wird, wenn der Break-Even-Stopp platziert wird.
* **Long Trailing Buffer (Punkte)** – Nachlaufdistanz für Long-Positionen, sobald die Gewinnschwelle erreicht ist.
* **Short Trailing Buffer (Punkte)** – Nachlaufdistanz für Short-Positionen, sobald die Gewinnschwelle erreicht ist.
* **Trailing Step (Punkte)** – minimale Verbesserung der Stoppposition erforderlich, bevor der Trailing Stop erneut aktualisiert wird.
* **Trail Only After Profit** – wenn aktiviert, wartet das Trailing, bis sich die Position über den Puffer hinaus bewegt hat, bevor es aktiviert wird.
* **Kerzentyp** – Zeitrahmen der Kerzen, die zur Mustererkennung, Auftragsablauf und Ausstiegsverarbeitung verwendet werden.

## Zusätzliche Hinweise
* In „Punkten“ ausgedrückte Preisversätze basieren auf dem `PriceStep` des Wertpapiers. Symbole mit fünf (oder drei) Dezimalstellen werden automatisch in gebrochene Pip-Größen umgewandelt, genau wie in MetaTrader.
* Stop-Losses und Take-Profits werden über Marktaufträge innerhalb der Strategie ausgeführt, um das Verhalten von EA bei der Verwaltung von Levels bei geschlossenen Kerzen widerzuspiegeln.
* Die Strategie implementiert nicht die ungenutzte `vhod_3`-Funktion aus der Originalquelle; Es wurden nur die beiden aktiven Eintrittsblöcke portiert.
* Dieses Paket enthält nur die C#-Implementierung; Es wird keine Python-Version bereitgestellt.
