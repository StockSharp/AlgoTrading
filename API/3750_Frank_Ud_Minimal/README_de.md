# Frank Ud Minimalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel portiert den klassischen **Frank Ud** MetaTrader-Expertenberater nach StockSharp unter Verwendung der High-Level-Strategie API. Das ursprüngliche MQL-Skript führt ein abgesichertes Martingal-Gitter aus, das jedes Mal Positionen hinzufügt, wenn sich der Preis gegenüber dem letzten Eintrag bewegt. Gewinne werden gesperrt, sobald die letzte (und damit größte) Order eine festgelegte Anzahl an Pips erreicht. Danach werden *alle* Trades auf dieser Seite gleichzeitig geschlossen.

## Kernlogik

1. **Symmetrische Absicherung.** Die Strategie unterhält zwei unabhängige Leitern von Marktpositionen: eine lange Leiter und eine kurze Leiter. Daher ist es möglich, gleichzeitig Long- und Short-Positionen zu halten, wie im Absicherungsmodus von MetaTrader.
2. **Martingale-Progression.** Die erste Bestellung auf einer Seite verwendet `InitialVolume` (Standard 0,1 Lots). Jeder weitere Eintrag auf derselben Seite verdoppelt das größte derzeit offene Volumen. Lautstärkeanpassungen berücksichtigen die `MinVolume`-, `MaxVolume`- und `VolumeStep`-Einschränkungen des Instruments.
3. **Einstiegsabstand.** Eine neue Position wird nur hinzugefügt, wenn sich der Preis um mindestens `ReEntryPips` (Standard 41 Pips) über den besten Einstiegspreis der bestehenden Leiter hinaus bewegt hat. The long ladder waits for ask prices to drop below `lowest_buy - ReEntryPips`, while the short ladder waits for bid prices to rise above `highest_sell + ReEntryPips`.
4. **Gewinnernte.** Für jede Leiter fungiert der Trade mit dem größten Volumen als „Trigger“-Order. Wenn sein Gewinn `TakeProfitPips` (Standard 65 Pips) übersteigt oder wenn der Preis das implizite Take-Profit-Level `(TakeProfitPips + 25)` berührt, das von der MQL-Version verwendet wird, wird jede Position auf dieser Seite mit einer einzigen Marktorder abgeflacht.
5. **Margin-Schutz.** Bevor ein neuer Eintrag eingereicht wird, überprüft die Strategie, ob die vom Portfolio gemeldete freie Marge (`CurrentValue - BlockedValue`) über `Balance × MinimumFreeMarginRatio` (Standard 0,5) bleibt. Wenn der Broker keine Portfoliostatistiken meldet, greift die Prüfung auf das Festvolumenverhalten des ursprünglichen Experten zurück.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPips` | Pip-Gewinnschwelle, gemessen an der letzten, größten Bestellung. Once exceeded, all positions on that side are closed. |
| `ReEntryPips` | Mindest-Pip-Abstand zwischen dem besten vorhandenen Eintrag und dem aktuellen Geld-/Briefkurs, bevor eine neue Martingal-Order hinzugefügt wird. |
| `InitialVolume` | Base lot size for the first order of each ladder. Folgeaufträge verdoppeln das größte aktive Volumen. |
| `MinimumFreeMarginRatio` | Erforderliches Verhältnis von freier Margin zu Guthaben, bevor neue Einträge zugelassen werden. Auf 0 setzen, um die Prüfung zu deaktivieren. |

## Hinweise zur Implementierung

- Die Strategie basiert ausschließlich auf Notierungen der Stufe 1: Gebotsaktualisierungen steuern die Short-Ladder-Logik und Briefaktualisierungen steuern die Long-Ladder-Logik.
- Bestellabsichten werden in einem internen Wörterbuch verfolgt, sodass `OnNewMyTrade` weiß, ob eine Ausführung eine Leiter geöffnet oder geschlossen hat. Dies ahmt die explizite Ticketbuchhaltung in der Quelle MQL nach.
- Die Positionsbuchhaltung speichert jede Ausführung (Preis und Volumen) in Listen, anstatt kumulative Statistiken abzufragen, und behält dabei das Verhalten der MQL-Arrays bei, die zum Auffinden des größten Loses und seines Einstiegspreises verwendet wurden.
- Der zusätzliche 25-Pip-Puffer, den der ursprüngliche Experte bei jeder Take-Profit-Order platziert hat, wird als zusätzliche Ausstiegsbedingung beibehalten.

> **Hinweis:** Der Python-Port wird wie gewünscht vorerst absichtlich weggelassen. Der Ordner enthält nur die C#-Implementierung und die mehrsprachige Dokumentation.
