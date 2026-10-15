# E-Skoch-Open-Strategie (StockSharp-Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **E-Skoch-Open**-Strategie repliziert den ursprünglichen MetaTrader 5 Expert Advisor, der ein einfaches Drei-Kerzen-Muster handelt. Die StockSharp-Implementierung verarbeitet abgeschlossene Kerzen, wertet Momentum-Umkehrungen in den jüngsten Schlusskursen aus und öffnet eine neue Position, wenn die erforderliche Konfiguration erscheint. Das Risiko wird durch Stop-Loss/Take-Profit-Abstände gesteuert, die in angepassten Punkten (Pips) gemessen werden, sowie durch ein Eigenkapitalwachstumsziel, das alle offenen Positionen flachlegen kann. Die Positionsgrößenbestimmung folgt einem Martingale-Schema: Nach einem Verlusttrade wird die nächste Ordergröße mit 1.6 multipliziert, während profitable Trades das Volumen auf den Anfangswert zurücksetzen.

## Handelslogik
1. Arbeitet mit dem durch den Parameter `CandleType` definierten Zeitrahmen (Standard: 1 Stunde).
2. Wartet, bis mindestens drei abgeschlossene Kerzen verfügbar sind.
3. **Kauf-Setup**: wenn `Close[n-3] > Close[n-2]` und `Close[n-1] < Close[n-2]`, und Long-Trades aktiviert sind.
4. **Verkauf-Setup**: wenn `Close[n-3] > Close[n-2]` und `Close[n-2] < Close[n-1]`, und Short-Trades aktiviert sind.
5. Wenn `CloseOnOppositeSignal` aktiviert ist, schließt ein entgegengesetztes Signal die bestehende Position sofort und überspringt neue Einträge für die aktuelle Bar.
6. Für jede neue Position fügt die Strategie statische Stop-Loss- und Take-Profit-Level hinzu, die vom aktuellen Schlusskurs und dem konfigurierten Abstand in angepassten Punkten berechnet werden. Wenn das Hoch/Tief einer abgeschlossenen Kerze eines dieser Niveaus erreicht, wird die Position geschlossen.
7. Die Strategie prüft kontinuierlich das Kontokapital. Wenn das Kapitalwachstum relativ zum letzten Flat-Moment `TargetProfitPercent` übersteigt, werden alle Positionen geschlossen.
8. Nachdem ein Trade mit Verlust schließt, wird das nächste Ordervolumen mit 1.6 multipliziert. Nach einem profitablen Trade kehrt das Volumen zur anfänglichen Größe zurück. Volumen werden unter Verwendung der Instrumentbeschränkungen normalisiert (`VolumeStep`, `VolumeMin`, `VolumeMax`).

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Für die Mustererkennung verwendeter Zeitrahmen. Funktioniert mit allen von StockSharp unterstützten Kerzen. |
| `InitialOrderVolume` | Basis-Losgröße für den ersten Trade in einer Sequenz (Standard: 0.01). |
| `StopLossPoints` | Stop-Loss-Abstand ausgedrückt in angepassten Punkten. Bei 5-stelligen oder 3-stelligen Instrumenten beträgt der Punktwert `PriceStep * 10`, sonst `PriceStep`. |
| `TakeProfitPoints` | Take-Profit-Abstand unter Verwendung derselben angepassten Punktkonvention. |
| `EnableBuySignals` / `EnableSellSignals` | Long- oder Short-Einträge umschalten. |
| `MaxBuyTrades` / `MaxSellTrades` | Maximale Anzahl aufeinanderfolgender Trades pro Richtung (`-1` entfernt das Limit). Der Port hält standardmäßig höchstens eine Position pro Richtung. |
| `TargetProfitPercent` | Kapitalgewinn-Prozentsatz, der das Schließen aller Positionen auslöst (Standard: 1.2%). |
| `CloseOnOppositeSignal` | Wenn aktiviert, zwingt ein Signal in die entgegengesetzte Richtung zu einer Flat-Position, bevor neue Trades in Betracht gezogen werden. |

## Risikohinweise
- Stop-Loss- und Take-Profit-Level werden aus Kerzenhochs/-tiefs simuliert. Im Live-Handel kann die Intrabar-Ausführung von MetaTrader abweichen, wo Schutzorders auf dem Server registriert sind.
- Der Martingale-Multiplikator (1.6) kann Volumen während Drawdowns schnell anwachsen lassen. Sicherstellen, dass die Instrumentgrenzen (`VolumeMax`) und das Portfoliokapital die größte erwartete Position unterstützen können.
- Gewinnsperre auf Kapitalbasis funktioniert nur, wenn Portfolio-Informationen über `Portfolio.CurrentValue` verfügbar sind.

## Verwendungstipps
- `CandleType` anpassen, um dem im ursprünglichen Expert Advisor verwendeten Zeitrahmen zu entsprechen.
- `StopLossPoints` / `TakeProfitPoints` an die Instrumentvolatilität anpassen; sie sind pip-basiert dank der angepassten Punktberechnung.
- Eine Richtung deaktivieren, wenn Hedging vom Broker oder der Risikorichtlinie nicht erlaubt ist.
- Bei langen Tests die Kapitalzielsetzung und Martingale-Einstellungen im Auge behalten, um unerwartete Liquidationen zu vermeiden.
