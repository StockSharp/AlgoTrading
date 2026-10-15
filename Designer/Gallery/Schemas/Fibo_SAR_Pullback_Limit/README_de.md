# Fibonacci-SAR-Rücklauf mit Limitorder
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm verbindet zwei Geschwindigkeiten des Parabolic SAR mit einer Drei-Kerzen-Spanne, platziert jeweils nur eine Fibonacci-Rücklauf-Limitorder, storniert die wartende Order bei einer Umkehr des Setups und schließt eine ausgeführte Position an den beim Einstieg gespeicherten Spannenniveaus.

![schema](schema.svg)

## Strategieübersicht

- Abgeschlossene einstündige BTCUSDT-Kerzen speisen den schnellen und langsamen Parabolic SAR sowie Highest(3) und Lowest(3). Entscheidungen beginnen erst, wenn alle Indikatoren gebildet sind.
- Die gebildete Lowest-Ausgabe gibt einen Entscheidungszyklus frei, nachdem Close, beide SAR-Werte, Hoch, Tief, Position und Status der wartenden Order der aktuellen Kerze erfasst wurden.
- Eine globale Sperre erlaubt nur eine aktive Einstiegsorder. Sie wird im Endzustand der Order gelöst; nach einer Ausführung verhindert die abgetastete Position einen weiteren Einstieg.
- Einstiegs-, Storno- und Ausstiegsbedingungen werden in stillen Ergebnisvariablen gespeichert und einmal je abgeschlossener Kerze freigegeben, sodass Werte benachbarter Kerzen nicht vermischt werden.
- Das Diagramm zeigt Kerzen, beide SAR-Reihen, Spanne und gespeicherte Schutzmarken, registrierte und stornierte Limits, Marktausstiege und alle Ausführungen.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn `Slow SAR < Fast SAR < Close`, die Position leer ist und kein Einstieg wartet, wird eine Buy-Limitorder bei `Low3 + (High3 - Low3) * 50%` gesendet. Vor ihrer Ausführung wird sie storniert, falls `Slow SAR > Fast SAR` oder `Fast SAR >= Close` gilt.
- **Short-Einstieg**: Wenn `Slow SAR > Fast SAR > Close`, die Position leer ist und kein Einstieg wartet, wird eine Sell-Limitorder bei `High3 - (High3 - Low3) * 50%` gesendet. Vor ihrer Ausführung wird sie storniert, falls `Slow SAR < Fast SAR` oder `Fast SAR <= Close` gilt.
- **Ausstieg**: Beim Akzeptieren des Einstiegssignals werden die Niveaus der jeweiligen Seite gespeichert. Long verwendet den Stop `Low3 - 30` und das Ziel `Low3 + (High3 - Low3) * 161%`; Short verwendet den Stop `High3 + 30` und das Ziel `High3 - (High3 - Low3) * 161%`. Erreicht ein abgeschlossener Close eine gespeicherte Marke, wird eine entgegengesetzte Marketorder mit Volumen 1 gesendet.

## Parameter

| Parameter | Standardwert | Beschreibung |
|---|---|---|
| Security | BTCUSDT@BNBFT | Instrument für das Abonnement abgeschlossener Kerzen. Strategy Security muss für Orders und Ausführungen dasselbe Instrument verwenden. |
| Candle Series | 01:00:00 | Abgeschlossene einstündige Kerzen für Indikatoren, Entscheidungen, Ausstiege und Diagramm. |
| Fast SAR Acceleration | 0.02 | Anfangsbeschleunigung des schnellen Parabolic SAR. |
| Fast SAR Increment | 0.02 | Beschleunigungszuwachs des schnellen Parabolic SAR. |
| Fast SAR Maximum | 0.20 | Maximale Beschleunigung des schnellen Parabolic SAR. |
| Slow SAR Acceleration | 0.01 | Anfangsbeschleunigung des langsamen Parabolic SAR. |
| Slow SAR Increment | 0.02 | Beschleunigungszuwachs des langsamen Parabolic SAR. |
| Slow SAR Maximum | 0.10 | Maximale Beschleunigung des langsamen Parabolic SAR. |
| High Lookback | 3 | Anzahl abgeschlossener Kerzen, die Highest für `High3` verwendet. |
| Low Lookback | 3 | Anzahl abgeschlossener Kerzen, die Lowest für `Low3` verwendet. |
| Entry Fibonacci, % | 50 | Lage des Limitpreises innerhalb der aktuellen Drei-Kerzen-Spanne. |
| Target Fibonacci, % | 161 | Spannenmultiplikator für jedes gespeicherte Gewinnziel. |
| Stop Offset | 30 | Absoluter Preisabstand außerhalb des Drei-Kerzen-Tiefs oder -Hochs für den gespeicherten Stop. |
| Order Volume | 1 | Menge jedes Einstiegs und jedes durch die Positionsseite gesicherten Marktausstiegs. |

## Diagrammdetails

- Die Security-[Variable](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) konfiguriert abgeschlossene [Candles](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html); Transaktionsblöcke verwenden Strategy Security und Strategy Portfolio.
- Vier nur gebildete Werte ausgebende [Indicator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Blöcke berechnen beide Parabolic-SAR-Werte sowie getrennt das Hoch und Tief von drei Kerzen. Die Lowest-Ausgabe ist der gemeinsame Zyklustakt.
- [Formula](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/formula.html)-, Variable- und [Comparison](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Blöcke richten numerische Eingaben aus, wenden Leerpositions- und Pending-Seiten-Sperren an und geben nur wahre Aktionsimpulse aus.
- Jedes akzeptierte Signal speichert den berechneten Stop und das Ziel, bevor sein [Order registering](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/orders/register.html)-Block ausgelöst wird. Die gespeicherten Werte bewegen sich bei offener Position nicht.
- Die Referenz der registrierten Order bleibt für eine gezielte [Order cancellation](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html) gespeichert. Die Pending-Sperre wird nur durch das Finished-Ereignis des Registrierungsblocks nach Ausführung, bestätigter Stornierung oder Registrierungsfehler gelöst.
- Seitengesicherte [Modify position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Blöcke senden eine feste entgegengesetzte Marketorder von einer Einheit, wenn der abgeschlossene Close einen gespeicherten Stop oder ein Ziel erreicht. Das Diagramm empfängt alle relevanten Preis-, Order-, Storno- und MyTrade-Ströme.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, setzen Sie Strategy Security auf BTCUSDT@BNBFT und führen Sie sie mit einstündiger Historie aus. Prüfen Sie Preisskala, Fibonacci-Niveaus, Stop-Abstand, Order-Lebenszyklus und Marktausstieg, bevor Sie das Diagramm im Live-Handel einsetzen.
