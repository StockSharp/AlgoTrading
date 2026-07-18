# ErrorEA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ErrorEA-Strategie** ist ein StockSharp-Port des MetaTrader-Advisors `errorEA.mq4`. Der ursprüngliche Experte verglich die +DI- und -DI-Komponenten des Average Directional Index und stapelte weiterhin Marktaufträge in der erkannten Trendrichtung, während er einen sehr hohen Sicherheits-Stop-Loss und einen engen Scalping-Take-Profit anwendete. Diese C#-Version stellt die gleiche Idee mit dem übergeordneten API von StockSharp wieder her, fügt klare Parameterkontrollen hinzu und dokumentiert das Risikomodell explizit.

## Handelslogik
1. Abonnieren Sie den konfigurierten Zeitrahmen (`CandleType`) und füttern Sie einen `AverageDirectionalIndex`-Indikator mit den eingehenden Kerzen.
2. Warten Sie, bis die Kerze vollständig geschlossen ist und ADX einen endgültigen Wert für diesen Balken erzeugt.
3. Vergleichen Sie die Zeilen +DI und -DI:
   - wenn +DI > -DI, behandelt die Strategie den Markt als bullisch;
   - wenn -DI > +DI, gilt der Markt als bärisch;
   - Gleiche Werte erzeugen keine neuen Signale.
4. Bei einem bullischen Signal:
   - eine bestehende Short-Nettoposition glätten (StockSharp verwendet Netting-Konten, sodass gegenteilige Absicherungen geschlossen werden);
   - Wenn die Anzahl der Long-Scale-in-Trades immer noch unter `MaxTrades` liegt, senden Sie eine weitere Marktkauforder mit dem vom Risikokontrollblock zurückgegebenen Volumen.
5. Bei einem bärischen Signal:
   - eine bestehende Long-Position schließen;
   - Wenn die Anzahl der Short-Tranchen unter `MaxTrades` liegt, senden Sie einen Marktverkaufsauftrag mit derselben Positionsgrößenlogik.
6. Schutzanordnungen werden verwaltet von `StartProtection`:
   - `StopLossPoints` wird in Preisschritte umgewandelt und fungiert als breiter fester Stop, genau wie die Eingabe `StopLoss` in MetaTrader;
   - Wenn `EnableTakeProfit` wahr ist, repliziert `TakeProfitPoints` das kleine Scalping-Ziel, das EA über `OrderModify` angewendet hat.
7. Positionszähler (`_longTrades`/`_shortTrades`) werden immer dann zurückgesetzt, wenn die Nettoposition auf Null zurückkehrt oder auf die entgegengesetzte Seite wechselt, um sicherzustellen, dass die Scale-In-Obergrenze bei Stop-Outs und Umkehrungen durchgesetzt wird.

## Risikomanagement und Dimensionierung
- `BaseVolume` spiegelt die `MiniLots`-Eingabe von MetaTrader wider. Sie dient als Startlosgröße für jeden Trade.
- Wenn `EnableRiskControl` wahr ist, reproduziert die Strategie die ursprüngliche `PowerRisk`-Formel: `volume = BaseVolume * max(1, PortfolioValue / RiskDivider)`. Der Standardteiler (`10000`) entspricht der MQL-Implementierung.
- Nachdem die Formel angewendet wurde, wird das Ergebnis durch `MinVolume`, `MaxVolume`, die Austauschlimits (`Security.MinVolume`, `Security.MaxVolume`) und den Volumenschritt (`Security.VolumeStep`) begrenzt. Dadurch wird verhindert, dass EA eine Größe anfordert, die der Veranstaltungsort ablehnen würde.
- Die berechnete Größe wird für jede neue Skalierungsreihenfolge verwendet, während die entsprechende Richtung innerhalb der Obergrenze `MaxTrades` bleibt.

## Parameter
| Name | Typ | Standard | MetaTrader Gegenstück | Beschreibung |
| --- | --- | --- | --- | --- |
| `AdxPeriod` | `int` | `14` | `iADX(..., 14, ...)` | Glättungszeitraum des durchschnittlichen Richtungsindex. |
| `CandleType` | `DataType` | 15-minütiger Zeitrahmen | Zeitrahmen des Diagramms | Für alle Berechnungen verwendete Kerzenreihe. |
| `MaxTrades` | `int` | `9` | `MaxTrades` | Maximale Anzahl von Scale-in-Aufträgen pro Richtung. |
| `EnableRiskControl` | `bool` | `true` | `RiskControl` | Ermöglicht die dynamische Losberechnung basierend auf dem Portfoliowert. |
| `BaseVolume` | `decimal` | `0.15` | `MiniLots` | Basislosgröße vor Anwendung des Risikomultiplikators. |
| `RiskDivider` | `decimal` | `10000` | implizit (Teiler in `PowerRisk`) | Dividendenwert, der auf den Portfoliowert angewendet wird, wenn die Risikokontrolle aktiv ist. |
| `MaxVolume` | `decimal` | `3` | `MaxLot` | Obergrenze für das automatisch berechnete Volumen (vor Börsenrundung). |
| `MinVolume` | `decimal` | `0.01` | `MarketInfo(..., MODE_MINLOT)` | In der endgültigen Bestellung zulässiges Mindestvolumen. |
| `StopLossPoints` | `int` | `1000` | `StopLoss` | Stop-Loss-Distanz in Preisschritten. Auf `0` setzen, um den Stopp zu deaktivieren. |
| `EnableTakeProfit` | `bool` | `true` | `ScalpeControl` | Ermöglicht das Tight Scalping Take-Profit. |
| `TakeProfitPoints` | `int` | `10` | `ScalpeProfit` | Take-Profit-Distanz in Preisschritten. |

## Unterschiede zum ursprünglichen Fachberater
- Die MetaTrader-Version enthielt einen Fehler, der den +DI-Wert mit dem -DI-Wert überschrieb. Der StockSharp-Port vergleicht die richtigen Komponenten und spiegelt das beabsichtigte Verhalten der Strategie wider.
- MetaTrader ermöglicht eine Absicherung. StockSharp arbeitet in einer Netting-Umgebung, sodass der Port das entgegengesetzte Risiko schließt, bevor er neue Geschäfte in Signalrichtung hinzufügt.
- Slippage-Erkennung (`GetSlippage`) und Kommentarausgabe wurden entfernt, da StockSharp Order-Slippage intern verarbeitet und die Risikozeichenfolgen rein kosmetischer Natur waren.
- Auftragsänderungen (`OrderModify`) werden durch einen einzelnen `StartProtection`-Aufruf ersetzt, der sowohl Stop-Loss- als auch Take-Profit-Abstände mit börsenbezogener Rundung abdeckt.

## Anwendungstipps
- Stellen Sie sicher, dass die Sicherheit über die richtigen `PriceStep`-, `VolumeStep`-, `MinVolume`- und `MaxVolume`-Metadaten verfügt, damit die integrierte Lautstärkeanpassung ordnungsgemäß funktionieren kann.
- Richten Sie `BaseVolume`, `MinVolume` und `MaxVolume` auf das Instrument aus, mit dem Sie handeln. Der Konstruktor weist außerdem `Strategy.Volume` das angepasste Basisvolumen zu, wodurch manuelle Aktionen in der Benutzeroberfläche mit automatisierten Bestellungen konsistent werden.
- Erhöhen Sie den Zeitrahmen oder die ADX-Periode, wenn die +DI/-DI-Signale zu verrauscht werden. Die Scale-In-Logik funktioniert bei stetigen Trends am besten.
- Deaktivieren Sie `EnableTakeProfit`, wenn Sie lieber zulassen möchten, dass der Stop-Loss die Position verlässt, anstatt kleine Gewinne zu erzielen.
