# PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nativer RMI mit Schlusskurs-SMA-Ausrichtung, anschließend ein nur enger werdender SMA/ATR-Preisstop. Keine Rentabilitätsbehauptung.

## Indikatoren und Einstieg

Standardmäßig abgeschlossene Fünfminutenkerzen. Nativer RMI verwendet `d[t]=close[t]-close[t-RmiMomentum]`, Lag3. Über die letzten `RmiPeriod=21` Impulse ist U das Mittel positiver d (sonst null), D das Mittel absoluter negativer d (sonst null). `RMI=100*U/(U+D)`; bei Nenner null kein Einstiegswert. Dies ist die native relative Momentumberechnung mit gleitenden Mitteln.

Schlusskurs-SMA und nativer Wilder-ATR teilen `SuperTrendLength=5`. ATR umfasst Lücken zum vorherigen Schlusskurs, beginnt mit wachsendem Mittel, danach Wilder. Nach Bildung aller drei Indikatoren wird nur aus Null eingestiegen: Long bei RMI>60 und close>SMA; Short bei RMI<40 und close<SMA. Marktorder über `Volume`. Eine offene Position ignoriert weitere Einstiegssignale.

`Direction` ist ein optionaler typisierter `Sides`-Filter: C# `null`/Python `None` erlaubt beide; `Sides.Buy` nur Long, `Sides.Sell` nur Short. Nur Einstiege werden gefiltert; der Stop schließt stets die tatsächliche Position. Keine String- oder Zahlencodes.

## Trailing-Stop

`width=SuperTrendMultiplier*ATR`, Standardmultiplikator4. Long startet mit `SMA-width`, danach je abgeschlossener Kerze `max(previousStop,SMA-width)`; Short startet mit `SMA+width`, danach `min(previousStop,SMA+width)`. Keine Lockerung. Initialisierung aus Signal-SMA/ATR unabhängig vom späteren Ausführungspreis; beibehaltene SuperTrend-Parameternamen konfigurieren diese SMA-zentrierten Bänder.

Positiver Bid≤Longstop oder positiver Ask≥Shortstop schließt die gesamte tatsächliche Position per Marktorder. Nach Aktualisierung wird auch der Schlusskurs geprüft; Erreichen des neuen Stops kann sofort schließen. Zwischenkerzenkurse können auslösen. Neue Stops gelten für aktuelle/zukünftige beobachtete Preise, nicht rückwirkend für frühere Kerzenhochs/-tiefs. SMA/ATR aktualisiert Schutz auch bei leerem aktuellen RMI.

Lokale Preisüberwachung: Level1 Bid/Ask benötigt für Schutz durch Zwischenkerzenkurse; ohne Kurse werden Schlusskurse geprüft. Keine native Börsenstoporder; Ausführung kann vom Schwellwert abweichen. Ausstehende Orders blockieren Duplikate, Indikatoren/Stop laufen weiter. Auf der Ausstiegskerze kein Einstieg, auf einer späteren geeigneten Kerze möglich. Keine EMA, sechsstündige Wartezeit, sofortige Gegensignalumkehr oder gesonderter Take-Profit.

| Parameter | Standard |
|---|---:|
| `RmiPeriod` | 21, positiv |
| `RmiMomentum` | 3, positiver Schlusskurs-Lag |
| `SuperTrendLength` | 5, gemeinsame positive SMA/ATR-Periode |
| `SuperTrendMultiplier` | 4, positive ATR-Breite |
| `Direction` | Unbelegt: beide Richtungen |
| `CandleType` | 5 Minuten |

C#-/Python-Tests berechnen Momentumsummen, SMA, Wilder-ATR und nachgezogenen Stop unabhängig im vorhandenen BTC/TON-Archiv. Sie prüfen jeden Einstieg, Ausstieg am ausführbaren Preis und tatsächliche Position, beide Seiten/Filter, gehaltene Gegensignale, enge Zwischenkerzenstops und Einstiege unter sechs Stunden Abstand.
