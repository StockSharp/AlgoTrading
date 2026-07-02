# CCI MA v1.5 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erstellt den Expertenberater MetaTrader „CCI_MA v1.5“ innerhalb des StockSharp-High-Level-API neu. Der ursprüngliche Roboter wartet darauf, dass der Commodity Channel Index (CCI) einen einfachen gleitenden Durchschnitt überschreitet, der auf den CCI-Werten selbst berechnet wird, und verwendet einen sekundären CCI, um Exits um die ±100-Schwellenwerte herum zu überwachen. Der StockSharp-Port behält die gleiche Signalreihenfolge, das optionale Geldmanagement und die punktbasierten Stopp-/Zielentfernungen bei und passt alles an Kerzenabonnements und Indikatorbindungen an.

## Wie es funktioniert
* **Datenquelle** – Eine benutzerdefinierte Kerzenserie (standardmäßig 15-Minuten-Kerzen) speist beide CCIs. Die Indikatoren lesen den Schlusskurs der Kerze, um die `PRICE_CLOSE`-Einstellung von MetaTrader widerzuspiegeln.
* **Kernindikatoren** – Der primäre `CommodityChannelIndex` (Parameter `CciPeriod`) liefert den Momentumwert. Ein `SimpleMovingAverage` mit dem Zeitraum `MaPeriod` wird auf den Strom von CCI-Werten angewendet, um die Triggerlinie zu bilden. Ein sekundärer CCI (`SignalCciPeriod`) überwacht überkaufte und überverkaufte Umkehrungen um ±100.
* **Einstiegslogik** – Ein Long-Trade wird auf dem Balken nach einem Aufwärts-Crossover eröffnet: Die zuvor abgeschlossene Kerze (`prevCci`) muss über dem gleitenden Durchschnitt CCI liegen, während die Kerze davor (`prev2Cci`) darunter lag. Ein kurzes Signal ist die symmetrische Kreuzung nach unten. Bestehende gegensätzliche Positionen werden geschlossen und umgedreht, indem der Absolutwert der aktuellen Position zur neuen Ordergröße addiert wird, was dem Verhalten der MQL-Version entspricht.
* **Ausstiegslogik** – Long-Positionen werden liquidiert, wenn der aufsichtsrechtliche CCI von über +100 auf unter +100 fällt oder wenn der primäre CCI wieder unter seinen gleitenden Durchschnitt fällt (wiederum anhand der beiden zuvor abgeschlossenen Kerzen bewertet). Shorts treten unter den umgekehrten Bedingungen auf. Schutzstopps emulieren die punktbasierten Abstände von MetaTrader: Die Strategie leitet eine Pip-Größe aus dem Instrument `PriceStep` ab (Multiplikation mit 10 für drei- oder fünfstellige Kurse) und vergleicht die Kerzenextreme mit `entry ± distance` bei jeder abgeschlossenen Kerze.
* **Positionsgröße** – `LotVolume` definiert die Basis-Ordergröße. Wenn `UseMoneyManagement` aktiviert ist, multipliziert die Strategie es mit einem ganzzahligen Faktor gleich `floor(balance / DepositPerLot)`, begrenzt durch `MaxMultiplier`, wodurch die Einzahlungsleiter des Fachberaters reproduziert wird. Das Bestellvolumen wird vor der Übermittlung an die Instrumentenbeschränkungen `VolumeStep`, `MinVolume` und `MaxVolume` angepasst.

## Parameter
- **Kerzentyp** – Kerzendatentyp, der allen Indikatorberechnungen zugrunde liegt.
- **CCI-Periode** – Länge des primären CCI-Oszillators.
- **Exit-CCI-Zeitraum** – Länge des Überwachungszeitraums CCI, der für Schwellenwert-Exits verwendet wird.
- **CCI MA-Periode** – Periode des einfachen gleitenden Durchschnitts, der auf den primären CCI angewendet wird.
- **Lotvolumen** – Basishandelsvolumen vor der Skalierung des Geldmanagements.
- **Geldverwaltung aktivieren** – Aktiviert die einzahlungsbasierte Skalierung des Lotvolumens.
- **Einzahlung pro Lot** – Erforderliche Guthabenerhöhung, um den Lot-Multiplikator um eins zu erhöhen (wird nur verwendet, wenn die Geldverwaltung aktiv ist).
- **Max. Multiplikator** – Maximaler Multiplikator, den das Geldmanagement erreichen kann.
- **Stop-Loss (Pips)** – Abstand in Pips für den Schutzstopp; Zum Deaktivieren auf Null setzen.
- **Take Profit (Pips)** – Abstand in Pips für das Gewinnziel; Zum Deaktivieren auf Null setzen.

Die Strategie wartet auf zwei vollständig geschlossene Kerzen, bevor sie die erste Order erteilt, sodass die Zwei-Balken-Crossover-Vergleiche genau mit der verzögerten Ausführung des MQL-Experten übereinstimmen. Stop-Loss- und Take-Profit-Prüfungen werden für fertige Kerzen unter Verwendung ihrer Hoch-/Tief-Extremwerte ausgeführt, was den serverseitigen Schutzaufträgen von MetaTrader nahe kommt und gleichzeitig innerhalb des hohen Niveaus StockSharp API bleibt.
