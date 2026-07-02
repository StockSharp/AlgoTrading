# Marktmeisterstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

`MarketMasterStrategy` ist eine hochrangige StockSharp-Konvertierung des MetaTrader 4-Expertenberaters „Market Master“ (`MQL/31326/MarketMaster EN.mq4`). Der ursprüngliche Bot kombinierte einen umfangreichen Indikatorstapel mit komplizierten Geldverwaltungsregeln, Nachrichtenvermeidung und mehrstufigen Orderpyramiden. Der C#-Port konzentriert sich auf den deterministischen technischen Kern, sodass er auf der ereignisgesteuerten Engine von StockSharp ohne externe Webdienste ausgeführt werden kann. Alle Indikatorentscheidungen werden im Rahmen des Handelszeitraums durch ein einziges Kerzenabonnement gemäß den Repository-Richtlinien berechnet.

## Kernindikatoren

Die Strategie bindet die folgenden StockSharp-Indikatoren an die Handelskerzenserie:

- **AverageTrueRange (ATR)** – zwei Instanzen werden verwaltet. Die erste verfolgt die primären Einstiegsbedingungen, die zweite spiegelt die MT4-„Absicherung“ ATR wider, die für Wiederherstellungspositionen verwendet wurde.
- **MoneyFlowIndex (MFI)** – misst den volumenbereinigten Preisfluss, um Akkumulations- oder Verteilungsschwankungen zu erkennen.
- **BullsPower / BearsPower** – Replizieren Sie die MT4-Filter `iBullsPower` und `iBearsPower`, die vor dem Abschluss von Trades eine bullische/bärische Dominanz erforderten.
- **StochasticOscillator** – liefert `%K` und `%D` Zeilen. Bei der Konvertierung werden die ursprünglichen Oszillatorlängen berücksichtigt und der Benutzer kann den Filter ein- oder ausschalten.
- **ParabolicSar** – in MetaTrader wurden zwei Zeitrahmen verwendet. Der StockSharp-Port speichert zwei unabhängige SAR-Indikatoren (Primär- und Bestätigungsindikatoren), deren Schritte die Eingaben des Expertenberaters widerspiegeln.

Alle Indikatoren werden automatisch um StockSharp aufgewärmt. Die Strategie greift nicht über `GetValue` auf den Indikatorverlauf zu – stattdessen speichert sie die vorherigen Werte in privaten Feldern (`_prevAtr`, `_prevMfi`, `_prevStochasticMain` usw.), wie es die Konvertierungsregeln erfordern.

## Signallogik

Der MQL-Experte definierte zwei Haupteintragsfamilien („ZERO“ und „MA“). Sie haben identische ATR/MFI/Bulls/Bears-Filter, unterscheiden sich jedoch in der Oszillatorbestätigung. Die StockSharp-Version stellt den umfangreicheren „MA“-Zweig bereit, da dieser am restriktivsten ist und daher den realen Handelsbedingungen am nächsten kommt. Ein Long-Signal wird bestätigt, wenn bei einer fertigen Kerze alle folgenden Bedingungen zutreffen:

1. ATR steigt relativ zur vorherigen Kerze (entweder die primäre Kerze ATR oder die Absicherung ATR, je nachdem, ob bereits eine Position vorhanden ist).
2. MFI steigt und Bears Power ist positiv, was Aufwärtsdruck signalisiert.
3. Der Stochastic-Oszillator ist aktiviert und `%K` liegt über `%D` mit steigender Tendenz, während `%K` unter der konfigurierbaren überkauften Obergrenze (`StochasticBuyLevel`) bleibt.
4. Parabolic SAR-Filter sind aktiviert und die Kerze schließt über beiden SAR-Werten.
5. Das aktuelle Kerzenvolumen erreicht den konfigurierten Schwellenwert (`MinVolume` oder `MinHedgeVolume`).

Short-Signale spiegeln die Long-Logik mit sinkendem MFI, negativer Bulls Power, `%K` unter `%D` und SAR Werten über dem Preis wider. Volumenprüfungen verhindern den Handel bei schwachen Märkten und replizieren die `iVolume`-Aufrufe von MT4.

## Positionsmanagement

- **Automatische Lautstärke** – das Original EA bot einen Balance-basierten Positionsgrößenblock. `CalculateBaseVolume` folgt dem gleichen Geist, indem es das Auftragsvolumen mit `RiskMultiplier` skaliert und dabei die Einschränkungen des Instruments `VolumeStep`, `MinVolume` und `MaxVolume` berücksichtigt.
- **Pyramidierung** – wenn `AllowSameSignalEntries` gleich `true` ist, verwenden zusätzliche Bestellungen das Basisvolumen multipliziert mit `VolumeMultiplier` wieder. Da StockSharp-Strategien mit Nettopositionen arbeiten, erhöht Pyramiding das Netto-Long- oder Netto-Short-Engagement, anstatt parallele Tickets zu eröffnen.
- **Entgegengesetzte Signale** – `AllowOppositeEntries` steuert, ob eine erkannte Umkehr die aktuelle Position sofort schließt und optional einen Handel in die neue Richtung eröffnet. Wenn die Strategie deaktiviert ist, wird sie beendet, wartet jedoch auf ein neues Signal, bevor sie erneut eintritt, und imitiert damit den Schalter „Kein entgegengesetztes Signal“ in der MT4-Schnittstelle.
- **Stop-Loss** – die MT4-Eingabe `StopLoss` wird als `StopLossPoints` angezeigt. Wenn das Instrument einen `PriceStep` bereitstellt, wird der Wert über `StartProtection` in StockSharp Schutzanordnungen umgewandelt.
- **Handelszeiten** – `UseTradingWindow`, `TradingStart`, `TradingEnd`, `UseTradingBreak`, `BreakStart` und `BreakEnd` reproduzieren das Eröffnungsfenster und die Intraday-Pause des Quellenexperten. Zeitvergleiche werden in der Börsenzeitzone durchgeführt, die von den eingehenden Kerzennachrichten getragen wird.

## Unterschiede zur MetaTrader-Version

- **Nachrichtenfilter** – der MT4-Roboter hat Wirtschaftskalenderdaten von Investing.com und DailyFX heruntergeladen. Durch die Umstellung entfallen alle Netzwerkaufrufe und werden durch eine manuelle Steuerung des Handelsfensters ersetzt. Passen Sie bei nachrichtensensiblem Verhalten die Timing-Parameter an oder pausieren Sie die Strategie extern.
- **Überprüfungen des Bestellverlaufs** – Funktionen wie `OrdersHistoryTotal()` und die „Wieder öffnen“-Logik waren eng mit dem Ticketmodell von MetaTrader verknüpft. StockSharp arbeitet mit einer Nettoposition, sodass der Port einfach einen erneuten Eintritt zulässt, wenn der Richtungsfilter wieder gültig wird.
- **Wiederherstellungsanordnungen** – der ursprüngliche Code verwaltete mehrere Magic Numbers und Kommentarbezeichnungen. Der Port behält die Multiplikatorlogik (`VolumeMultiplier`) bei, aber jede zusätzliche Bestellung verändert die einzelne Nettoposition.
- **Trailing Stop** – Der `TrailingStop`/`TrailingStep`-Block von MetaTrader basierte auf einer asynchronen Auftragsänderung. StockSharp-Benutzer können die Strategie erweitern, indem sie `PositionChanged`-Ereignisse abonnieren oder nachgestellte Optionen in `StartProtection` aktivieren, aber die Basiskonvertierung konzentriert sich auf die Signalparität.

## Parameter

| Eigentum | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | `1` | Basisbestellgröße, wenn die automatische Volumeneinstellung deaktiviert ist. |
| `UseAutoVolume` | `true` | Aktivieren Sie die risikobasierte Volumenskalierung. |
| `RiskMultiplier` | `10` | Prozentsatz des Portfoliosaldos, der bei der automatischen Volumenberechnung verwendet wird (spiegelt `Risk_Multiplier`). |
| `VolumeMultiplier` | `2` | Pyramidenfaktor für zusätzliche Einträge (`KLot`). |
| `MinVolume` | `3000` | Mindestkerzenvolumen für den ersten Eintrag (`MinVol`). |
| `MinHedgeVolume` | `3000` | Volumenschwelle für Add-on-Trades (`MinVolH`). |
| `AtrPeriod` / `AtrHedgePeriod` | `14` | ATR Längen für die Basis- und Hedge-Filter. |
| `MfiPeriod` | `14` | MFI-Zeitraum. |
| `BullBearPeriod` | `14` | Bulls/Bears Power-Periode. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | `5 / 3 / 3` | Stochastic Oszillatorkonfiguration. |
| `StochasticBuyLevel` / `StochasticSellLevel` | `60 / 40` | Oszillatorschwellenwerte (`StoBuy` und `StoSell`). |
| `UseStochasticFilter`, `UsePsarFilter`, `UsePsarConfirmation` | `true` | Schaltet für indikatorbasierte Bestätigungen um. |
| `PsarStep` / `PsarMaxStep` / `PsarConfirmStep` / `PsarConfirmMaxStep` | `0.02 / 0.2 / 0.02 / 0.2` | SAR Beschleunigungen und Obergrenzen. |
| `AllowSameSignalEntries` | `false` | Aktivieren Sie Pyramiding für identische Signale. |
| `AllowOppositeEntries` | `true` | Ermöglichen Sie sofortige Umkehrgeschäfte. |
| `UseTradingWindow` | `false` | Beschränken Sie den Handel auf ein Zeitintervall. |
| `TradingStart` / `TradingEnd` | `06:00 / 18:00` | Tägliches Handelsfenster. |
| `UseTradingBreak` | `false` | Ermöglichen Sie eine kurze Intraday-Pause. |
| `BreakStart` / `BreakEnd` | `06:00:01 / 06:00:02` | Überwinden Sie Grenzen (entsprechen Sie den MT4-Standards). |
| `StopLossPoints` | `0` | Optionaler Schutzanschlag in den Instrumentenpunkten. |
| `CandleType` | `15m TimeFrame` | Für alle Indikatoren verwendete Kerzenserie. |

## Nutzungshinweise

1. Hängen Sie die Strategie im StockSharp Designer oder im Code an ein Wertpapier und Portfolio an und starten Sie sie dann während der Aufwärmphase, damit sich alle Indikatoren bilden können.
2. Wenn Sie eine Bestätigung für mehrere Zeitrahmen benötigen, passen Sie die Einstellungen für `CandleType` und SAR entsprechend an. Die Strategie abonniert einen einzelnen Kerzen-Feed und bindet jeden Indikator über `Bind`, sodass keine manuelle Indikatorregistrierung erforderlich ist.
3. Verwenden Sie die StockSharp-Protokollierung (`LogInfo`, `LogWarning`) zum Debuggen, wenn Sie den Code erweitern. Durch die Umstellung wird die interne Zustandsverwaltung einfach gehalten, sodass zusätzliche Module (z. B. Schleppschutz) problemlos eingesteckt werden können.
4. Die Strategie basiert auf Nettopositionen. Wenn Sie planen, das Verhalten einzelner Tickets ähnlich wie MetaTrader zu modellieren, binden Sie die Strategie in einen Multi-Sicherheits-Router ein, der synthetische Tickets verfolgt.

## Erweiterung des Hafens

- Implementieren Sie eine benutzerdefinierte Exit-Logik, indem Sie `OnNewMyTrade` überschreiben oder `PositionChanged` abonnieren.
- Fügen Sie die Integration des Wirtschaftskalenders hinzu, indem Sie eine externe Komponente einführen, die `UseTradingWindow` umschaltet oder `Stop()` aufruft, wenn sich Ereignisse mit großer Tragweite nähern.
- Rufen Sie zur Signalvisualisierung `CreateChartArea()` und `DrawIndicator()` in `OnStarted` auf – bei der Konvertierung bleiben diese Hooks der Übersichtlichkeit halber leer.

Der Code entspricht vollständig den Repository-Richtlinien: Er verwendet Tab-Einrückung, `Bind`-Abonnements auf hoher Ebene, vermeidet Indikator-Rückverweise und macht alle konfigurierbaren Eingaben über `StrategyParam`-Objekte verfügbar.
