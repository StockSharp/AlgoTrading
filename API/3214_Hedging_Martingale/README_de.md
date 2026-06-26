# Hedging Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein StockSharp-Port des MetaTrader-Expertenberaters "Hedging Martingale" (Ordner `MQL/23693`). Sie hält eine ausgeglichene Hedge aufrecht, indem sie bei jeder neuen Bar sowohl eine Long- als auch eine Short-Position eröffnet, und wendet dann ein Martingal-Averaging-Schema an. Wenn sich der Preis um eine konfigurierbare Pip-Distanz ungünstig bewegt, fügt die Strategie eine neue Position auf der Verlustseite mit erhöhtem Volumen hinzu, während die entgegengesetzte Hedge bestehen bleibt. Der schwebende Gewinn wird mit geld- und prozentbasierten Zielen zusammen mit einer optionalen Trailing-Sperre verwaltet.

## Trading-Logik
- **Anfängliche Hedge**: Wann immer die Strategie flat ist und eine neue Kerze schließt, kauft und verkauft sie gleichzeitig mit demselben Basisvolumen.
- **Martingal-Schritte**: Wenn sich der Preis um `Pip Step` Pips gegen eine Seite bewegt, wird auf dieser Seite eine zusätzliche Order eröffnet. Das Volumen wird mit dem `Volume Multiplier` multipliziert, was die progressive Lot-Größenbestimmung aus der MQL-Version emuliert. Die entgegengesetzte Seite bleibt offen, um die Hedge aufrechtzuerhalten.
- **Take-Profit pro Trade**: Jeder offene Eintrag hat eine individuelle Take-Profit-Distanz, die durch `Take Profit (pips)` definiert ist. Wenn sich der Markt um diese Distanz zugunsten eines Beins bewegt, wird das Bein durch eine Gegenbuchung reduziert.
- **Korb-Ausstiege**: Das gesamte Positionsset kann geschlossen werden, wenn der schwebende Gewinn ein Geldziel, einen Prozentsatz des Anfangskapitals erreicht oder nachdem eine Trailing-Sperre mehr als den erlaubten Rückzug zurückgibt. Diese Verhaltensweisen replizieren `Take_Profit_In_Money`, `Take_Profit_In_percent` und `TRAIL_PROFIT_IN_MONEY2` aus dem ursprünglichen Experten.
- **Trade-Limits**: Der Parameter `Max Trades` beschränkt die Anzahl der aktiven Martingal-Schritte. Wenn `Close On Max` aktiviert ist, wird der Korb liquidiert, sobald das Limit überschritten wird.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| Candle Type | Zeitrahmen, der die Logik antreibt. Jede abgeschlossene Kerze kann neue Hedging-Aktionen auslösen. |
| Use Money TP / Money Take Profit | Aktivieren und den schwebenden Gewinn (in Währungseinheiten) definieren, der alle Positionen schließt. |
| Use Percent TP / Percent Take Profit | Korb schließen, wenn der schwebende Gewinn einen Prozentsatz des anfänglichen Portfoliowerts erreicht. |
| Enable Trailing / Trailing Start / Trailing Step | Geldbasierte Trailing-Sperre für den Korb aktivieren und Triggerniveau zusammen mit dem erlaubten Gewinnrückzug konfigurieren. |
| Take Profit (pips) | Distanz in Pips für Take-Profit-Ausstiege pro Bein. |
| Pip Step | Ungünstige Preisbewegung (in Pips) erforderlich, bevor eine weitere Martingal-Order hinzugefügt wird. |
| Base Volume | Anfangsvolumen für beide Buy- und Sell-Beine. |
| Volume Multiplier | Multiplikator beim größten Positionsvolumen, wenn Martingal-Einstiege hinzugefügt werden. |
| Max Trades | Maximale Anzahl gleichzeitig offener Einstiege (in beiden Richtungen). |
| Close On Max | Ob alle Positionen liquidiert werden sollen, sobald die maximale Trade-Anzahl überschritten wird. |

## Hinweise
- Die Strategie verwendet `BuyMarket` und `SellMarket` für alle Orderplatzierungen und spiegelt das Market-Execution-Modell des Quell-Experten wider.
- Volumenwerte werden auf den Lot-Schritt des Instruments normalisiert, um abgelehnte Orders zu vermeiden.
- Wenn die Strategie flat wird, wird die Trailing-Sperre zurückgesetzt, damit neue Körbe mit einer sauberen Gewinnreferenz beginnen.

## Dateien
- `CS/HedgingMartingaleStrategy.cs` – Implementierung der konvertierten Strategie (C#).
- `README.md` – diese Dokumentation (Englisch).
- `README_zh.md` – Chinesische Übersetzung.
- `README_ru.md` – Russische Übersetzung.
