# iCHO Trend CCIDualOnMA Filterstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein High-Level-StockSharp-Port des MetaTrader-Expertenberaters **„iCHO Trend CCIDualOnMA Filter“**. Es kombiniert einen Chaikin-Oszillator-Nulllinienregimefilter mit einer dualen Commodity Channel Index (CCI)-Bestätigung, die auf einer geglätteten Preisreihe berechnet wird. Das Ergebnis ist ein Trendfolge-Ansatz, der auf Momentum-Verschiebungen reagiert, aber dennoch eine Momentum-Bestätigung durch das Paar CCI erfordert, bevor ein Handel eingeleitet wird.

## Handelslogik

1. **Chaikin-Oszillatorkern** – die Akkumulations-/Verteilungslinie wird durch zwei konfigurierbare gleitende Durchschnitte geglättet. Ihr Unterschied spiegelt den Chaikin-Oszillator wider. Kreuze über/unter Null signalisieren eine Änderung des vorherrschenden Kapitalflusses.
2. **Dualer CCI-Filter** – beide CCI-Instanzen verwenden dieselbe Preiseingabe mit gleitendem Durchschnitt und Glättung, aber unterschiedliche Lookback-Zeiträume. Bei einem langen Setup muss sich der schnelle CCI vom negativen Bereich erholen und den langsamen CCI überschreiten, während der Chaikin-Oszillator über Null bleibt. Ein kurzer Aufbau spiegelt diese Bedingungen wider.
3. **Optionale Umkehrung** – das Original EA bietet eine „Umkehr“-Flagge, die Long- und Short-Signale vertauscht. Der Port behält dieses Verhalten bei, sodass die gleichen Regeln für Gegentrendtests verwendet werden können.
4. **Positionsverwaltung** – optionale Flags schließen das Gegenrisiko, bevor eine neue Position eröffnet wird, und beschränken die Strategie auf eine einzige offene Position. Zur Nachahmung der MetaTrader-Implementierung wird eine Ein-Trade-pro-Barren-Regel durchgesetzt.
5. **Sitzungsfilter** – Der Handel kann auf ein benutzerdefiniertes Intraday-Fenster beschränkt werden, einschließlich Wrap-Around-Sitzungen, die über Mitternacht hinausgehen.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `FastChaikinLength` | Schnelle gleitende Durchschnittsperiode, die im Chaikin-Oszillator verwendet wird. |
| `SlowChaikinLength` | Langsame gleitende Durchschnittsperiode, die im Chaikin-Oszillator verwendet wird. |
| `ChaikinMethod` | Methode des gleitenden Durchschnitts (einfach, exponentiell, geglättet, linear gewichtet), angewendet auf die Akkumulations-/Verteilungslinie. |
| `FastCciLength` | Rückblick auf den Fast Commodity Channel Index. |
| `SlowCciLength` | Rückblick auf den langsamen Commodity Channel Index. |
| `MaLength` | Länge des gleitenden Vorverarbeitungsdurchschnitts, der die CCIs speist. |
| `MaMethod` | Methode des gleitenden Durchschnitts, die zur Vorverarbeitung des Preises verwendet wird, bevor er die CCIs erreicht. |
| `MaPrice` | Preisart (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet), die vor den CCIs geglättet wird. |
| `UseClosedBar` | Verarbeiten Sie nur vollständig fertige Kerzen (Standardeinstellung: wahr, identisch mit `SignalsBarCurrent=bar_1` im EA). |
| `ReverseSignals` | Tauschen Sie Long- und Short-Logik aus. |
| `CloseOpposite` | Schließen Sie eine offene Position in die entgegengesetzte Richtung, bevor Sie eine neue eröffnen. |
| `OnlyOnePosition` | Erlauben Sie immer nur eine einzige offene Position. |
| `TradeMode` | Beschränken Sie die Ausführung auf Longs, Shorts oder beides (BuyOnly, SellOnly, BuyAndSell). |
| `UseTimeFilter` | Aktivieren Sie den Handelssitzungsfilter. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Sitzungsgrenzen (einschließlich Beginn, ausschließlich Ende), ausgedrückt in Austauschzeit. Wrap-Around-Sitzungen werden unterstützt. |
| `CandleType` | Zeitrahmen des Kerzenabonnements, das die Indikatoren speist. |

## Notizen

- Die Strategie verwendet nur `SubscribeCandles`-Bindungen und integrierte Indikatoren auf hoher Ebene. Es sind keine benutzerdefinierten Puffer oder historischen Anforderungen erforderlich.
- Alle preisbasierten Berechnungen verwenden die gleiche Vorverarbeitung des gleitenden Durchschnitts wie der Indikator MetaTrader `CCIDualOnMA`, indem sie den Indikator CCI mit einer geglätteten Preisreihe versorgen.
- Die Standardparameter reproduzieren die ursprünglichen EA-Standardwerte: Chaikin 3/10 EMA, CCI Perioden 14 und 50, 12-Perioden-SMA-Vorverarbeitung und ein Handelsfenster von 10:01 bis 15:02.
