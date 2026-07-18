# Die einfachste Daytrade-Strategie aller Zeiten
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Umwandlung des MetaTrader 4 Expert Advisors **„Einfachster Daytrade-Roboter aller Zeiten“** zum StockSharp High-Level API.
- Konzipiert für einfachen Tageshandel: Jede Sitzung eröffnet höchstens eine Marktposition, die der Richtung der vorherigen Tageskerze folgt.
- Verwendet nur Kerzendaten, ohne technische Indikatoren oder Oszillatoren. Die gesamte Auftragsverwaltung erfolgt über Marktaufträge.

## Handelslogik
1. Sammeln Sie tägliche Kerzen (`DailyCandleType`, Standard `TimeSpan.FromDays(1)`) und speichern Sie die Eröffnungs- und Schlusskurse des letzten abgeschlossenen Tages.
2. Abonnieren Sie Intraday-Kerzen (`IntradayCandleType`, Standard `TimeSpan.FromMinutes(1)`), um die Ausführung voranzutreiben.
3. Während der frühen Sitzungszeiten (während die Kerzenöffnungsstunde streng unter `EntryHourLimit` liegt, Standard `1`):
   - Wenn der vorherige Tagesschluss über dem vorherigen Tageseröffnungskurs liegt, geben Sie mit `BuyMarket(TradeVolume)` eine Long-Position ein.
   - Wenn der vorherige Tagesschluss unter dem vorherigen Tageseröffnungskurs liegt, geben Sie mit `SellMarket(TradeVolume)` eine Short-Position ein.
   - Wenn die tägliche Kerze flach schloss (offen gleich geschlossen), wird kein Handel eröffnet.
4. Halten Sie die Position den ganzen Tag über. Wenn die Intraday-Candle-Stunde größer oder gleich `MarketCloseHour` ist (Standardwert `20`), schließen Sie alle offenen Positionen mit einer Marktorder (`SellMarket` für Long-Positionen, `BuyMarket` für Short-Positionen).
5. Die Strategie eröffnet nur dann eine neue Position, wenn keine aktive Position vorhanden ist, sodass höchstens ein Handel pro Tag gewährleistet ist.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `TradeVolume` | Auftragsvolumen sowohl für Long- als auch für Short-Einträge. Muss positiv sein. | `1` |
| `EntryHourLimit` | Späteste Stunde (exklusiv), zu der ein neuer Handel eingeleitet werden kann. Werte außerhalb von `[0, 23]` werden durch Validierung begrenzt. | `1` |
| `MarketCloseHour` | Stunde, in der die Strategie eine offene Position zwangsweise schließt. Gilt täglich. | `20` |
| `IntradayCandleType` | Zeitrahmen, der für die Handelsausführungslogik und das Positionsmanagement verwendet wird. | `TimeSpan.FromMinutes(1).TimeFrame()` |
| `DailyCandleType` | Zeitrahmen, der zum Lesen der Eröffnungs- und Schlusskurse des Vortages verwendet wird. | `TimeSpan.FromMinutes(5).TimeFrame()` |

Alle Parameter werden über `Param()` registriert und können im StockSharp-Optimierer optimiert werden.

## Risikomanagement
- Die Strategie verwendet keine Stop-Loss- oder Take-Profit-Level; Das Risiko wird durch den täglichen Ausstieg bei `MarketCloseHour` kontrolliert.
- `StartProtection()` ist beim Start aktiviert, um unerwartete, nicht flache Positionen während des Handels zu verhindern.
- Da pro Tag nur eine Position aktiv sein kann, wird die maximale Exposition durch `TradeVolume` definiert.

## Nutzungshinweise
- Führen Sie die Strategie auf Instrumenten aus, die sowohl Intraday- als auch tägliche Kerzenverläufe liefern. Die Standardkonfiguration erfordert Minuten- und Tageskerzen.
- Richten Sie `EntryHourLimit` und `MarketCloseHour` an der Handelssitzung des ausgewählten Instruments aus.
- Der Algorithmus erwartet in den Kerzenzeitstempeln die Ortszeit der Börse; Passen Sie die Datenquellen entsprechend an.
- Die Logik spiegelt den ursprünglichen MQL-Expert Advisor wider und ermöglicht die Replikation des Verhaltens innerhalb der StockSharp-Umgebung ohne Python-Komponenten.
