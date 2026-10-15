# Strategie Kreuzung Zweier iMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den klassischen MetaTrader 5 Expert Advisor **"Crossing of two iMA"** in die High-Level-API von StockSharp. Sie handelt, wenn sich zwei konfigurierbare gleitende Durchschnitte kreuzen, und kann optional Bestätigung von einem dritten gleitenden Durchschnitt erfordern, der als Richtungsfilter fungiert. Die Implementierung behält die ursprüngliche Flexibilität, indem sie manuelle oder risikobasierte Positionsgrößenbestimmung, Offsets im Stil ausstehender Einstiege und einen Trailing Stop mit benutzerdefiniertem Schritt unterstützt.

Die Konvertierung verarbeitet Signale beim Schließen jeder abgeschlossenen Kerze und repliziert, wie der MQL5-Experte auf einen neuen Balken wartet. Das Verhalten von ausstehenden Aufträgen (`PriceLevelPips`) wird intern durch Überwachung von Kerzenhochs und -tiefs simuliert, sodass keine tatsächlichen Stop-/Limit-Aufträge gesendet werden. Ein ausstehender Long-Trigger wird aktiviert, wenn der Balken den gewählten Preis für Buy-Stop-Einstiege erreicht oder auf den Preis für Buy-Limit-Einstiege fällt, und die gleiche symmetrische Logik wird für Short-Setups angewendet.

## Handelsregeln

- **Indikatoren**
  - Erster gleitender Durchschnitt `First` (Periode, Versatz und Methode sind konfigurierbar).
  - Zweiter gleitender Durchschnitt `Second` (ebenfalls vollständig konfigurierbar).
  - Optionaler dritter gleitender Durchschnitt `Third` als Filter (`UseThirdMovingAverage = true`).
- **Einstiegskriterien**
  - **Primärer Kreuzung (Balken 0 und 1)**
    - **Long**: der erste MA kreuzt auf dem aktuellen Balken von unten nach oben über den zweiten MA, während er auf dem vorherigen Balken darunter war. Wenn der Filter aktiv ist, muss der dritte MA unter dem ersten MA bleiben, um den Long-Ausbruch zu validieren.
    - **Short**: der erste MA kreuzt unter den zweiten MA und, wenn der Filter aktiviert ist, muss der dritte MA über dem ersten MA bleiben.
  - **Zusatzkreuzung (Balken 0 und 2)**
    - Führt eine zusätzliche Rückwärtssuche durch, um schnelle Kreuzungen zu erfassen, die zwischen den beiden vorherigen Balken aufgetreten sind. Die Strategie ignoriert dieses Signal, wenn innerhalb der letzten drei Balken bereits ein Trade eröffnet wurde (identisch mit der MQL5-Verlaufssuche).
- **Richtung**: sowohl Long als auch Short.
- **Stops und Ziele**
  - Stop-Loss und Take-Profit werden in Pips ausgedrückt. Sie werden basierend auf der Tick-Größe des Instruments in Preisoffsets umgerechnet und für 3/5-stellige Preisangaben wie der ursprüngliche EA angepasst.
  - Trailing Stop aktiviert sich nur, wenn `TrailingStopPips > 0`. Er bewegt den Stop um die Trailing-Distanz, sobald der Preis sich um mindestens `TrailingStepPips` über das vorherige Stop-Niveau hinaus bewegt.
- **Pending-Order-Modus (`PriceLevelPips`)**
  - `0`: sofort zum Marktpreis einsteigen.
  - `< 0`: Stop-Orders simulieren (Buy Stop über Preis, Sell Stop unter Preis). Stop-Loss und Take-Profit werden um denselben Offset verschoben.
  - `> 0`: Limit-Orders simulieren (Buy Limit unter Preis, Sell Limit über Preis). Schutzniveaus werden entsprechend verschoben.

## Geldmanagement

- `UseFixedVolume = true` repliziert den manuellen Lot-Modus des EA. Die Strategie verwendet einfach `Volume` (und schließt Gegenpositionen, bevor eine neue geöffnet wird).
- Wenn `UseFixedVolume = false`, weist die Strategie Risiko als `Portfolio.CurrentValue * RiskPercent / 100` zu. Die Auftragsgröße wird zu `riskAmount / stopDistance`. Wenn kein Stop-Loss angegeben wird (`StopLossPips = 0`), ist die berechnete Risikoentfernung null, sodass die Strategie die Positionseröffnung verweigert — identisch mit dem ursprünglichen `MoneyFixedRisk`-Verhalten, das null Lots zurückgibt.

## Trailing-Logik

- Long-Positionen verfolgen den Stop auf `Close - TrailingStopPips * pipValue`, sobald sich der Preis mindestens `TrailingStepPips` über den vorherigen Stop hinaus bewegt hat. Der Trailing-Wert bewegt sich immer nach oben und lockert den Stop nie.
- Short-Positionen spiegeln dieses Verhalten, indem der Stop auf `Close + TrailingStopPips * pipValue` bewegt wird, wenn der Preis ausreichend zu seinen Gunsten voranschreitet.
- Take-Profit und initialer Stop werden vor Trailing-Anpassungen überprüft, um sicherzustellen, dass Ausstiege mit den ursprünglichen EA-Prioritäten übereinstimmen.

## Standardparameter

- Erster MA: Länge `5`, Versatz `3`, Methode `Smoothed`.
- Zweiter MA: Länge `8`, Versatz `5`, Methode `Smoothed`.
- Dritter MA-Filter: aktiviert, Länge `13`, Versatz `8`, Methode `Smoothed`.
- Risikokontrollen: Stop-Loss `50` Pips, Take-Profit `50` Pips, Trailing `10` Pips mit `4` Pip-Schritt.
- Geldmanagement: `UseFixedVolume = true`, `RiskPercent = 5` für den alternativen Dimensionierungsmodus.
- Ausstehender Offset: `0` Pips (Marktausführung).
- Kerzentyp: 1-Minuten-Zeitrahmen (kann geändert werden, um dem ursprünglichen Chartperiode zu entsprechen).

## Implementierungshinweise

- Die `shift`-Parameter des gleitenden Durchschnitts verzögern Signalwerte genau um die konfigurierte Anzahl von Balken, sodass das Plotting auf StockSharp-Charts mit dem MT5-visuellen Versatz übereinstimmt.
- Die Strategie speichert nur den minimal erforderlichen Zustand (aktuell, vorherig und zwei Balken zurück), um die "Balken [0], [1], [2]"-Logik aus MQL5 zu erfüllen. Keine historischen Sammlungen werden über diesen Puffer hinaus neu erstellt.
- Ausstehende Einstiege werden gelöscht, wenn ein neues Signal erscheint, was den `DeleteAllOrders()`-Aufruf des EA repliziert.
- Da StockSharp Aufträge asynchron ausführt, verwendet der für Trailing- und Zielberechnungen erfasste Einstandspreis den beabsichtigten Trigger-Preis. Backtests reproduzieren daher die ursprüngliche EA-Logik auf Kerzendaten ohne Abhängigkeit von Tick-Level-Fills.
