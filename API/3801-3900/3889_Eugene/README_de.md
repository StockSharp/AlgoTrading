# Eugene-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung

Die Eugene-Strategie portiert den ursprünglichen MetaTrader 4-Expertenberater „Eugene“ auf die StockSharp-Hochebene API. Der Algorithmus überwacht standardmäßig stündliche Kerzen und sucht nach Ausbrüchen von Innenkerzen, die durch ein Retracement auf ein Drittel der vorherigen Kerze bestätigt werden. Sobald ein Ausbruch bestätigt ist, geht die Strategie in die Ausbruchsrichtung über und kann bestehende Positionen umkehren, wenn ein entgegengesetztes Setup auftritt.

## Handelslogik

1. **Erkennung innerhalb der Kerze** – die vorherige Kerze muss vollständig innerhalb der Reichweite der Kerze davor liegen. Seine Schlussrichtung bestimmt, ob er als schwarzer (bärischer) oder weißer (bullischer) Insider klassifiziert wird.
2. **Vogelfilter** – eine innere Kerze, die durch eine andere Kerze derselben Farbe dahinter bestätigt wird, wird als „Vogel“ markiert. Schwarze Vögel blockieren Long-Trades, weiße Vögel blockieren Short-Trades. Dies spiegelt den Schutzfilter der MQL-Version wider.
3. **Zickzack-Bestätigungsniveaus** – zwei Bestätigungspreise werden bei einem Drittel des vorherigen Kerzenkörpers oder Dochts berechnet:
   - Das lange Bestätigungsniveau liegt ein Drittel unter dem vorherigen Schlusskurs (Körper für bullische Kerzen, Docht für bärische Kerzen).
   - Das Short-Bestätigungsniveau liegt ein Drittel über dem vorherigen Schlusskurs (Körper für bärische Kerzen, Docht für bullische Kerzen).
4. **Sitzungsfilter** – wenn die aktuelle Kerze um 08:00 Uhr oder später öffnet, gelten Bestätigungen auch ohne Retracement als erfüllt.
5. **Breakout-Bedingung** – ein Kaufsignal erfordert, dass die aktuelle Kerze ein höheres Hoch als die vorherige Kerze erreicht, gleichzeitig aber ein höheres Tief beibehält und sich mit der Spanne der Kerze zwei Balken zurück überschneidet. Ein Verkaufssignal nutzt die symmetrischen Bedingungen mit niedrigeren Tiefs und niedrigeren Hochs.
6. **Positionsmanagement** – vor der Eröffnung eines neuen Handels schließt die Strategie alle gegenteiligen Positionen. Pro Kerze kann nur ein Long- und ein Short-Eintrag ausgegeben werden, wodurch die `Counter_buy`- und `Counter_sell`-Einschränkungen des ursprünglichen Expert Advisors repliziert werden.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `Trade Volume` | Auftragsgröße für Marktaufträge. | `0.1` |
| `Candle Type` | Zeitrahmen der verarbeiteten Kerzenserie. | `1 hour` |

## Diagramme

Wenn ein Chartbereich verfügbar ist, zeichnet die Strategie die verarbeiteten Kerzen zusammen mit den ausgeführten Trades auf und hilft so, das Ausbruchsverhalten zu visualisieren.

## Notizen

- Die StockSharp-Version behält den stündlichen Sitzungsfilter vom MQL-Experten bei. Passen Sie den Kerzentyp an, wenn Sie in anderen Märkten oder Zeitzonen handeln.
- Stop-Loss- und Take-Profit-Management sind in der Quelldatei MQL nicht enthalten. Der Port überlässt das Risikomanagement daher der Hosting-Umgebung.
