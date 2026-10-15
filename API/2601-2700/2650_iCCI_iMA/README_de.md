# iCCI iMA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Expertenberater «iCCI iMA». Sie handelt Commodity Channel Index (CCI)-Kreuzungen gegen einen exponentiellen gleitenden Durchschnitt (EMA), der direkt auf den CCI-Strom angewendet wird. Ein sekundärer CCI, mit seiner eigenen Periode berechnet, überwacht Überkauft-/Überverkauft-Umkehrungen um die ±100-Bänder. Orders werden in Lots dimensioniert, optional nach Kontostand skaliert, und jeder Trade wird durch konfigurierbare Stop-Loss- und Take-Profit-Niveaus in Pips geschützt.

## Funktionsweise
* **Datenquelle** – Eine konfigurierbare Kerzenserie (1-Minuten-Kerzen standardmäßig) speist alle Indikatorberechnungen mit dem typischen Kerzenpreis `(high + low + close) / 3`.
* **Kernindikatoren** – Der primäre CCI misst den Momentum mit der `CciPeriod`-Länge. Ein EMA dieses CCI (Länge `MaPeriod`) glättet den Oszillator und fungiert als Signallinie. Der sekundäre `CciClosePeriod`-CCI überwacht Schwellenkreuzungen.
* **Einstiegslogik** – Eine Long-Position öffnet sich, wenn der aktuelle CCI über seinem EMA liegt, während der Wert vor zwei abgeschlossenen Kerzen unter dem EMA lag, was auf einen Aufwärtskreuzung hinweist. Eine Short-Position spiegelt diese Logik wider, wenn der CCI nach unten kreuzt. Der Algorithmus handelt erst, wenn alle Indikatoren vollständig gebildet sind und zwei historische Balken verfügbar sind, um den ursprünglichen Look-Back der MQL-Implementierung zu reproduzieren.
* **Ausstiegslogik** – Bestehende Longs schließen, wenn der sekundäre CCI wieder unter +100 fällt oder wenn der primäre CCI nach zwei Balken früher darüber die EMA unterschreitet. Shorts steigen aus, wenn der sekundäre CCI über −100 steigt oder wenn der CCI unter der gleichen Zwei-Balken-Bestätigung wieder über die EMA steigt. Schutzstops überwachen jede abgeschlossene Kerze: Long-Positionen schließen, wenn der Preis auf `entry − stopLossPips * pipSize` fällt und nehmen Gewinn bei `entry + takeProfitPips * pipSize`; Shorts verwenden die symmetrischen Niveaus mit `entry + stopLoss` und `entry − takeProfit`. Die Pip-Größe wird vom Preisschritt des Wertpapiers abgeleitet und passt sich durch Multiplikation der Tick-Größe mit 10 an 3- oder 5-stellige Kurse an, was der MetaTrader-Konvertierung entspricht.
* **Positionsgrößenbestimmung** – Die Basis-Lot-Größe (`LotSize`) wird gegen die `VolumeStep`-, `MinVolume`- und `MaxVolume`-Werte des Instruments validiert, damit Orders Börsenbeschränkungen einhalten. Wenn Money-Management aktiviert ist, multipliziert die Strategie die Lot-Größe mit einem ganzzahligen Faktor gleich dem Kontostand dividiert durch `DepositPerLot`, auf 20 begrenzt, und auf jedem Balken aktualisiert, was das ganzzahlige Step-Scaling des ursprünglichen Experten reproduziert.

## Parameter
- **Kerzentyp** – Für Indikatorberechnungen verwendete Datenserie.
- **CCI-Periode** – Länge des primären CCI, der die Kreuzungssignale antreibt.
- **CCI-Schlusskurs-Periode** – Länge des sekundären CCI für die ±100-Umkehrungsüberwachung.
- **CCI-EMA-Periode** – Periode des EMA, der die primären CCI-Werte glättet.
- **Lot-Größe** – Basis-Handelsvolumen in Lots vor jeder Skalierung.
- **Money-Management aktivieren** – Schaltet die saldenbasierte Skalierung der Lot-Größe um.
- **Einzahlung pro Lot** – Saldeninkrement, das erforderlich ist, um den Lot-Multiplikator um eins zu erhöhen (nur aktiv, wenn Money-Management aktiviert ist).
- **Stop-Loss (Pips)** – Schutzstop-Abstand in Pips; auf null setzen zum Deaktivieren.
- **Take-Profit (Pips)** – Gewinnziel-Abstand in Pips; auf null setzen zum Deaktivieren.

Der Algorithmus erfordert zwei vollständig abgeschlossene Kerzen, bevor er mit dem Handel beginnt, damit die Zwei-Balken-Kreuzungsvergleiche mit der Quell-MQL-Logik übereinstimmen. Stop-Loss- und Take-Profit-Prüfungen werden auf geschlossenen Kerzen unter Verwendung ihrer Hoch/Tief-Extreme bewertet, was MetaTrader's serverseitige Schutzorders innerhalb der StockSharp High-Level-API approximiert.
