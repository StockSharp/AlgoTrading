# Strategie Kreuzung zweier iMA v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie recreiert den MetaTrader Expert Advisor "Crossing of two iMA v2" mit StockSharps High-Level-API. Zwei verschobene gleitende Durchschnitte erzeugen Kreuzungssignale, optional gefiltert durch einen dritten gleitenden Durchschnitt. Schutzstops, feste oder prozentbasierte Positionsgrößen und ein Bar-für-Bar-Trailing-Stop emulieren das Verhalten des ursprünglichen Roboters, während die Implementierung mit den StockSharp-Best-Practices konform bleibt.

## Indikatoren und Eingaben
- **Erster gleitender Durchschnitt** – konfigurierbarer Zeitraum, Verschiebung, Glättungsmethode und angewendeter Preis.
- **Zweiter gleitender Durchschnitt** – unabhängige Konfiguration mit demselben Optionssatz.
- **Dritter gleitender Durchschnitt Filter** – optionaler Trendfilter, der Long-Trades nur hält, wenn der erste MA unter dem Filter liegt, und Short-Trades, wenn der erste MA über dem Filter liegt.
- **Kerzentyp** – steuert den Zeitrahmen/die Reihe, die durch das Datenabonnement geliefert wird.

## Handelslogik
### Schritt 1 – Sofortige Kreuzung
1. Bei jeder fertigen Kerze aktualisiert die Strategie alle gleitenden Durchschnitte mit den ausgewählten angewendeten Preisen.
2. Ein **Long**-Einstieg wird ausgelöst, wenn der erste MA die zweite MA zwischen der vorherigen und aktuellen Bar **überquert**.
3. Ein **Short**-Einstieg wird ausgelöst, wenn der erste MA die zweite MA zwischen der vorherigen und aktuellen Bar **unterschreitet**.
4. Wenn der Filter aktiviert ist, erfordern Long-Signale, dass der erste MA **unter** dem Filter-MA bleibt, während Short-Signale erfordern, dass er **über** dem Filter-MA bleibt.

### Schritt 2 – Verzögerte Bestätigung
Wenn in Schritt 1 kein Signal feuert, prüft die Strategie auf eine Kreuzung, die vor zwei Bars begann, aber noch gültig ist. Dies spiegelt das ursprüngliche EA-Verhalten wider, das die jüngste Geschichte nach verpassten Kreuzen durchsucht. Um wiederholte Ausführungen zu vermeiden, aktiviert sich das Signal nur, wenn mindestens drei Bars seit dem letzten Trade vergangen sind.

### Orderausführung
- Einstiege werden zum Marktpreis ausgeführt. Entgegengesetzte Positionen werden geschlossen, bevor in der neuen Richtung eröffnet wird.
- Ausstiege erfolgen, wenn Stop-Loss-, Take-Profit- oder Trailing-Stop-Niveaus auf der aktuellen Kerze berührt werden. Der Trade wird mit einer Marktorder geschlossen, sobald ein Schutzniveau verletzt wird.

## Risikomanagement
- **Stop-Loss**- und **Take-Profit**-Abstände werden in Pips konfiguriert. Sie werden in Preisoffsets umgerechnet, indem der `PriceStep` des Instruments verwendet wird (standardmäßig `1`, wenn nicht verfügbar).
- Der **Trailing Stop** beginnt beim Einstiegspreis und folgt der günstigen Preisbewegung. Der Stop wird aktualisiert, wenn der beste Preis mindestens `TrailingStepPips` Pips über das vorherige Trailing-Niveau hinausgeht.
- Wenn sowohl ein fester Stop als auch ein Trailing Stop aktiv sind, verwendet die Strategie das konservativere Niveau (höher für Long-Positionen, niedriger für Short-Positionen).

## Positionsgrößen
- Wenn `UseRiskPercent` **true** ist, entspricht das Volumen `Equity * RiskPercent / (StopLossPips * PipValue)`. Wenn kein Stop definiert ist, greift die Strategie auf das feste Volumen zurück.
- Wenn `UseRiskPercent` **false** ist, ist die Handelsgröße immer `FixedVolume`.
- `PipValue` sollte den Geldwert eines einzelnen Pips pro einem Lot/Kontrakt des gehandelten Instruments widerspiegeln.

## Implementierungshinweise
- Die StockSharp-Implementierung arbeitet vollständig auf geschlossenen Kerzen und registriert keine ausstehenden Orders. Benutzer, die Stop- oder Limit-Einstiege benötigen, können die Strategie entsprechend erweitern.
- Der dritte MA-Filter kann deaktiviert werden, um jede Kreuzung zu handeln, was der EA-Option `InpFilterMA = false` entspricht.
- Stellen Sie sicher, dass Kerzentyp, Preisschritt und Pip-Wert-Parameter mit dem gehandelten Instrument übereinstimmen, für eine korrekte Risikosteuerung.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `FirstPeriod` | Periode des ersten gleitenden Durchschnitts. | 5 |
| `FirstShift` | Verschiebung (Bars), die auf den Ausgang des ersten gleitenden Durchschnitts angewendet wird. | 3 |
| `FirstMethod` | Glättungsmethode des ersten gleitenden Durchschnitts (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Smoothed` |
| `FirstAppliedPrice` | Angewendeter Preis für den ersten gleitenden Durchschnitt (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `SecondPeriod` | Periode des zweiten gleitenden Durchschnitts. | 8 |
| `SecondShift` | Verschiebung (Bars) auf den Ausgang des zweiten gleitenden Durchschnitts. | 5 |
| `SecondMethod` | Glättungsmethode für den zweiten gleitenden Durchschnitt. | `Smoothed` |
| `SecondAppliedPrice` | Angewendeter Preis für den zweiten gleitenden Durchschnitt. | `Close` |
| `UseFilter` | Aktiviert den dritten MA-Richtungsfilter. | `true` |
| `ThirdPeriod` | Periode des dritten MA-Filters. | 13 |
| `ThirdShift` | Verschiebung (Bars) auf den dritten MA-Ausgang. | 8 |
| `ThirdMethod` | Glättungsmethode für den dritten MA-Filter. | `Smoothed` |
| `ThirdAppliedPrice` | Angewendeter Preis für den dritten MA-Filter. | `Close` |
| `UseRiskPercent` | Umschalten zwischen festem Volumen und prozentbasierter Positionsgröße. | `true` |
| `FixedVolume` | Handelsgröße bei aktivem festem Sizing. | 0.1 |
| `RiskPercent` | Anteil des Eigenkapitals, der pro Trade riskiert wird. | 5 |
| `PipValue` | Geldwert eines Pips pro Lot/Kontrakt. | 1 |
| `StopLossPips` | Stop-Loss-Abstand in Pips. | 50 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. | 50 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. | 10 |
| `TrailingStepPips` | Minimales Pip-Inkrement, das erforderlich ist, um den Trailing Stop vorzurücken. | 4 |
| `CandleType` | Kerzendatentyp / Zeitrahmen, der von der Strategie verwendet wird. | 1-Minuten-Kerzen |
