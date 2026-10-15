# X MAN-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die X MAN-Strategie stellt die Kernlogik des MetaTrader-Expertenberaters `X_MAN.mq4` innerhalb des StockSharp-High-Level-API wieder her. Das System handelt Ausbrüche, die durch einen schnellen und langsamen linear gewichteten gleitenden Durchschnitt (LWMA) gesteuert werden, während Einträge mit Multi-Timeframe-Momentum und einer monatlichen MACD-Bestätigung gefiltert werden. Es ist für Trendfortsetzungsgeschäfte konzipiert, die nur dann ausgelöst werden, wenn Momentum und Trendstruktur übereinstimmen.

## Handelslogik

1. **Primärer Trendfilter** – Zwei auf dem ausgewählten primären Zeitrahmen berechnete LWMAs müssen durch mindestens den konfigurierbaren Wert `DistancePoints` getrennt sein. Bei einem langen Setup muss der schnelle LWMA um diesen Abstand über dem langsamen LWMA liegen, während bei einem kurzen Setup der langsame LWMA dominieren muss.
2. **Momentum-Bestätigung** – Die Strategie abonniert eine Kerzenserie mit höherem Zeitrahmen und speist sie in einen Momentum-Indikator ein. Der absolute Abstand der letzten drei Momentumwerte vom neutralen Wert (100) muss mindestens einmal die entsprechende Kauf- oder Verkaufsschwelle überschreiten, um einen Handel in diese Richtung zu ermöglichen.
3. **MACD Filter** – Eine monatliche Kerzenserie treibt einen Standard (12, 26, 9) MACD an. Long-Trades sind nur zulässig, wenn die MACD-Linie über der Signallinie liegt, und Short-Trades erfordern das umgekehrte Verhältnis.
4. **Auftragsausführung** – Wenn alle Filter übereinstimmen, wird die Strategie mit Marktaufträgen umgesetzt. Positionen werden nur dann umgedreht, wenn das entgegengesetzte Setup angezeigt wird und die aktuelle Position flach oder in die entgegengesetzte Richtung ist.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Primärer Zeitrahmen, der für die LWMA-Berechnungen verwendet wird. |
| `HigherCandleType` | Höherer Zeitrahmen, der den Impulsfilter speist. |
| `MacdCandleType` | Zeitrahmen für die MACD-Bestätigung (standardmäßig monatlich). |
| `FastMaPeriod` | Länge des schnellen LWMA. |
| `SlowMaPeriod` | Länge des langsamen LWMA. |
| `MomentumPeriod` | Rückblickfenster des Impulsoszillators. |
| `MomentumBuyThreshold` | Mindestabstand von 100 für bullisches Momentum erforderlich. |
| `MomentumSellThreshold` | Für eine rückläufige Dynamik ist ein Mindestabstand von 100 erforderlich. |
| `DistancePoints` | Minimaler Abstand zwischen schnellem und langsamem LWMA, ausgedrückt in Preispunkten. |
| `TakeProfitPoints` | Optionale schützende Take-Profit-Distanz in Punkten. |
| `StopLossPoints` | Optionaler Schutz-Stop-Loss-Abstand in Punkten. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, sodass sie im StockSharp Designer optimiert oder zur Laufzeit konfiguriert werden können.

## Risikomanagement

Wenn entweder `TakeProfitPoints` oder `StopLossPoints` größer als Null ist, aktiviert die Strategie das integrierte Schutzmodul von StockSharp mithilfe von Marktausstiegen. Es ist noch keine zusätzliche Trailing- oder Breakeven-Logik vom ursprünglichen MQL-Experten implementiert.

## Unterschiede zum Original-Experten

- Die MetaTrader-Implementierung verwaltete Aktienstopps, Break-Even-Bewegungen und komplexe Geldverwaltungsoptionen. Diese Konvertierung konzentriert sich auf die Kernrichtungsfilter und Markteintritte; Auf ein Money Management auf Portfolioebene wird bewusst verzichtet.
- Die Auftragsgröße wird an die Hosting-Umgebung delegiert. Die ursprüngliche Lot-Exponenten-Logik wird nicht reproduziert.
- Warnungen, E-Mail-Benachrichtigungen und manuelle Trailing-Stop-Änderungen sind nicht enthalten.

Diese Änderungen halten die Strategie prägnant und nutzen das hochrangige API von StockSharp, während das Haupthandelskonzept erhalten bleibt.
