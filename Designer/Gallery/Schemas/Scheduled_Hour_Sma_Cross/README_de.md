# SMA-Kreuzung zu einer festgelegten Stunde
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm wertet einen schnellen und einen langsamen gleitenden Durchschnitt auf abgeschlossenen dreißigminütigen BTCUSDT-Kerzen aus, erlaubt geplante Einstiege bei Kerzen mit Eröffnungsstunde 12, schließt gegenläufige Positionen außerhalb dieser Stunde und trennt Marktaktionen durch eine Abkühlzeit von acht Kerzen.

![schema](schema.svg)

## Strategieüberblick

- Abgeschlossene Dreißig-Minuten-Kerzen speisen SMA(8) und SMA(21), die nur gebildete Werte ausgeben. Eine Entscheidung wird erst freigegeben, wenn beide Werte für dieselbe Kerze vorliegen.
- Ein Aufwärtstrend bedeutet `SMA(8) > SMA(21)`, ein Abwärtstrend bedeutet `SMA(8) < SMA(21)`. Gleiche Werte lösen keine Aktion aus.
- Der Time-Block liefert den Entscheidungszeitstempel, und Converter extrahiert daraus Hour. Im Stapel einer abgeschlossenen Kerze stimmt dieser Zeitstempel mit dem von der Regel verwendeten `OpenTime` überein. Ein einmaliges Flag erlaubt genau eine Entscheidung je Kerze, auch während synchroner Orderereignisse.
- Ein durch Ausführungen gesteuerter Zustand hält die Nettoposition als `-1`, `0` oder `1`. Jeder der vier Aktionspfade bereitet vor der Order seinen eigenen Folgezustand vor und übernimmt ihn nur, wenn dieser Pfad eine Ausführung meldet.
- Jede Aktion startet eine Abkühlzeit von acht Kerzen. Die nachfolgenden Kerzen 1 bis 7 bleiben gesperrt; die achte nachfolgende abgeschlossene Kerze setzt den Zähler vor ihrer Entscheidung auf null herab und ist wieder berechtigt.

## Ein- und Ausstiegsregeln

- **Geplante bullische Aktion**: Bei einer Kerze mit `OpenTime.Hour` gleich 12 sendet ein Aufwärtstrend bei flacher oder kurzer Position einen Marktkauf mit Volume 1. Eine kurze Position wird auf null reduziert und nicht direkt gedreht.
- **Geplante bärische Aktion**: In derselben Stunde sendet ein Abwärtstrend bei flacher oder langer Position einen Marktverkauf mit Volume 1. Eine lange Position wird auf null reduziert und nicht direkt gedreht.
- **Ausstieg außerhalb der Stunde**: Zu jeder anderen Eröffnungsstunde schließt ein Abwärtstrend eine lange Position mit einem Marktverkauf, während ein Aufwärtstrend eine kurze Position mit einem Marktkauf schließt. Außerhalb der Stunde 12 wird keine neue Position eröffnet.
- Es gibt keine separate Schlussstunde. Alle vier Zweige benötigen eine verfügbare Abkühlzeit, und nur ein wahrer Zweig kann seinen unabhängigen Modify-position-Block auslösen.

## Parameter

| Parameter | Standardwert | Beschreibung |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrument für Candles und Strategy trades. Stellen Sie Strategy Security für Transaktionen auf dasselbe Instrument. |
| Candle Series | 00:30:00 | Intervall abgeschlossener Kerzen und Takt für Indikatoraktualisierungen und Abkühlschritte. |
| Fast SMA Length | 8 | Anzahl abgeschlossener Kerzen im schnellen einfachen gleitenden Durchschnitt. |
| Slow SMA Length | 21 | Anzahl abgeschlossener Kerzen im langsamen einfachen gleitenden Durchschnitt. |
| Trade Hour | 12 | Zulässiger Wert des Feldes `OpenTime.Hour` der abgeschlossenen Kerze für geplante Einstiegsaktionen. |
| Cooldown N | 8 | Frühester Index einer nachfolgenden abgeschlossenen Kerze, an dem eine weitere Aktion geprüft werden kann. |
| Volume | 1 | Feste Menge für jeden Marktkauf oder Marktverkauf. |

## Diagrammdetails

- Die BTC-[Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) speist das Instrument in aufgebaute, abgeschlossene [Candles](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) und [Strategy trades](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html).
- Zwei nur bei gebildeten Werten arbeitende [Indicator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Blöcke berechnen SMA(8) und SMA(21). Formula-Blöcke geben die Zahlenwerte an bullische und bärische [Comparison](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcke weiter.
- Time gibt zuerst die gespeicherte Position, die festgelegte Stunde, den Nullwert, den Abkühlzustand, seinen Zeitstempel an den Hour-Converter und das feste Volumen aus. Zuletzt löst er den ausstehenden Entscheidungsspeicher aus, sodass jedes logische Gatter mit fünf Eingängen einen konsistenten Kerzenschnappschuss erhält.
- Das einmalige Flag verhindert einen Wiedereintritt während der synchronen Transaktionsverarbeitung. Vier getrennte [Modify position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke verhindern, dass ein falscher Zweig das für einen wahren Zweig bereitgestellte Volumen verbraucht.
- Die Kandidaten für geplanten Kauf und Verkauf sind `Position + 1` und `Position - 1`; beide Ausstiegskandidaten sind null. Ein Kandidat erreicht den Positionszustand nur über die Ausgabe `MyTrade` des zugehörigen Modify position.
- Der [Delay](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)-Block empfängt jede Kerze vor den Entscheidungen derselben Kerze. Ein wahres Aktionsgatter markiert die Abkühlzeit als nicht verfügbar und aktiviert N = 8, bevor es die Order sendet. Das Diagramm zeigt Kerzen, beide Durchschnitte, die ausgeführte Position, vier Aktionsausführungsströme und alle Strategieausführungen.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, setzen Sie Strategy Security auf BTCUSDT@BNBFT, wählen Sie ein Portfolio und führen Sie das Diagramm mit Dreißig-Minuten-Historie aus. Mit den enthaltenen Märzdaten und den angegebenen Werten ergab die Prüfung 59 abgeschlossene Marktorders und 59 Ausführungen ohne Transaktionsfehler. Prüfen Sie vor dem Livehandel die Zeitzone der Kerzen, das Stundenfeld, das Volumen und das Abkühlverhalten.
