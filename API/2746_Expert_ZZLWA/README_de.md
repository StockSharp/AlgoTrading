# Expert-ZZLWA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Diese Strategie ist ein StockSharp-High-Level-Port des ursprünglichen **ExpertZZLWA** MetaTrader-5-Expert-Advisors. Der EA bot drei verschiedene Betriebsmodi und optional Martingale-Positionsgrößen. Der Port behält die Struktur des ursprünglichen Experten bei und passt ihn an StockSharp-Kerzen und -Indikatoren an:

1. **Original-Modus** – wechselt auf jedem abgeschlossenen Balken zwischen Long- und Short-Trades, solange keine offene Position vorhanden ist.
2. **ZigZag-Addition-Modus** – recreiert das Verhalten des benutzerdefinierten Indikators „ZigZag LW Addition", indem er neue Swing-Hochs und -Tiefs durch rollende Höchst-/Tiefstwerte verfolgt.
3. **Moving-Average-Test-Modus** – spiegelt die geglättete MA (150) vs. einfache MA (10) Crossover-Logik aus dem MQL-Code wider.

Alle Modi verwenden konfigurierbare schützende Stop-Loss- und Take-Profit-Offsets in Preispunkten. Die Strategie unterstützt optional Martingale-Sizing, bei dem ein neuer Trade nach einem realisierten Verlust um einen Multiplikator erhöht wird, begrenzt durch ein maximales Volumen.

## Handelslogik

### Original-Modus

- Arbeitet nur mit fertigen Kerzen.
- Wenn keine Position offen ist, wechselt die Strategie auf jedem neuen Balken zwischen Long- und Short-Marktorders.
- Stop-Loss und Take-Profit werden über den eingebauten `StartProtection`-Helper registriert.
- Sobald ein Trade schließt (entweder am Stop oder am Ziel), wird die entgegengesetzte Richtung für den nächsten Balken aktiv.

### ZigZag-Addition-Modus

- Abonniert die ausgewählte Kerzenserie und pflegt rollende `Highest`- und `Lowest`-Indikatoren.
- Erkennt ein Swing-Hoch, wenn das Kerzenhoch den aktuellen Höchstwert berührt, während die vorherige Swing-Richtung nicht aufwärts war. Dies recreiert die Buy/Sell-Puffer-Signale von „ZigZag LW Addition".
- Erkennt ein Swing-Tief, wenn das Kerzentief den rollenden Tiefstwert in der entgegengesetzten Weise berührt.
- Generiert unmittelbar nach dem Kerzenschluss eine Marktorder in der signalisierten Richtung.

### Moving-Average-Test-Modus

- Erstellt einen geglätteten gleitenden Durchschnitt mit Länge 150 und einen einfachen gleitenden Durchschnitt mit Länge 10 (entspricht der MQL-Implementierung).
- Erzeugt ein Long-Signal, wenn der geglättete MA vom vorherigen Balken zum aktuellen Balken über den einfachen MA kreuzt.
- Erzeugt ein Short-Signal, wenn der geglättete MA unter den einfachen MA kreuzt.
- Signale werden nur auf geschlossenen Kerzen verarbeitet.

### Martingale-Behandlung

- Nach jedem eigenen Trade verfolgt die Strategie die Nettoposition und den durchschnittlichen Einstiegspreis.
- Wenn eine Position vollständig geschlossen wird, wird der realisierte Gewinn des letzten Trades aufgezeichnet.
- Wenn der Trade mit Verlust schloss und Martingale aktiviert ist, wird das nächste Ordervolumen zu `letztes_Volumen * MartingaleMultiplier` (begrenzt durch `MaximumVolume`).
- Wenn der Trade mit Gewinn schloss oder Martingale deaktiviert ist, fällt die Strategie auf das Basisvolumen zurück.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `StopLossPoints` | 600 | Abstand zum Schutz-Stop in Preispunkten. |
| `TakeProfitPoints` | 700 | Abstand zum Take-Profit in Preispunkten. |
| `BaseVolume` | 0.01 | Standard-Ordergröße, wenn Martingale nicht angewendet wird. |
| `UseMartingale` | false | Aktiviert Martingale-Sizing, wenn auf true gesetzt. |
| `MartingaleMultiplier` | 2 | Multiplikator, der nach einem Verlust auf das letzte Trade-Volumen angewendet wird. |
| `MaximumVolume` | 10 | Maximal zulässiges Volumen für Martingale-Sizing. |
| `Mode` | Original | Betriebsmodus: `Original`, `ZigZagAddition` oder `MovingAverageTest`. |
| `ZigZagTerm` | LongTerm | Empfindlichkeits-Preset für den ZigZag-Modus (ShortTerm, MediumTerm, LongTerm). |
| `SlowMaPeriod` | 150 | Periode des geglätteten MA im MA-Test-Modus. |
| `FastMaPeriod` | 10 | Periode des einfachen MA im MA-Test-Modus. |
| `CandleType` | 15-Minuten-Zeitrahmen | Für die Verarbeitung abonnierter Kerzentyp. |

## Hinweise

- Stop/Take-Offsets werden mit dem Instrument-`PriceStep` multipliziert, was dem `_Point`-Verhalten von MetaTrader entspricht.
- Die Strategie verwendet ausschließlich die StockSharp-High-Level-API (`SubscribeCandles` + Indikator-Bindung).
- Die ZigZag-Empfindlichkeits-Presets entsprechen Highest/Lowest-Längen von 12 (Kurz), 24 (Mittel) und 48 (Lang). Passen Sie diese an, wenn eine andere Swing-Breite erforderlich ist.
- Der Martingale-Tracker basiert auf eigenen Trade-Benachrichtigungen; stellen Sie sicher, dass die Strategie in einer Umgebung läuft, in der Fills korrekt gemeldet werden.

## Konversionsunterschiede vs. MQL

- Die MQL-Version interagierte mit einem kompilierten `ZigZag LW Addition`-Indikator. In StockSharp approximieren wir die Puffer mit rollenden Hochs/Tiefs, was ähnliche Signale ohne externe Binärdateien liefert.
- Die Orderplatzierung basiert auf `BuyMarket` / `SellMarket` und dem verwalteten Schutz-Helper anstelle von manuellen Order-Tickets.
- Die historische Lot-Berechnung im ursprünglichen Experten verwendete die Terminal-Deal-History. Der Port repliziert dieses Verhalten durch Analyse eigener Trades in Echtzeit und Speicherung des letzten geschlossenen Trade-Volumens und -Gewinns.
- Slip- und Magic-Number-Eingaben aus MQL werden weggelassen, da StockSharp sie für Marktorders in diesem Kontext nicht benötigt.
