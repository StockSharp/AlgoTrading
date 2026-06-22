# Geldverwaltungs-Strategie mit festem Margin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das MetaTrader-Beispiel "Money Fixed Margin" mithilfe der High-Level-API von StockSharp. Sie zeigt, wie Positionen durch Risikierung eines festen Prozentsatzes des Portfolios bemessen werden, während die in Pips ausgedrückte Stop-Loss-Distanz in einen absoluten Preisversatz umgerechnet wird. Die Strategie handelt nur Long-Positionen und konzentriert sich auf die Demonstration der Geldverwaltungslogik anstelle eines prädiktiven Einstiegssignals.

## Details

- **Einstiegskriterien**:
  - **Long**: führt nach jeder abgeschlossenen Kerzenanzahl, die durch `Check Interval` angegeben wird (standardmäßig jede 980. Bar), einen Marktkauf aus. Die Order verwendet den Schlusskurs der auslösenden Kerze als Referenz für Risikoberechnungen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Ein schützender Stop-Loss wird automatisch über `StartProtection` in einer Entfernung angehängt, die aus dem Parameter `Stop Loss (pips)` abgeleitet wird.
  - Es wird kein Gewinnziel verwendet; Positionen werden nur durch den Stop-Loss oder manuellen Eingriff geschlossen.
- **Stops**: Nur Stop Loss.
- **Standardwerte**:
  - `Stop Loss (pips)` = 25
  - `Risk Percent` = 10
  - `Check Interval` = 980
  - `Candle Type` = 1-Minuten-Zeitrahmen
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Ja (Stop-Loss)
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (konfigurierbar über `Candle Type`)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel (skaliert mit `Risk Percent`)

## Positionsgrößenlogik

1. Die Strategie liest `Security.PriceStep` und `Security.Decimals` um die Pip-Größe abzuleiten. Paare mit 3 oder 5 Dezimalstellen verwenden einen zehnfachen Multiplikator, um der MetaTrader-Definition eines Pips zu entsprechen.
2. `Stop Loss (pips)` wird mit der Pip-Größe multipliziert, um eine absolute Preisdistanz (`ExtStopLoss`) zu erhalten, die mit dem MQL5-Code identisch ist.
3. Der aktuelle Portfoliowert (bevorzugt `Portfolio.CurrentValue`, dann `Portfolio.BeginValue`) wird mit `Risk Percent / 100` multipliziert, um das pro Trade exponierte Kapital zu bestimmen.
4. Das Risiko pro Einzellot wird durch das Produkt aus der Stop-Loss-Distanz, der Anzahl der Preisschritte innerhalb dieser Distanz und `Security.StepPrice` (wenn verfügbar) berechnet. Wenn `StepPrice` unbekannt ist, wird die Preisdistanz selbst als Fallback verwendet.
5. Die Division des Risikobetrags durch das Risiko pro Lot ergibt das gewünschte Volumen. Das Ergebnis wird auf den `VolumeStep` des Instruments normiert, auf Mindest- und Höchstvolumengrenzen begrenzt und zur Transparenz protokolliert. Ein Vergleichswert mit null Stop-Loss-Distanz wird ebenfalls protokolliert, um zu veranschaulichen, warum der Money Manager Trades ohne einen schützenden Stop ablehnt.

## Arbeitsablauf

1. Beim Start abonniert die Strategie die konfigurierte Kerzenserie, berechnet die Pip-Größe und aktiviert `StartProtection` mit der berechneten absoluten Stop-Loss-Distanz.
2. Jede abgeschlossene Kerze erhöht einen internen Zähler. Wenn der Zähler das gewählte `Check Interval` erreicht, bewertet die Strategie die Positionsgröße, gibt Diagnoseinformationen aus und setzt den Zähler zurück.
3. Wenn das berechnete Volumen positiv ist, wird eine Marktbuy-Order platziert. Der eingebaute Schutz hängt den Stop-Loss bei `Close - ExtStopLoss` an. Fehler (z. B. durch unzureichende Daten oder Instrumente mit Nullpreis) verhindern die Orderübermittlung.
4. Es werden keine weiteren Trades durchgeführt, bis der Zähler ein weiteres Intervall abgeschlossen hat, wobei der Fokus auf dem Geldmanagement statt auf der Signalfrequenz liegt.

## Verwendungshinweise

- Setzen Sie `Risk Percent` auf einen konservativen Wert, wenn Sie sich mit einem Live-Konto verbinden; das Standard-Risiko von 10% spiegelt das MQL-Beispiel wider, ist aber für den echten Handel aggressiv.
- Stellen Sie sicher, dass das Instrument aussagekräftige `PriceStep`- und `StepPrice`-Metadaten bereitstellt. Wenn nicht verfügbar, arbeitet die Strategie weiterhin, interpretiert das Risiko jedoch in rohen Preiseinheiten.
- Die Strategie vermeidet absichtlich Short-Trades, um der originalen Demonstration treu zu bleiben. Passen Sie `BuyMarket`/`SellMarket`-Aufrufe an, wenn zweiseitiger Handel gewünscht wird.
- Kombinieren Sie dieses Geldverwaltungsmodul mit anderen Signalgeneratoren durch Wiederverwendung des `CalculateFixedMarginVolume`-Helpers aus dem Strategiecode.
