# Frank Ud Minimalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel portiert den klassischen **Frank Ud** MetaTrader-Expertenberater nach StockSharp unter Verwendung der High-Level-Strategie API. Das ursprüngliche MQL-Skript führt ein abgesichertes Martingal-Gitter aus, das jedes Mal Positionen hinzufügt, wenn sich der Preis gegenüber dem letzten Eintrag bewegt. Gewinne werden gesperrt, sobald die letzte (und damit größte) Order eine festgelegte Anzahl an Pips erreicht. Danach werden *alle* Trades auf dieser Seite gleichzeitig geschlossen.

## Kernlogik

1. **Symmetrische Absicherung.** Die Strategie unterhält zwei unabhängige Leitern von Marktpositionen: eine lange Leiter und eine kurze Leiter. Daher ist es möglich, gleichzeitig Long- und Short-Positionen zu halten, wie im Absicherungsmodus von MetaTrader.
2. **Martingale-Progression.** Die erste Bestellung auf einer Seite verwendet `InitialVolume` (Standard 0,1 Lots). Jeder weitere Eintrag auf derselben Seite verdoppelt das größte derzeit offene Volumen. Jedes Lot, das die Strategie sendet – auch das allererste –, wird anschließend auf das begrenzt, was das Instrument tatsächlich annimmt: auf ein ganzzahliges Vielfaches von `VolumeStep` abgerundet, auf `MinVolume` angehoben, falls es darunter liegt, und bei `MaxVolume` gedeckelt. Einschränkungen, die das Instrument nicht meldet, werden übersprungen.
3. **Einstiegsabstand.** Eine neue Position wird nur hinzugefügt, wenn sich der Preis um mindestens `ReEntryPips` (Standard 41 Pips) über den besten Einstiegspreis der bestehenden Leiter hinaus bewegt hat. Die lange Leiter wartet darauf, dass der Briefkurs unter `lowest_buy - ReEntryPips` fällt, während die kurze Leiter darauf wartet, dass der Geldkurs über `highest_sell + ReEntryPips` steigt. Beide Seiten der Notierung stammen aus dem Schlusskurs derselben Kerze, sodass in dieser Portierung beide Vergleiche gegen denselben Preis erfolgen.
4. **Gewinnernte.** Für jede Leiter fungiert der Trade mit dem größten Volumen als „Trigger“-Order. Wenn sein Gewinn `TakeProfitPips` (Standard 65 Pips) übersteigt oder wenn der Preis das gepufferte Ziel erreicht, das `TakeProfitPips + ExtraTakeProfitPips` Pips von diesem Einstieg entfernt liegt, wird jede Position auf dieser Seite mit einer einzigen Marktorder abgeflacht und die Leiter geleert.
5. **Margin-Schutz.** Bevor ein neuer Eintrag eingereicht wird, überprüft die Strategie, ob die freie Marge des Portfolios – sein aktueller Wert abzüglich der gemeldeten Kommission – über `Balance × MinimumFreeMarginRatio` (Standard 0,5) bleibt. Die Absicherung gilt für beide Leitern und für jeden Eintrag darauf, den allerersten eingeschlossen. Ein Verhältnis von null schaltet sie ab; dasselbe geschieht, wenn das Portfolio überhaupt keinen Wert meldet – in beiden Fällen ist die Prüfung schlicht bestanden und die Strategie fällt auf das Festvolumenverhalten des ursprünglichen Experten zurück.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Pip-Gewinnschwelle, gemessen an der letzten, größten Bestellung. Bei Überschreitung werden alle Positionen auf dieser Seite geschlossen. |
| `ReEntryPips` | Mindest-Pip-Abstand zwischen dem besten vorhandenen Eintrag und dem aktuellen Geld-/Briefkurs, bevor eine neue Martingal-Order hinzugefügt wird. |
| `InitialVolume` | Basis-Lotgröße für die erste Order jeder Leiter. Folgeaufträge verdoppeln das größte aktive Volumen. |
| `MinimumFreeMarginRatio` | Erforderliches Verhältnis von freier Margin zu Guthaben, bevor neue Einträge zugelassen werden. Auf 0 setzen, um die Prüfung zu deaktivieren. Standard 0,5. |
| `ExtraTakeProfitPips` | Zusätzlicher Pip-Abstand, der bei der Berechnung des gepufferten Ausstiegsziels zu `TakeProfitPips` addiert wird. Standard 25. |
| `CandleType` | Kerzenserie, die die Strategie abonniert. Standard: Zeitrahmen von 1 Minute. |

## Hinweise zur Implementierung

- Ein Pip ist nicht die rohe Preisschrittweite. Bei der ersten verarbeiteten abgeschlossenen Kerze setzt die Strategie einen Pip auf ein Zehntausendstel des notierten Preises, begrenzt ihn nach unten durch die Preisschrittweite des Instruments (damit er nie feiner ist, als das Instrument tatsächlich handelt) und behält diesen Wert für den restlichen Lauf bei, damit sich das Gitter nicht unter sich selbst verschiebt. Damit wird die Forex-Konvention nachgebildet, für die der Experte geschrieben wurde (0,0001 bei EURUSD zu 1,10; 0,01 bei USDJPY zu 150), und die Abstände bleiben auch bei einem fünfstellig notierten Instrument sinnvoll, bei dem die rohe Schrittweite von 0,01 ein Ziel von 65 Pips bei nahezu jeder Kerze erreichen würde. Meldet das Instrument keine Preisschrittweite, wird der Pip allein durch diesen Bruchteil bestimmt.
- Die Strategie wird von abgeschlossenen Kerzen angetrieben, nicht von Notierungen der Stufe 1. Sie abonniert die `CandleType`-Serie (standardmäßig ein Zeitrahmen von 1 Minute) und ignoriert jede noch nicht abgeschlossene Kerze. Die mitgelieferte Historie enthält kein Orderbuch, daher dient der Schlusskurs der abgeschlossenen Kerze zugleich als Geld- und als Briefkurs. Die C#- und die Python-Implementierung abonnieren auf genau dieselbe Weise.
- Ein Leitereintrag wird in dem Moment festgehalten, in dem die Order gesendet wird, und nicht bei ihrer Ausführung: Beim Öffnen werden der Schlusskurs der Kerze und das angeforderte Volumen an die Liste angehängt, beim Schließen wird eine einzige Marktorder über das gesamte Volumen der Leiter gesendet und die Liste geleert. Es wird kein Wörterbuch mit Orderabsichten geführt und kein Ausführungs-Callback verwendet – in diesem Emulator wird die Ausführung synchron innerhalb der Orderregistrierung geliefert, noch bevor die Order überhaupt in ein solches Wörterbuch geschrieben werden könnte.
- Die Positionsbuchhaltung speichert jeden Leitereintrag (Preis und Volumen) in einfachen Listen, anstatt kumulative Statistiken abzufragen, und behält dabei das Verhalten der MQL-Arrays bei, die zum Auffinden des größten Loses und seines Einstiegspreises verwendet wurden.
- Der zusätzliche Pip-Puffer, den der ursprüngliche Experte bei jeder Take-Profit-Order platziert hat, ist als Parameter `ExtraTakeProfitPips` (standardmäßig 25 Pips) verfügbar und wird als zusätzliche Ausstiegsbedingung beibehalten.

> Implementierungen sind in C# und Python verfügbar.
