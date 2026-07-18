# Serielle MA Swing Strategie (API/2782)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Konvertiert den MetaTrader SerialMA Expert Advisor in eine StockSharp High-Level-Strategie mithilfe von Kerzenabonnements und einem benutzerdefinierten seriellen gleitenden Durchschnittsindikator.
- Eröffnet neue Swing-Positionen immer dann, wenn der serielle gleitende Durchschnitt seine Richtung relativ zum Preis dreht, mit optionaler Signalumkehr und Begrenzung der Anzahl gleichzeitiger Swings.
- Implementiert dieselben schützenden Stop-Loss- und Take-Profit-Abstände, gemessen in Instrumentenpunkten, die bei jeder abgeschlossenen Kerze neu berechnet werden.

## Serieller Gleitender Durchschnittsindikator
Der ursprüngliche EA hängt vom benutzerdefinierten *SerialMA*-Indikator ab, der seinen gleitenden Durchschnitt nach jedem Preiskreuzungspunkt neu aufbaut. Der portierte Indikator repliziert dieses Verhalten durch:
1. Akkumulieren von Schlusskursen seit dem letzten Kreuzungspunkt und Berechnen ihres arithmetischen Mittels.
2. Verfolgen der Differenz zwischen dem Mittelwert und dem aktuellen Schluss, um eine Vorzeichenänderung zu erkennen.
3. Zurücksetzen des internen Fensters, wenn sich das Vorzeichen ändert, wodurch der Durchschnitt effektiv von der Kreuzungsleiste neu gestartet wird und das Ereignis für die Strategie signalisiert wird.

Diese Implementierung stellt den gleitenden Durchschnittswert zusammen mit einem booleschen Flag bereit, das angibt, dass ein Kreuzungspunkt auf dem vorherigen Balken aufgetreten ist, was es der Strategie ermöglicht, die MQL-Logik ohne manuellen Pufferzugriff zu spiegeln.

## Handelslogik
1. Bei jeder abgeschlossenen Kerze liest die Strategie den seriellen gleitenden Durchschnittswert und das Kreuzungs-Flag.
2. Wenn die vorherige Kerze einen Kreuzungspunkt ausgelöst hat:
   - Wenn der vorherige Schluss über dem vorherigen gleitenden Durchschnitt lag, wird ein Long-Signal generiert.
   - Wenn der vorherige Schluss unter dem vorherigen gleitenden Durchschnitt lag, wird ein Short-Signal generiert.
3. Der Parameter **ReverseSignals** tauscht optional Long- und Short-Einstiege.
4. Der Parameter **OpenedMode** steuert das Positionsstapeln:
   - **AllSwing** öffnet bei jedem Signal eine neue Order, auch wenn bereits eine Position in dieser Richtung besteht.
   - **SingleSwing** öffnet nur dann eine neue Order, wenn keine Exposition in dieser Richtung besteht.
5. Vor dem Einreichen einer neuen Order schließt die Strategie immer das bestehende Engagement in der entgegengesetzten Richtung, um die Swing-Logik konsistent mit dem Quell-EA zu halten.
6. Stop-Loss- und Take-Profit-Abstände werden bei jeder Kerze unter Verwendung des Instrumentenpreisschritts angewendet, was den punktbasierten Risikokontrollen des ursprünglichen Experten entspricht.

## Parameter
| Name | Beschreibung | Standardwert |
| --- | --- | --- |
| `OpenedMode` | Ermöglicht entweder das Stapeln von Swings oder das Halten eines einzelnen Swings pro Richtung. | `AllSwing` |
| `EnableBuy` | Aktiviert oder deaktiviert Long-Einstiege. | `true` |
| `EnableSell` | Aktiviert oder deaktiviert Short-Einstiege. | `true` |
| `ReverseSignals` | Kehrt die Handelsrichtung um. | `false` |
| `TradeVolume` | Ordergröße (Lots) für jeden neuen Swing. | `1` |
| `StopLossPoints` | Stop-Loss-Abstand in Preisschritten (Punkten). Ein Wert von `0` deaktiviert den Stop. | `0` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten (Punkten). Ein Wert von `0` deaktiviert den Take-Profit. | `0` |
| `CandleType` | Kerzendatentyp für Berechnungen. | `5-Minuten-Kerzen` |

## Orderverwaltung und Schutz
- Bei Long-Positionen prüft die Strategie, ob das Kerzentief das Stop-Loss-Niveau verletzt oder das Kerzenhoch das Gewinnziel erreicht hat, und gibt entsprechend eine Marktorder zum Glätten aus.
- Bei Short-Positionen löst das Kerzenhoch den Stop-Loss aus und das Kerzentief löst das Gewinnziel aus.
- Schutzniveaus werden in `PriceStep`-Einheiten gemessen. Wenn das Instrument keinen Preisschritt bereitstellt, bleiben die Schutzprüfungen inaktiv, was das Verhalten bei fehlenden Tick-Size-Informationen widerspiegelt.

## Verwendungshinweise
- Die Implementierung verwendet die StockSharp High-Level-API (`SubscribeCandles` + `BindEx`) und vermeidet Low-Level-Pufferverwaltung.
- Es ist keine Python-Version enthalten, wie angefordert. Nur der C#-Port befindet sich in `CS/SerialMASwingStrategy.cs`.
- Die Strategie ist für Swing-artige Ausführung ähnlich dem ursprünglichen EA gedacht; das Aktivieren beider Richtungen und das Beibehalten des Standard-`AllSwing`-Modus ähnelt am ehesten dem MQL-Verhalten.
