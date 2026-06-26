# Tengri-Strategie (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine High-Level-StockSharp-Nachbildung des MetaTrader-Expertenberaters *Tengri*. Der ursprüngliche Berater handelt EURUSD und USDCHF mit einem Grid-und-Scale-Ansatz, der durch RSI, benutzerdefinierte "Silence"-Volatilitätsfilter und einen EMA-Trendmesser gesteuert wird. Die C#-Version behält den Verhaltenskern bei und passt ihn an StockSharp-Konventionen und Netto-Positionsrechnung an.

## Kernideen

- **Direktionaler Bias** – vergleicht das aktuelle Angebot mit dem Eröffnungspreis einer höheren Zeitrahmen-Kerze (Standard 30 Minuten). Eine positive Differenz tendiert die Strategie zu Long, eine negative zu Short.
- **Momentum-Filter** – ein 14-periodiger RSI, berechnet auf Stunden-Kerzen, muss unter 70 für Long-Einträge und über 30 für Short-Einträge bleiben, entsprechend der MetaTrader-Logik.
- **Stille-Markt-Filter** – der ursprüngliche benutzerdefinierte "Silence"-Indikator wird mit ATR-Werten emuliert, die durch EMAs auf zwei verschiedenen Zeitrahmen geglättet werden. Beide Filter müssen unter konfigurierbaren Schwellenwerten bleiben, um Einträge oder Scale-ins zu erlauben.
- **Trendbestätigung** – eine EMA auf einem mittleren Zeitrahmen stellt sicher, dass Long-Ergänzungen nur oberhalb der EMA und Short-Ergänzungen nur unterhalb stattfinden.
- **Grid- und Martingale-Sizing** – der erste Trade verwendet entweder ein festes Lot oder ein eigenkapitalproportionales Lot. Zusätzliche Trades multiplizieren das vorherige Volumen mit konfigurierbaren Faktoren (1.70 vor `StepX`, 2.08 danach standardmäßig).
- **Pip-Abstand** – der Abstand zwischen Grid-Orders folgt zwei Basisschritten (10 Pips und 20 Pips standardmäßig) und kann exponentiell durch `PipStepExponent` vergrößert werden.

## Trading-Workflow

1. **Einstiegsbewertung** (pro `EntryCandleType`, Standard M1):
   - Richtung aus der `DealCandleType`-Kerze bestimmen.
   - RSI und den ersten Stille-Filter prüfen.
   - Sicherstellen, dass keine aktiven Trades in dieselbe Richtung vorhanden sind (entgegengerichtete Positionen werden zuerst geflacht, da StockSharp-Portfolios netted werden).
   - Eine Market-Order mit der berechneten Lot-Größe abgeben. Der erste Trade speichert ein pip-basiertes Take-Profit-Ziel.
2. **Scale-in-Bewertung** (pro `ScaleCandleType`, Standard M1):
   - EMA-Trend und den zweiten Stille-Filter bestätigen.
   - Überprüfen, ob der letzte Ausführungspreis weit genug vom aktuellen Markt entfernt ist, anhand der Pip-Step-Regeln.
   - Eine weitere Market-Order mit Martingale-Sizing hinzufügen, solange die Richtung gültig bleibt und die Trade-Anzahl unter `MaxTrades` liegt.
3. **Positionsmanagement**:
   - Das optionale globale Gewinnziel schließt die Position, wenn sowohl Long- als auch Short-Stacks vorhanden sind und der kombinierte nicht realisierte PnL `Equity / LimitDivisor` übersteigt.
   - Der Take-Profit des ersten Trades dient als einfacher Ausstieg: Wenn das Angebot/die Nachfrage das gespeicherte Ziel erreicht, wird die gesamte Nettoposition geflacht.
   - Kein automatischer Stop-Loss wird verwendet, entsprechend dem MetaTrader-Code.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `DealCandleType` | Zeitrahmen, dessen Eröffnungspreis den direktionalen Bias definiert. |
| `EntryCandleType` | Zeitrahmen zur Bewertung von Einstiegssignalen. |
| `ScaleCandleType` | Zeitrahmen zur Prüfung von Grid-Ergänzungen. |
| `MaCandleType` | Zeitrahmen für den EMA-Trendfilter. |
| `Silence1CandleType` / `Silence2CandleType` | Zeitrahmen für ATR-basierte Volatilitätsfilter. |
| `RsiPeriod` | RSI-Länge (Standard 14). |
| `SilencePeriod1/2`, `SilenceInterpolation1/2`, `SilenceLevel1/2` | ATR-Glättung und Schwellenwerte zur Steuerung der zwei Stille-Filter. |
| `MaPeriod` | EMA-Periode. |
| `PipStep`, `PipStep2`, `PipStepExponent` | Abstände zwischen Scale-in-Trades. |
| `LotExponent1`, `LotExponent2`, `StepX` | Martingale-Faktoren für zusätzliche Positionen. |
| `LotSize`, `FixLot`, `LotStep` | Money-Management-Einstellungen für die erste Position. |
| `SlTpPips` | Pip-Abstand zur Festlegung eines Take-Profits für den ersten Trade (0 deaktiviert ihn). |
| `MaxTrades` | Maximale Anzahl von Einträgen pro Richtung. |
| `UseLimit`, `LimitDivisor` | Konfiguration zur globalen Gewinnsperre. |
| `CloseFriday`, `CloseFridayHour` | Optionale Sperre für Spät-Freitag-Einträge. |

## Unterschiede zur MetaTrader-Version

- **Silence-Indikator-Ersatz** – der proprietäre "Silence"-Indikator wird mit ATR-Werten angenähert, die durch EMAs geglättet werden. Die Schwellenwerte behalten dieselbe numerische Skala, können aber angepasst werden, wenn der ATR-Proxy sich anders verhält.
- **Netto-Positionsrechnung** – StockSharp-Portfolios werden netted, daher flacht die Strategie die entgegengesetzte Richtung ab, bevor ein neuer Stack geöffnet wird, anstatt beide Seiten gleichzeitig abzusichern.
- **Take-Profit-Handling** – MetaTrader hängt TP nur an die erste Order. Der Port schließt die gesamte Nettoposition, wenn dieses Ziel ausgelöst wird. Zusätzliche Orders haben absichtlich kein TP, entsprechend dem ursprünglichen Risikomodell.
- **Symbolwahl** – die Strategie verwendet das der Strategieinstanz zugewiesene `Security`. Separate Instanzen für EURUSD, USDCHF oder ein anderes Instrument konfigurieren.

## Verwendungshinweise

- Den Volumenschritt und die Min-/Max-Volumina am Zielinstrument konfigurieren, damit die `LotCheck`-Rundung mit den Broker-Anforderungen übereinstimmt.
- Die Strategie setzt voraus, dass die Broker-Kurse Best-Bid/Ask-Updates liefern. Ohne Level1-Daten können die Richtungs- und TP-Prüfungen nicht funktionieren.
- Da kein Stop-Loss vorhanden ist, sollte die Strategie mit externen Risikokontrollen (Eigenkapital-Stop, manuelle Überwachung usw.) betrieben werden.

## Visualisierung

Um das Verhalten zu analysieren, Chart-Widgets mit den abonnierten Kerzenserien (Richtungs-, Einstiegs- und Skalierungszeitrahmen) verbinden und die EMA- und ATR-Indikatoren überlagern. Dies spiegelt die Diagnosewerkzeuge wider, die mit dem ursprünglichen Berater verwendet werden.
