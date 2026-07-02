# Parabolic SAR Bug-2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Parabolic SAR Bug 2 Strategy** ist die StockSharp High-Level-Konvertierung des MetaTrader Expert Advisors `pSAR_bug2` aus dem Ordner `MQL/9503`. Der ursprüngliche EA reagiert auf den allerersten Parabolic SAR-Punkt, der auf der gegenüberliegenden Seite des Preises erscheint. Wenn der Punkt unter den Schlusskurs fällt, schließt das System alle Short-Trades und eröffnet sofort eine Long-Position; Wenn der Punkt über den Schlusskurs springt, spiegelt die Logik das Verhalten auf der Short-Seite wider. Schützende Stop-Loss- und Take-Profit-Level werden in Rohpreispunkten berechnet, genau wie in MetaTrader, wo die Werte mit der Instrumentengröße `Point` multipliziert werden.

Der StockSharp-Port behält die gleiche Absicht bei und nutzt gleichzeitig das übergeordnete API des Frameworks. Es abonniert fertige Kerzen, bindet einen Parabolic SAR-Indikator mit konfigurierbaren Beschleunigungsparametern, überwacht Punktumkehrungen und sendet Marktaufträge in der Größe, um sowohl das vorherige Engagement zu glätten als auch den neuen Handel einzurichten.

## Handelslogik
1. **Indikatorvorbereitung**. Die Strategie abonniert einen benutzerdefinierten Kerzentyp (standardmäßig 15-Minuten-Zeitrahmen) und bindet einen Parabolic SAR mit Beschleunigungsschritt `SarStep` und maximaler Beschleunigung `SarMaximum`.
2. **Statusverfolgung**. Bei der ersten abgeschlossenen Kerze zeichnet der Algorithmus auf, ob der SAR-Wert über oder unter dem Schlusskurs liegt. Jede neue Kerze vergleicht die neue SAR-Position mit dem zuvor gespeicherten Zustand.
3. **Eintrittsregeln**.
   - **Long-Einstieg**: Wird ausgelöst, wenn sich der SAR von über dem Schlusskurs auf unter den Schlusskurs bewegt. Das Auftragsvolumen wird als `TradeVolume + |Position|` berechnet, sodass eine bestehende Short-Position in einer einzigen Marktorder geschlossen und rückgängig gemacht wird. Nach dem Einstieg werden Stop-Loss- und Take-Profit-Level im Verhältnis zum Kerzenschluss gespeichert.
   - **Short-Einstieg**: wird ausgelöst, wenn sich der SAR von unter dem Schlusskurs auf über dem Schlusskurs bewegt. Jede bestehende Long-Position wird abgeflacht und am Markt wird ein neuer Short-Trade mit derselben kombinierten Größenformel eingegeben.
4. **Schutzausgänge**. Bei jeder abgeschlossenen Kerze werden die gespeicherten Stop-Loss- und Take-Profit-Level mit dem Hoch/Tief verglichen. Wenn der Preis ein Schutzniveau durchbricht, sendet die Strategie einen Marktauftrag zum Schließen der offenen Position und setzt die zwischengespeicherten Stop- und Take-Werte zurück.

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände werden in Rohpreispunkten berechnet, indem der konfigurierte `StopLossPoints` oder `TakeProfitPoints` mit der Wertpapierpreisstufe multipliziert wird. Ein konservativer Fallback von `0.0001` wird verwendet, wenn das Instrument keinen Preisschritt veröffentlicht.
- Die Strategie prüft `IsFormedAndOnlineAndAllowTrading()` vor der Übermittlung von Aufträgen und stellt so sicher, dass die Marktdaten online sind und der Handel zulässig ist.
- Umkehreingaben beinhalten immer die absolute aktuelle Positionsgröße und stellen so sicher, dass die neue Order das vorherige Risiko abmildert, bevor der gegenteilige Trade etabliert wird.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Basisauftragsvolumen in Losen. Der gleiche Wert wird der internen Eigenschaft `Strategy.Volume` zugewiesen. |
| `StopLossPoints` | `90` | Stop-Loss-Distanz in Preispunkten. Der Abstand wird mit dem Preisschritt des Instruments multipliziert, um den tatsächlichen Preisversatz zu erhalten. |
| `TakeProfitPoints` | `20` | Take-Profit-Distanz in Preispunkten, umgerechnet durch die Preisstufe des Instruments. |
| `SarStep` | `0.001` | Anfänglicher Beschleunigungsfaktor für den Indikator Parabolic SAR. |
| `SarMaximum` | `0.2` | Maximaler Beschleunigungsfaktor für den Indikator Parabolic SAR. |
| `CandleType` | `15m time-frame` | Kerzentyp, der für Berechnungen und Signalauswertung verwendet wird. |

## Hinweise zur Konvertierung
- Die maklerseitigen Stop-Loss- und Take-Profit-Orders von MetaTrader werden durch die Überwachung von Candle-Extremen und die Übermittlung von Marktausstiegen nachgeahmt, wenn die Schwellenwerte überschritten werden.
- Die MetaTrader EA erforderten eine manuelle Verwaltung von `OrdersTotal()` und expliziten `OrderClose()`-Aufrufen. Die StockSharp-Version erreicht das gleiche Verhalten, indem sie eine einzelne Marktorder mit der Größe `TradeVolume + |Position|` sendet, die gleichzeitig jede entgegengesetzte Position schließt und die neue eröffnet.
- Es wird keine Python-Implementierung bereitgestellt, die der Aufgabenanforderung entspricht. Der Ordner enthält derzeit nur die C#-Version der Strategie.
