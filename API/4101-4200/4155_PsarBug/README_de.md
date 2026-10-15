# Psar-Bug-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Psar Bug Strategy** ist eine direkte Portierung des MetaTrader 4 Expert Advisors `pSAR_bug.mq4`. Es reagiert auf den allerersten Parabolic SAR-Punkt, der auf der gegenüberliegenden Seite des Preises erscheint, und kehrt die Position sofort um. Die StockSharp-Implementierung abonniert Kerzen, wertet nur abgeschlossene Balken aus und verwendet API auf hoher Ebene, um Marktaufträge zu platzieren und Schutzstopps zu verwalten.

## Handelslogik
- Berechnen Sie den Parabolic SAR mit einem Beschleunigungsschritt von `0.02` und einer maximalen Beschleunigung von `0.2` (beide konfigurierbar).
- Warten Sie auf eine fertige Kerze, bei der sich der Wert Parabolic SAR relativ zum Schluss ändert:
  - **Long-Einstieg**: Der aktuelle SAR-Wert liegt unter dem Schlusskurs, während der vorherige SAR-Wert über dem vorherigen Schlusskurs lag.
  - **Short-Einstieg**: Der aktuelle SAR-Wert liegt über dem Schlusskurs, während der vorherige SAR-Wert unter dem vorherigen Schlusskurs lag.
- Kehren Sie die bestehende Belichtung bei jedem Signal um. Wenn ein Kaufsignal erscheint, wird jede offene Short-Position abgeflacht und durch eine Long-Position der konfigurierten Größe ersetzt. Bei Verkaufssignalen verhält es sich umgekehrt.
- Wenden Sie feste Stop-Loss- und Take-Profit-Abstände an, ausgedrückt in Instrumentenpreisschritten. Der Schutz wird mit `StartProtection` implementiert, sodass die Risikoparameter automatisch an jede neue Position angehängt werden.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `TradeVolume` | Auftragsvolumen in Losen, die für Einträge verwendet werden. Der Standardwert beträgt `0.1` Lose. |
| `StopLossPoints` | Abstand vom Einstiegspreis zum Stop-Loss, ausgedrückt in Preisschritten. Spiegelt die Eingabe MetaTrader `StopLoss`. |
| `TakeProfitPoints` | Abstand vom Einstiegspreis zum Take-Profit, ausgedrückt in Preisschritten. Spiegelt die Eingabe MetaTrader `TakeProfit`. |
| `SarAccelerationStep` | Anfänglicher Beschleunigungsfaktor des Indikators Parabolic SAR. |
| `SarAccelerationMax` | Maximaler Beschleunigungsfaktor für die Parabolic SAR-Berechnung. |
| `CandleType` | Kerzendatentyp (Zeitrahmen), der für die Indikatorberechnungen verwendet wird. Standardmäßig funktioniert die Strategie bei 15-Minuten-Kerzen. |

## Hinweise zur Konvertierung
- Der ursprüngliche Experte verweist direkt auf das aktuelle Diagrammsymbol und den Zeitrahmen. Die StockSharp-Version stellt den Kerzentyp als Parameter bereit, sodass der Zeitrahmen ohne Neukompilierung geändert werden kann.
- Schutzstopps werden als absolute Preisversätze dargestellt. Sie werden einmalig beim Start initialisiert und automatisch von der Plattform verwaltet.
- Die Auftragsverwaltung basiert auf der Netting-Logik: Durch den Kauf von `Volume + |Position|`-Lots wird sowohl der vorherige Short geschlossen als auch der neue Long eröffnet, wodurch das MetaTrader-Verhalten des Schließens vor dem Öffnen in die entgegengesetzte Richtung reproduziert wird.

## Nutzung
1. Konfigurieren Sie die gewünschten Sicherheits-, Zeitrahmen- (`CandleType`) und Risikoparameter im StockSharp Designer oder Backtester.
2. Stellen Sie sicher, dass Marktdaten verfügbar sind, und starten Sie die Strategie. Signale werden nur an fertigen Kerzen ausgewertet.
3. Überwachen Sie Positionen und Leistung mit den Standardtools von StockSharp. Die Diagramme zeigen Kerzen, den Indikator Parabolic SAR und ausgeführte Trades zur visuellen Validierung der Umkehrsignale.
