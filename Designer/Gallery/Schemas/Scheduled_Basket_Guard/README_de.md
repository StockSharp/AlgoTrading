# Zeitgesteuerter Zwei-Leg-Basket mit Geldwächter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm handelt nach der Uhr und nicht nach einem Signal. Es enthält überhaupt keinen Indikator: Einmal am Tag öffnet sich ein kurzes Einstiegsfenster, und das Diagramm kauft im selben Moment zwei Instrumente; ein späteres Fenster stellt beide glatt, und zwischen den beiden Fenstern überwacht ein Geldwächter, was der Basket verdient hat, und schließt ihn vorzeitig bei einem Gewinnziel oder einem Verlustlimit.

![schema](schema.svg)

## Strategieübersicht

- Zwei Variable-Blöcke vom Typ Security benennen die beiden Instrumente, die das Diagramm handelt. Jeder speist seine eigene Kerzenreihe, seine eigene Eröffnungsaktion und seine eigene Schließaktion, sodass die beiden Legs getrennt dimensioniert und verwaltet, aber gemeinsam eröffnet und geschlossen werden.
- Sync hält die beiden 5-Minuten-Kerzenreihen zurück, bis beide Legs denselben Balken geliefert haben, und gibt sie als einen Satz frei, sodass der für den Basket gezeichnete Wert nie einen frischen Preis des einen Legs mit einem veralteten Preis des anderen mischt.
- Ein Formula-Block multipliziert jeden freigegebenen Schlusskurs mit der gehandelten Größe des jeweiligen Legs und addiert die beiden Produkte. Das Ergebnis ist der tatsächliche Wert des Baskets bei den Größen, die das Diagramm handelt, und es ist die Linie, die im Chart-Panel gezeichnet wird.
- Working time liest den Zeitstempel der fertigen Kerzen des ersten Legs und ist nur innerhalb des Einstiegsfensters offen, sodass genau ein Balken pro Tag durchkommt. Flag macht aus dieser Öffnung einen einzelnen Impuls und bleibt eingerastet, bis der Basket glattgestellt ist.
- Der eingerastete Impuls löst zwei Position modify-Blöcke aus, die auf Open position gesetzt sind. Jeder trägt sein eigenes Security und sein eigenes Volume, und die Bedingung Open position lässt das Paar stumm bleiben, sobald dieses Instrument bereits gehalten wird, sodass ein Impuls niemals einen zweiten Basket auf den ersten stapeln kann.
- Strategy P&L speist einen Formula-Block, der realisiertes und offenes Geld addiert. Ein zweiter Formula-Block zieht den Betrag ab, der im Moment der Basket-Eröffnung festgehalten wurde, wodurch aus dem laufenden Gesamtstand des Kontos das Ergebnis allein des aktuellen Baskets wird.
- Ein logisches ODER verbindet drei Gründe zum Glattstellen: das Schließfenster, ein Basket-Ergebnis über dem Gewinnziel und ein Basket-Ergebnis unter dem Verlustlimit. Sein Signal steuert zwei Position modify-Blöcke, die auf Close position gesetzt sind, und gibt außerdem das tägliche Flag frei, sodass das nächste Einstiegsfenster die Verriegelung offen vorfindet.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Innerhalb des Einstiegsfensters öffnet die fertige Kerze des ersten Legs Working time, das tägliche Flag ist noch nicht verbraucht, und beide Open position-Blöcke feuern auf denselben Impuls: Der eine kauft das erste Leg in seiner eigenen Größe, der andere kauft das zweite Leg in seiner eigenen Größe. Jede Order ist eine Market-Order, und jede wird auf ihrem eigenen Instrument unterdrückt, wenn dort bereits eine Position offen ist.
- **Short-Einstieg**: Das Diagramm hat keine Short-Seite: Beide Einstiegsblöcke tragen eine feste Direction Buy. Ein Short-Basket ist nur eine Einstellung entfernt - stellen Sie die Direction der beiden Open position-Blöcke auf Sell, und derselbe Zeitplan, dieselbe Verriegelung und derselbe Geldwächter führen den Basket in die andere Richtung.
- **Ausstieg**: Drei Gründe stellen den Basket glatt, und jeder einzelne von ihnen genügt. Das Schließfenster, das von der Strategie-Uhr und nicht von eintreffenden Kerzen angetrieben wird, stellt planmäßig glatt; ein Basket-Ergebnis, das über das Gewinnziel steigt, stellt vorzeitig im Gewinn glatt; ein Basket-Ergebnis, das unter das Verlustlimit fällt, stellt vorzeitig im Verlust glatt. Alle drei laufen über ein logisches ODER in zwei Close position-Blöcke, die weder ein Volumen noch eine Richtung benötigen, weil sie beides aus der Position berechnen, die sie vorfinden - und gar nichts tun, wenn es keine gibt, sodass ein wiederholtes Signal innerhalb des Fensters harmlos ist.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| First Leg Security | BTCUSDT@BNBFT | Instrument, das vom ersten Leg gehandelt wird; es liefert die Kerzenreihe, die das Einstiegsfenster taktet. |
| Second Leg Security | TONUSDT@BNBFT | Instrument, das vom zweiten Leg gehandelt wird; es wird mit denselben Signalen eröffnet und geschlossen wie das erste. |
| First Leg Candles | 00:05:00 | Zeitrahmen des ersten Legs. Es werden nur fertige Kerzen verwendet, das Einstiegsfenster muss daher mindestens eine Kerze breit sein. |
| Second Leg Candles | 00:05:00 | Zeitrahmen des zweiten Legs. Halten Sie ihn gleich dem des ersten Legs, da beide zusammengehalten werden, bevor der Basket-Wert berechnet wird. |
| Alignment Interval | 00:05:00 | Gruppierungsintervall, mit dem die beiden Legs ausgerichtet werden. Es sollte dem Zeitrahmen der Kerzen entsprechen; ein größerer Wert würde das Paar später freigeben als der Balken, zu dem es gehört. |
| Entry Window From | 10:00:00 | Beginn des täglichen Einstiegsfensters, gelesen aus dem Zeitstempel der fertigen Kerzen des ersten Legs. |
| Entry Window Until | 10:04:00 | Ende des täglichen Einstiegsfensters. Die Spanne zwischen den beiden Werten muss genau eine Kerzeneröffnung enthalten, sonst würde die Tagesverriegelung mehr als einmal scharf geschaltet. |
| Flatten Window From | 17:00:00 | Beginn des täglichen Glattstellungsfensters, gelesen von der Strategie-Uhr und nicht aus eintreffenden Kerzen. |
| Flatten Window Until | 17:10:00 | Ende des täglichen Glattstellungsfensters. Halten Sie es einige Kerzen breit, damit die Uhr darin mindestens einmal abgetastet wird. |
| First Leg Size | 0.01 | Menge, mit der das erste Leg eröffnet wird, und zugleich das Gewicht, das das erste Leg im gezeichneten Basket-Wert trägt. |
| Second Leg Size | 100 | Menge, mit der das zweite Leg eröffnet wird, und zugleich das Gewicht, das das zweite Leg im gezeichneten Basket-Wert trägt. Wählen Sie sie so, dass beide Legs vergleichbare Geldbeträge beitragen. |
| Profit Target | 100 | Geldbetrag, den der aktuelle Basket verdient hat und oberhalb dessen er vorzeitig geschlossen wird. Er wird ab dem Moment der Basket-Eröffnung gemessen, nicht ab dem Start des Laufs. |
| Loss Limit | -500 | Geldbetrag, den der aktuelle Basket verloren hat und unterhalb dessen er vorzeitig geschlossen wird; ein negativer Wert, auf dieselbe Weise gemessen wie das Gewinnziel. |

## Diagrammdetails

- Zwei [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)-Blöcke vom Typ Security sind die einzige Stelle, an der ein Instrument benannt wird. Jeder speist einen [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Block und den Security-Eingang der beiden Position modify-Blöcke, die dieses Leg verwalten, sodass ein Leg durch Ändern eines einzigen Wertes auf ein anderes Instrument umgestellt wird.
- [Sync](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/sync.html) nimmt eine Linie je Leg entgegen und lässt beide gemeinsam heraus, sobald der Balken auf beiden Seiten vollständig ist. Die freigegebenen Kerzen werden im Chart-Panel gezeichnet und in Schlusskurse umgewandelt, die ein [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html)-Block mit den gehandelten Größen gewichtet und zur Linie des Basket-Werts zusammenfasst.
- Die beiden Uhren sind bewusst verschieden. Die Einstiegs-[Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) wird von den Kerzen des ersten Legs gespeist, damit die Einstiegsentscheidung nicht von dem Preis abdriften kann, zu dem sie getroffen wird; die schließende Working time wird von [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) gespeist, sodass das Glattstellen auch dann erfolgt, wenn die Daten verstummen. [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) sitzt zwischen dem Einstiegsfenster und den Orders und wird nur durch das Glattstellungssignal zurückgesetzt, und genau das begrenzt das Diagramm auf einen Basket pro Tag.
- [P&L change](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) meldet bei jeder Aktualisierung realisiertes und offenes Geld. Ihre Addition ergibt den laufenden Gesamtstand des Kontos; ein Variable-Block hält diesen Stand beim ersten Einstiegs-Fill fest, ein zweiter Variable-Block bewahrt ihn auf und gibt ihn bei jeder späteren Aktualisierung erneut aus, und seine Subtraktion lässt das Ergebnis des gerade offenen Baskets übrig. Der Wächter misst damit den aktuellen Basket und nicht die gesamte Lebensdauer des Kontos, und er geht auf null zurück, sobald der Basket geschlossen ist.
- Jede Aktion ist ein [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Block. Die beiden Einstiegsblöcke verwenden die Bedingung Open position mit ausdrücklicher Richtung und ausdrücklichem Volumen; die beiden Ausstiegsblöcke verwenden Close position, das beides nicht entgegennimmt, sondern aus der aktuellen Position seines eigenen Instruments ableitet. [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) führt die vier Fill-Ströme zu der einen Trade-Reihe zusammen, die im Chart-Panel gezeichnet wird, während die vier Order-Ströme getrennt gezeichnet werden.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
