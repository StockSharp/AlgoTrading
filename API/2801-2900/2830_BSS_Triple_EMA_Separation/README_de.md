# BSS Triple EMA-Separations-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **BSS Triple EMA-Separations-Strategie** ist ein StockSharp-Port des MetaTrader 5-Expert Advisors "BSS 1_0" (MQL ID 20591). Der Ansatz überwacht drei gleitende Durchschnitte mit wachsenden Rückblickfenstern und wartet darauf, dass sie sich um mindestens eine konfigurierbare Distanz ausbreiten. Wenn die schnellen, mittleren und langsamen Durchschnitte ordnungsgemäß separiert sind, steigt die Strategie in Trendrichtung ein, wobei eine Abklingzeit zwischen Fills und eine Obergrenze für die Gesamtpositionsgröße eingehalten werden.

Diese Implementierung behält das Kernverhalten des ursprünglichen Roboters bei und exponiert die Konfiguration über StockSharp-`StrategyParam`-Objekte. Alle Kommentare und die Dokumentation sind auf Englisch geschrieben, wie angefordert.

## Handelslogik

1. Ein einzelner Kerzenstrom abonniert, der durch den `CandleType`-Parameter definiert ist, und drei gleitende Durchschnitte berechnen (schnell, mittel, langsam). Jeder Durchschnitt kann eine andere Glättungsmethode verwenden (einfach, exponentiell, geglättet oder linear gewichtet).
2. Für ein **Long-Setup** müssen folgende Bedingungen auf einer abgeschlossenen Kerze erfüllt sein:
   - `Langsamer MA - Mittlerer MA >= MinimumDistance`.
   - `Mittlerer MA - Schneller MA >= MinimumDistance`.
3. Für ein **Short-Setup** ist die inverse Separation erforderlich:
   - `Schneller MA - Mittlerer MA >= MinimumDistance`.
   - `Mittlerer MA - Langsamer MA >= MinimumDistance`.
4. Vor dem Öffnen eines Trades stellt die Strategie sicher:
   - Alle Indikatoren sind vollständig gebildet und die Strategie kann handeln (`IsFormedAndOnlineAndAllowTrading`).
   - Die Pause seit dem letzten Einstieg (`MinimumPauseSeconds`) ist abgelaufen.
   - Das Hinzufügen eines neuen Lots verletzt nicht das Expositionslimit `MaxPositions`.
5. Bei einem Einstiegssignal schließt die Strategie zunächst jede offene Position in der entgegengesetzten Richtung. Erst nach der nächsten Kerze erwägt sie das Öffnen einer Position in der neuen Richtung, was das Verhalten des ursprünglichen MQL-EA widerspiegelt.
6. Wenn eine neue Position eröffnet oder skaliert wird, wird die Fill-Zeit gespeichert, um die Abklingzeit zwischen Einstiegen zu erzwingen.

Es werden keine automatischen Stop-Loss- oder Take-Profit-Level verwendet. Das Risikomanagement wird durch den Distanzfilter, die Pause zwischen Trades und die maximale Anzahl erlaubter Lots pro Richtung erreicht.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OrderVolume` | 0.1 | Volumen für jede Einstiegsorder. Die Nettoposition ist auf `OrderVolume * MaxPositions` begrenzt. |
| `MaxPositions` | 2 | Maximale Anzahl von Lots (pro Richtung), die gleichzeitig gehalten werden können. |
| `MinimumDistance` | 0.0005 | Minimale Preislücke zwischen benachbarten gleitenden Durchschnitten. Wählen Sie einen für das Instrument geeigneten Wert (für ein 5-stelliges FX-Paar entsprechen 0,0005 5 Pips). |
| `MinimumPauseSeconds` | 600 | Abklingzeit in Sekunden zwischen neuen Einstiegen. Das Schließen von Trades setzt den Timer nicht zurück; nur Einstiege tun es. |
| `FirstMaPeriod` | 5 | Periode des schnellsten gleitenden Durchschnitts. Muss strikt kleiner als `SecondMaPeriod` sein. |
| `FirstMaMethod` | Exponential | Für den schnellen gleitenden Durchschnitt verwendete Glättungsmethode (Simple, Exponential, Smoothed, LinearWeighted). |
| `SecondMaPeriod` | 25 | Periode des mittleren gleitenden Durchschnitts. Muss strikt kleiner als `ThirdMaPeriod` sein. |
| `SecondMaMethod` | Exponential | Für den mittleren gleitenden Durchschnitt verwendete Glättungsmethode. |
| `ThirdMaPeriod` | 125 | Periode des langsamen gleitenden Durchschnitts. |
| `ThirdMaMethod` | Exponential | Für den langsamen gleitenden Durchschnitt verwendete Glättungsmethode. |
| `CandleType` | 1-Minuten-Zeitrahmen | Kerzendatenquelle für Indikatorberechnungen und Signalbewertung. |

## Implementierungshinweise

- Die StockSharp-High-Level-API wird verwendet: `SubscribeCandles` streamt Daten, und `.Bind` speist die gleitenden Durchschnitte und den Signalhandler gleichzeitig.
- Die gleitenden Durchschnitte werden beim Strategiestart entsprechend den ausgewählten Methoden instanziiert. Die Standardkonfiguration entspricht dem ursprünglichen EA (drei exponentielle MAs auf Schlusskursen).
- `StartProtection()` wird aufgerufen, um die von StockSharp bereitgestellten integrierten Positionsüberwachungstools zu aktivieren.
- Die Strategie überschreibt `OnPositionChanged`, um Einstiege mit Zeitstempeln zu versehen. Dieser Zeitstempel wird mit `MinimumPauseSeconds` verglichen, um das Abklingverhalten der MetaTrader-Version beizubehalten.
- Entgegengesetzte Positionen werden ausgeglichen, bevor neue berücksichtigt werden, was sicherstellt, dass die Nettoexposition nie ohne vorherigen Durchgang durch null das Vorzeichen wechselt, genau wie bei der ursprünglichen Implementierung, bei der alle Short-Positionen geschlossen wurden, bevor Longs eröffnet wurden.

## Nutzungsrichtlinien

1. Wählen Sie ein Instrument und stellen Sie sicher, dass seine Tick-Größe im `MinimumDistance`-Wert widergespiegelt ist. Beispielsweise:
   - EURUSD (5-stellige Preisgestaltung): `0,0005` entspricht 5 Pips.
   - USDJPY (3-stellige Preisgestaltung): `0,05` entspricht 5 Pips.
2. Passen Sie die gleitenden Durchschnittsperioden und -methoden an das Marktregime an, das Sie ansprechen möchten.
3. Erhöhen Sie `MinimumPauseSeconds` bei langsameren Zeitrahmen, um Überhandel zu vermeiden, oder verringern Sie es bei niedrigeren Zeitrahmen, wenn die Marktstruktur häufige Einstiege erlaubt.
4. Testen Sie verschiedene `MaxPositions`-Werte in Kombination mit der Kontraktgröße Ihres Brokers, um die Exposition mit Ihrem Risikoplan abzustimmen.

## Einschränkungen im Vergleich zur MQL-Version

- Der MetaTrader-Experte erlaubte die Auswahl alternativer Preisquellen (Eröffnung, Hoch, Tief usw.). Der StockSharp-Port arbeitet derzeit nur auf Schlusskursen, was der Standardkonfiguration des ursprünglichen Roboters entspricht.
- Der Port verwendet ein Nettopositoinsmodell (positiv für Longs, negativ für Shorts). Wenn `MaxPositions` erreicht ist, werden keine zusätzlichen Lots hinzugefügt, bis die Exposition reduziert wird, was die Wirkung des ursprünglichen Pro-Position-Zählers reproduziert.

Mit diesen Überlegungen können Sie das Verhalten der ursprünglichen BSS-Strategie innerhalb des StockSharp-Ökosystems reproduzieren und sie bei Bedarf mit zusätzlichen Risikokontrollen oder Analysen erweitern.
