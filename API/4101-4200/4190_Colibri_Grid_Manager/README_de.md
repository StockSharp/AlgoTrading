# Colibri Grid Manager-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Colibri Grid Manager ist eine StockSharp-Portierung des MetaTrader 4 Expert Advisors `Colibri.mq4` (Originalordner `MQL/9713`). Die Strategie konzentriert sich auf den diskretionären Grid-Handel: Sie bereitet auf Abruf mehrschichtige ausstehende Aufträge vor, dimensioniert jeden Auftrag anhand des konfigurierten Risikobudgets, fügt Schutzausstiege hinzu und erzwingt ein tägliches Drawdown-Limit, bevor der weitere Handel deaktiviert wird.

## Handelslogik
1. Wenn die Strategie startet, abonniert sie die ausgewählte Kerzenserie und das Orderbuch, um die Referenzpreise im Auge zu behalten, setzt die tägliche Gewinnbasis zurück und löscht frühere Orders.
2. Wenn `EnableGrid` wahr ist und keine Positions- oder aktiven Rasteraufträge vorhanden sind, erstellt die Strategie ein neues Raster für jede zulässige Richtung (`AllowBuy`, `AllowSell`). Aufträge können um einen manuellen Mittelpreis herum oder relativ zu expliziten Kauf-/Verkaufs-Einstiegsankern verteilt werden.
3. Der Auftragstyp (`OrderType`) steuert, ob das Raster Limit-, Stop- oder sofortige Markteintritte verwendet. Der Abstand zwischen den Niveaus wird über `LevelSpacingPoints` in Punkten festgelegt und mithilfe der Tick-Größe des Instruments in Preisinkremente umgewandelt.
4. Das Volumen ist entweder fest (`FixedOrderVolume`) oder von `RiskPercent` abgeleitet. Das risikobasierte Sizing verteilt den konfigurierten Prozentsatz des aktuellen Portfolio-Eigenkapitals über alle Ebenen hinweg in eine Richtung und dividiert ihn durch das durch den Schutzstopp implizierte monetäre Risiko.
5. Sobald eine Einstiegsorder ausgeführt wird, platziert die Strategie automatisch gepaarte Schutzorder: Stops werden von `StopLossPrice` oder `StopDistancePoints` abgeleitet, während Take-Profits von `TakeProfitDistancePoints` abhängen oder standardmäßig einen Rasterschritt entfernt sind. Ausstehende Bestellungen können nach `ExpirationHours` Stunden ablaufen.
6. Die Strategie überwacht kontinuierlich den realisierten plus variablen PnL. Wenn der Verlust des aktuellen Handelstages `DailyLossLimitPercent` überschreitet, werden alle Aufträge storniert, offene Positionen geschlossen und die Erstellung eines neuen Rasters ausgesetzt, bis der nächste Tag beginnt.
7. Manuelle Umschaltungen (`CloseAllPositions`, `CloseLongPositions`, `CloseShortPositions`, `CancelOrders`) ermöglichen es dem Händler, das Buch sofort zu glätten oder zu bereinigen, ohne den Code zu berühren.

## Parameter
- **EnableGrid** – Hauptschalter, der die automatische Netzwartung aktiviert oder deaktiviert.
- **OrderType** – Eingabeauftragstyp (`Limit`, `Stop`, `Market`), der beim Erstellen von Ebenen verwendet wird.
- **AllowBuy / AllowSell** – Wählen Sie die Seiten aus, die am Raster teilnehmen dürfen.
- **UseCenterLine / CenterPrice** – wenn aktiviert, verteilen Sie Kauf-/Verkaufsniveaus symmetrisch um einen zentralen Preis; Ein Nullzentrum verwendet den mittleren Preis.
- **LevelSpacingPoints** – Abstand zwischen aufeinanderfolgenden Levels, gemessen in Punkten und über die Tick-Größe des Instruments in absolute Preisunterschiede umgerechnet.
- **LevelsCount** – Anzahl der Ebenen pro Richtung. Im Marktmodus wird unabhängig von diesem Wert nur eine Order gesendet.
- **BuyEntryPrice / SellEntryPrice** – explizite Anker für lange und kurze Raster, wenn der mittlere Modus deaktiviert ist (Null ist standardmäßig der aktuelle Geld-/Briefkurs).
- **StopLossPrice** – absolutes Stop-Level, das auf jede Order angewendet wird. Lassen Sie Null, um den Stopp von `StopDistancePoints` abzuleiten.
- **StopDistancePoints** – Fallback-Stop-Distanz in Punkten, wenn kein absoluter Stop-Preis angegeben wird.
- **TakeProfitDistancePoints** – optionale Take-Profit-Distanz in Punkten. Bei Null verwendet die Strategie einen Rasterschritt als Standardziel.
- **UseRiskSizing / RiskPercent** – aktivieren Sie die prozentuale Größenbestimmung und definieren Sie den Anteil des Eigenkapitals, der jedem Richtungsraster zugewiesen wird. Der Wert wird gleichmäßig auf alle Ebenen dieser Richtung aufgeteilt.
- **FixedOrderVolume** – Auftragsgröße, die verwendet wird, wenn die risikobasierte Größenbestimmung deaktiviert ist oder kein gültiges Volumen erzeugt.
- **ExpirationHours** – optionale Lebensdauer für ausstehende Grid-Bestellungen.
- **DailyLossLimitPercent** – Stop-Trading-Schwelle, ausgedrückt als Bruchteil des Portfolio-Eigenkapitals, das zu Beginn des Handelstages erfasst wurde.
- **CloseAllPositions / CloseLongPositions / CloseShortPositions / CancelOrders** – manuelle Wartungsbefehle, auf die über die Benutzeroberfläche zugegriffen werden kann.
- **CandleType** – Kerzenserie, die für Wartungsereignisse wie tägliche Zurücksetzungen verwendet wird.

## Implementierungshinweise
- Die Strategie basiert ausschließlich auf der übergeordneten Ebene StockSharp API: `SubscribeCandles`, `SubscribeOrderBook`, `BuyLimit`, `SellStop` usw. Es ist keine direkte Konnektorlogik oder ein Indikatorzugriff erforderlich.
- Bei der Auftragsgröße werden `Security.PriceStep` und `Security.StepPrice` verwendet, um punktbasierte Abstände aus dem MQL-Skript in monetäres Risiko umzuwandeln.
- Schutzausstiege werden über separate Stop-/Limit-Orders implementiert, anstatt die ursprüngliche Einstiegsorder zu ändern, was der Art und Weise entspricht, wie StockSharp verknüpfte Schutzorder verarbeitet.
- Der tägliche Verlustfilter wird bei einem Kalendertagswechsel zurückgesetzt und der Portfoliowert wird erneut erfasst. Händler können den Handel manuell fortsetzen, indem sie `EnableGrid` umschalten, wenn sie die Sicherheitssperre außer Kraft setzen möchten.
- Globale MT4-Variablen, Notfall-Schließflags und grafische Bereinigungsroutinen aus dem Quellskript wurden durch stark typisierte Parameter und manuelle Umschalter ersetzt.

## Nutzungstipps
1. Legen Sie fest, ob das Raster zentriert oder an bestimmten Preisen verankert werden soll, bevor Sie es aktivieren. Geben Sie für zentrierte Gitter ein aussagekräftiges `CenterPrice` an; Lassen Sie es für verankerte Gitter deaktiviert und geben Sie die Kauf-/Verkaufs-Einstiegspreise ein.
2. Kalibrieren Sie `LevelSpacingPoints`, `StopDistancePoints` und `TakeProfitDistancePoints` entsprechend der Instrumentenvolatilität. Denken Sie daran, dass es sich bei allen drei um punktbasierte Werte handelt.
3. Stellen Sie bei Verwendung der risikobasierten Größenbestimmung sicher, dass das Instrument über gültige `PriceStep` und `StepPrice` verfügt. andernfalls greift die Strategie auf das feste Volumen zurück.
4. Verwenden Sie die manuellen Steuerparameter, um Positionen schnell zu löschen oder zu reduzieren, bevor Sie Konfigurationsparameter ändern.
5. Kombinieren Sie das tägliche Verlustlimit mit externem Risikomanagement, wenn mehrere Strategien dasselbe Portfolio teilen.

## Unterschiede zum ursprünglichen Expert Advisor
- Die StockSharp-Version konzentriert sich auf eine saubere Parameterschnittstelle anstelle von globalen MT4-Variablen und einer kommentarbasierten Magic-Number-Logik.
- Notfall-Abschaltflags, automatische Anpassungen der Rastergröße und grafische Objektbereinigung aus dem Originalcode werden auf manuelles Umschalten und einfache Parametervalidierung reduziert.
- Trailing-Stop-Helfer aus dem Skript MQL werden nicht repliziert; Verwenden Sie bei Bedarf die vorhandenen nachgestellten Module von StockSharp.
- Die MQL-Abhängigkeitslogik zwischen Befehlen (Ausführen/Abbrechen basierend auf „Mutter“-Befehlen) wird nicht reproduziert. Jede Ebene arbeitet unabhängig mit ihren eigenen Schutzanordnungen.

Diese Anpassungen bewahren den Geist des ursprünglichen Colibri-Expertenberaters – strukturierte mehrstufige Einträge mit striktem Geldmanagement – und richten die Implementierung gleichzeitig an idiomatischen StockSharp-Mustern aus.
