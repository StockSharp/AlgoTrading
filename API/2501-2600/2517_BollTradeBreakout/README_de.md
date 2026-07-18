# BollTrade Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den originalen **BollTrade**-Expertenberater durch das Handeln von Bollinger-Band-Ausbrüchen mit einem konfigurierbaren Pip-Puffer und optionalem balancebasiertem Positionsgrößen-Management. Orders werden nur auf abgeschlossenen Kerzen eröffnet und mit festen Stop-Loss- und Take-Profit-Niveaus verwaltet.

## Konzept

- Abonniert den konfigurierbaren primären Zeitrahmen und berechnet einen Bollinger-Band-Envelope mit dem angegebenen Zeitraum und der Abweichung.
- Fügt einen zusätzlichen Versatz (`Band Offset`) gemessen in Pip-Einheiten über der oberen Band und unter der unteren Band hinzu, um vorzeitige Einstiege zu reduzieren.
- Eröffnet eine **Long**-Position, wenn der Kerzen-Schlusskurs unterhalb der unteren Band minus dem Versatz endet.
- Eröffnet eine **Short**-Position, wenn der Kerzen-Schlusskurs oberhalb der oberen Band plus dem Versatz endet.
- Zu jedem Zeitpunkt kann nur eine Position aktiv sein. Die Strategie wartet, bis der aktuelle Trade beendet ist, bevor neue Einstiege ausgewertet werden.

## Trade-Management

- Stop-Loss- und Take-Profit-Niveaus werden unmittelbar nach einem Einstieg gesetzt. Sie werden in Pip-Vielfachen ausgedrückt und bei jeder abgeschlossenen Kerze ausgewertet. Wenn der Preis eines der Niveaus berührt, wird die Position zum Marktpreis geschlossen.
- Wenn `Scale Volume` aktiviert ist, wächst (oder schrumpft) das gehandelte Volumen mit dem Kontostand. Die Skalierungsbasis ist der anfängliche Portfoliowert dividiert durch die Basislosgröße, was die ursprüngliche MQL-Implementierung imitiert. Das Volumen ist auf 500 Lots begrenzt, um das Risiko unter Kontrolle zu halten, genau wie im Quellcode.
- Die Pip-Größe wird aus dem Preisschritt des Wertpapiers abgeleitet. Für sehr kleine Schritte (Forex-artige Symbole) multipliziert der Code den Schritt mit 10, um fraktionale Pip-Schritte in Standard-Pips umzuwandeln, passend zum Verhalten der MetaTrader-Version.

## Parameter

| Name | Beschreibung | Standardwert |
| ---- | ------------ | ------------ |
| `Candle Type` | Zeitrahmen für Signal-Kerzen. | 15-Minuten-Zeitrahmen |
| `Bollinger Period` | Anzahl der Bars in der Bollinger-Band-Berechnung. | 4 |
| `Bollinger Deviation` | Breitenmultiplikator für die Bollinger Bänder. | 2 |
| `Band Offset` | Zusätzlicher Pip-Versatz, der außerhalb beider Bänder hinzugefügt wird, bevor Signale ausgelöst werden. | 3 |
| `Take Profit (pips)` | Abstand zum Gewinnziel in Pip-Einheiten. | 3 |
| `Stop Loss (pips)` | Abstand zum Schutzstop in Pip-Einheiten. | 20 |
| `Base Volume` | Standardvolumen in Lots, wenn die Skalierung deaktiviert ist. | 1 |
| `Scale Volume` | Wenn aktiviert, wird die Positionsgröße mit dem Kontostand skaliert. | Aktiviert |

## Verwendungshinweise

- Funktioniert am besten bei Forex- oder CFD-Symbolen, wo pip-basierte Versätze klare Ausbruchniveaus bieten, kann aber auch auf Futures oder Aktien ausgeführt werden, sofern ihr `PriceStep` konfiguriert ist.
- Die Strategie verarbeitet nur abgeschlossene Kerzen, daher lösen Intrabar-Spikes, die vor dem Kerzenschluss zurückkehren, keine Einstiege aus.
- Da Ausstiege mit festen Stops und Zielen gehandhabt werden, stellen Sie sicher, dass diese Abstände für den ausgewählten Zeitrahmen und die Instrumentenvolatilität angemessen sind.
- Der originale EA verwendete brokerseitige Stops. Dieser Port überwacht Kerzenextrema, um dasselbe Schutzverhalten innerhalb von StockSharp zu emulieren.

## Dateien

- `CS/BollTradeStrategy.cs` – C#-Implementierung der Strategie.
- `README.md` – englische Dokumentation (diese Datei).
- `README_ru.md` – russische Dokumentation.
- `README_zh.md` – chinesische Dokumentation.
