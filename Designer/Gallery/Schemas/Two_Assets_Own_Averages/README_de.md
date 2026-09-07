# Strategiediagramm für zwei Assets mit jeweils eigenem Durchschnitt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm richtet abgeschlossene Fünfzehn-Minuten-Kerzen von BTCUSDT@BNBFT und TONUSDT@BNBFT zeitlich aus, vergleicht jeden Schlusskurs mit dem für dieses Instrument berechneten einfachen gleitenden 20-Perioden-Durchschnitt und nutzt die gegenläufigen Relationen zur Verwaltung von Long- und Short-Exposure in BTCUSDT. Ein vorzeichenbehafteter, durch Ausführungen aktualisierter Zustandsmerker, einstufige Market-Umkehrungen, reguläre Ausstiege und ein lokaler fester 2%-Stop vervollständigen den Ablauf.

![schema](schema.svg)

## Strategieübersicht

- Getrennte Wertpapiervariablen konfigurieren ausschließlich die Kerzenabonnements für BTCUSDT und TONUSDT. Orderaktionen und der Strategy-trades-Strom verwenden das gewählte Strategy Security, das auf BTCUSDT@BNBFT gesetzt sein muss, damit es dem Parameter Traded Security entspricht.
- Nur abgeschlossene Fünfzehn-Minuten-Kerzen gelangen in einen Sync-Baustein. Jedes ausgerichtete Paar liefert BTC Close und BTC SMA(20) auf einem Zweig sowie TON Close und TON SMA(20) auf dem anderen; eine Entscheidung beginnt erst, wenn beide Durchschnitte gebildet sind.
- Die Long-Relation erfordert strikt `BTC Close < BTC SMA(20)` zusammen mit `TON Close > TON SMA(20)`. Die Short-Relation erfordert strikt `BTC Close > BTC SMA(20)` zusammen mit `TON Close < TON SMA(20)`. Gleichheit erfüllt keine der beiden vollständigen Relationen.
- Ein numerischer vorzeichenbehafteter Zustandsmerker hält den vom Diagramm verwalteten BTC-Status fest: `-1` bedeutet Short, `0` Flat und `1` Long. Aus Flat sendet eine vollständige Relation einen Market-Einstieg über eine Einheit. Aus dem entgegengesetzten Status steigt das Aktionsvolumen auf zwei Einheiten, schließt das bestehende Exposure über eine Einheit und stellt mit einer Market-Aktion eine Einheit in der neuen Richtung her.
- Eine vollständige Gegenrelation hat Vorrang vor einem regulären Ausstieg. Andernfalls schließt BTC auf der strikten Ausstiegsseite seines eigenen Durchschnitts die aktuelle Richtung mit einer ReduceOnly-Market-Aktion über eine Einheit. Jede synchronisierte Entscheidung ist abgeschlossen, bevor der gespeicherte BTC-Schlusskurs an den lokalen festen 2%-Market-Stop weitergegeben wird.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn ein synchronisiertes abgeschlossenes Kerzenpaar `BTC Close < BTC SMA(20)` und `TON Close > TON SMA(20)` erfüllt, akzeptiert das Kauf-Gatter einen flachen oder Short-Zustandsmerker. Es sendet aus Flat einen NoCondition-Market-Kauf mit Volume 1 oder aus einem Short über eine Einheit einen Kauf mit Volume 2, um direkt in einen BTC-Long über eine Einheit umzukehren.
- **Short-Einstieg**: Wenn ein synchronisiertes abgeschlossenes Kerzenpaar `BTC Close > BTC SMA(20)` und `TON Close < TON SMA(20)` erfüllt, akzeptiert das Verkaufs-Gatter einen flachen oder Long-Zustandsmerker. Es sendet aus Flat einen NoCondition-Market-Verkauf mit Volume 1 oder aus einem Long über eine Einheit einen Verkauf mit Volume 2, um direkt in einen BTC-Short über eine Einheit umzukehren.
- **Ausstieg**: Ein Long wird mit einem ReduceOnly-Market-Verkauf über eine Einheit geschlossen, wenn BTC Close strikt über BTC SMA(20) liegt und die vollständige Short-Relation fehlt. Ein Short wird mit einem ReduceOnly-Market-Kauf über eine Einheit geschlossen, wenn BTC Close strikt unter BTC SMA(20) liegt und die vollständige Long-Relation fehlt. Diese Negativprüfungen überlassen eine vollständige Gegenrelation dem Umkehrzweig über zwei Einheiten. Der lokale Schutz kann beide Richtungen außerdem mit einem festen 2%-Market-Stop schließen; Take Profit 0 deaktiviert das Gewinnziel.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | Wertpapier, das ausschließlich vom Kerzenabonnement des gehandelten Instruments verwendet wird. Setzen Sie Strategy Security auf denselben Wert BTCUSDT@BNBFT, da sämtliche Orderaktionen und der Strategy-trades-Strom Strategy Security verwenden. |
| Signal Security | TONUSDT@BNBFT | Wertpapier, das ausschließlich vom zweiten Kerzenabonnement verwendet wird. Seine Kurs-Durchschnitt-Relation fließt in die Entscheidungen ein, aber keine Orderaktion ist an diese Variable adressiert. |
| BTC Candles Series | 00:15:00 | Abgeschlossene Fünfzehn-Minuten-Kerzenserie für BTCUSDT, die zur Synchronisierung, für BTC Close, BTC SMA(20), Schutzprüfungen und den Chart verwendet wird. |
| TON Candles Series | 00:15:00 | Abgeschlossene Fünfzehn-Minuten-Kerzenserie für TONUSDT, die zur Synchronisierung, für TON Close, TON SMA(20) und den Chart verwendet wird. |
| BTC SMA Length | 20 | Periode der SimpleMovingAverage, die aus synchronisierten abgeschlossenen BTCUSDT-Kerzen berechnet wird. |
| TON SMA Length | 20 | Periode der SimpleMovingAverage, die aus synchronisierten abgeschlossenen TONUSDT-Kerzen berechnet wird. |
| Base Volume | 1 | Standardmenge für den Market-Einstieg. Das Aktionsvolumen lautet `Base Volume * (1 + abs(latch))`; damit verwendet ein Flat-Einstieg Base Volume und eine Umkehrung das Doppelte von Base Volume. Beim Standardwert sind dies Volume 1 und Volume 2. |
| Take Profit | 0 | Ein absoluter Wert von null deaktiviert den Take-Profit-Schutz. |
| Stop Loss | 2% | Nachteiliger prozentualer Abstand zum geschützten Ausführungskurs, bei dem der Stop-Loss aktiviert wird. |
| Trailing Stop Loss | false | Deaktiviert, sodass der 2%-Stop fest bleibt und keiner günstigen Kursbewegung folgt. |
| Use Market Orders | true | Aktiviert, sodass ein ausgelöster Stop das geschützte Exposure mit einer Market-Order schließt. |

## Diagrammdetails

- Zwei [Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)-Bausteine vom Typ Wertpapier speisen ausschließlich ihre jeweiligen [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Bausteine. Ein [Synchronisierung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/sync.html)-Baustein richtet die abgeschlossenen Ströme bei `00:15:00` aus, bevor einer der Zweige die Entscheidungskette erreicht.
- Jede synchronisierte Kerze wird in Close und einen gebildeten [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Wert aufgeteilt. Vier strikte Vergleichsbausteine bilden die gegenläufigen Long- und Short-Relationen aus dem Close jedes Instruments und seiner eigenen SMA(20).
- Eine numerische Unit-Variable hält den vorzeichenbehafteten Zustandsmerker. Ausführungen schreiben `1` nach einem Kauf, `-1` nach einem Verkauf und `0` nach einem regulären oder schützenden Ausstieg. Statusvergleiche erlauben Einstiege aus Flat und Umkehrungen von der Gegenseite; die Volumenformel lautet `Base Volume * (1 + abs(latch))`.
- Die Gatter der Long- und Short-Relationen steuern NoCondition-, MarketOrder-[Position ändern](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Aktionen an. Getrennte ReduceOnly-Market-Aktionen übernehmen reguläre Ausstiege. Jedes Gatter für einen regulären Ausstieg verlangt zusätzlich, dass die vollständige Gegenrelation falsch ist, sodass für dasselbe synchronisierte Paar nicht sowohl eine Umkehrung als auch eine Schließung über eine Einheit angefordert werden können.
- Der [Positionsschutz](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) erhält Ausführungen von Einstiegen, Umkehrungen und regulären Ausstiegen. Take Profit ist `0`, Stop Loss ist `2%`, Trailing Stop Loss ist `false`, Use Market Orders ist `true`, und der Schutz läuft lokal. Der synchronisierte BTC-Schlusskurs wird zunächst gespeichert und erst an den Schutz weitergegeben, nachdem beide Signalzweige ihre Entscheidung für dieses Paar abgeschlossen haben.
- Der Chart erhält die synchronisierten Kerzenströme für BTCUSDT und TONUSDT, BTC SMA(20), TON SMA(20), den Stop-Loss-Orderstrom sowie alle BTCUSDT-Ausführungen aus Strategy trades.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
