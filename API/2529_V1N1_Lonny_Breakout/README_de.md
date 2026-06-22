# V1N1 Lonny Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die V1N1 Lonny Breakout-Strategie repliziert den MetaTrader Expert Advisor "V1N1 LONNY". Sie zielt auf Ausbrüche ab, die rund um die London- und New-York-Sitzungen entstehen, indem eine Eröffnungsrange aufgebaut und auf einen entschiedenen Schlusskurs außerhalb dieser Range gewartet wird. Die Strategie stützt sich auf einen exponentiellen gleitenden Durchschnitt, um den vorherrschenden Trend zu erfassen, und auf einen Stochastik-Oszillator, um überkaufte oder überverkaufte Bedingungen vor dem Markteintritt herauszufiltern.

Ein konfigurierbares Risikomodell ermöglicht die Positionsgrößenbestimmung durch festes Volumen oder als Prozentsatz des Kontokapitals. Die Implementierung beinhaltet auch optionale Spread-Filterung, Trailing-Stops und ein balkenbasiertes Timeout, das den Trade schließt, wenn der Schwung nach einer vordefinierten Anzahl von Kerzen nachlässt.

## Handelslogik
1. **Sitzungsausrichtung** – Handel ist nur zwischen den konfigurierten Start- und Endzeiten erlaubt. Der Zeitplan kann gemäß Sommerzeit-Regelungen für London oder New York verschoben werden.
2. **Eröffnungsrange** – Unmittelbar vor Sitzungsbeginn zeichnet die Strategie die Hochs und Tiefs einer festen Anzahl von Kerzen auf. Diese Range liefert die Ausbruchsniveaus, die während des Handelsfensters verwendet werden.
3. **Trendbestätigung** – Die Steigung des exponentiellen gleitenden Durchschnitts (EMA) muss mit der Handelsrichtung übereinstimmen. Ein bullischer Ausbruch erfordert einen steigenden EMA, während ein bärischer Ausbruch einen fallenden EMA erfordert.
4. **Momentum-Filter** – Der Stochastik-Oszillator muss innerhalb einer konfigurierbaren Zone um den Mittelpunkt bleiben, um zu vermeiden, dass eingestiegen wird, wenn der Markt bereits überkauft oder überverkauft ist.
5. **Ausbruchsvalidierung** – Die vorherige Kerze muss mindestens um den minimalen Ausbruchsabstand über dem Range-Hoch oder -Tief schließen, aber nicht weiter als der maximale Abstand.
6. **Risikokontrollen** – Jede Position definiert einen Stop-Loss von der Range-Grenze und ein Take-Profit-Ziel basierend auf einem Faktor dieses Stop-Abstands. Ein Trailing-Stop kann den Ausstieg enger setzen, während der Trade fortschreitet, und Positionen können nach einer bestimmten Anzahl von Kerzen zwangsweise geschlossen werden.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `StartTrade` | Sitzungsstartzeit. |
| `EndTrade` | Sitzungsendzeit. |
| `SwitchDst` | Sommerzeit-Behandlung: Europa (kein Versatz), USA (relativer Versatz zwischen London und New York) oder deaktiviert. |
| `RiskModes` | Positionsgrößenmodus (Prozentsatz des Kapitals oder festes Volumen). |
| `PositionRisk` | Risikoprozentsatz oder festes Volumen, je nach Modus. |
| `TradeRange` | Anzahl der Kerzen zum Aufbau der Eröffnungsrange. |
| `MinRangePoints` / `MaxRangePoints` | Minimale und maximale Größe der Eröffnungsrange in Preispunkten. |
| `MinBreakRange` / `MaxBreakRange` | Minimaler und maximaler akzeptabler Ausbruchsabstand über oder unter der Range in Preispunkten. |
| `StopLossPoints` | Stop-Loss-Abstand gemessen von der gegenüberliegenden Seite der Range in Preispunkten. |
| `TpFactor` | Take-Profit-Multiplikator angewendet auf den Stop-Loss-Abstand. |
| `TrailStopPoints` | Optionaler Trailing-Stop-Abstand in Preispunkten. Auf null setzen, um das Trailing zu deaktivieren. |
| `TrendPeriod` | Periode für den EMA-Steigungsfilter. |
| `OverPeriod` | Periode für den Stochastik-Oszillator. |
| `OverLevels` | Abstand von 50, der zur Definition des akzeptablen Stochastik-Bereichs verwendet wird. |
| `BarsToClose` | Maximale Anzahl von Kerzen, die die Position offen halten. Null deaktiviert das Timeout. |
| `MaxSpreadPoints` | Maximaler erlaubter Spread in Preispunkten. |
| `SlippagePoints` | Referenz-Slippage in Preispunkten (zur Kompatibilität mit dem ursprünglichen Expert Advisor beibehalten). |
| `CandleType` | Von der Strategie verarbeiteter Kerzentyp und Zeitrahmen. |

## Verwendungshinweise
- Die Strategie ist für Instrumente mit einem festen Preisschritt konzipiert. Punktbasierte Eingaben werden mit dem `PriceStep` des Instruments multipliziert, um Preisabstände zu erhalten.
- Orderbuchdaten werden zur Schätzung des aktuellen Spreads verwendet. Wenn keine besten Bid/Ask-Kurse verfügbar sind, wird die Spread-Filterung übersprungen.
- Trailing- und Timeout-Ausstiege werden auf geschlossenen Kerzen ausgewertet, entsprechend der ursprünglichen MQL-Logik.
- Die Positionsgrößenbestimmung erfordert eine Portfoliobewertung (`Portfolio.CurrentValue`), wenn `RiskModes` auf Prozentsatz eingestellt ist. Wenn der Wert nicht verfügbar ist, greift die Strategie auf die konfigurierte Losgröße zurück.

## Dateien
- `CS/V1n1LonnyBreakoutStrategy.cs` – Strategie-Implementierung in C# für StockSharp.
- `README.md` – Diese Beschreibung auf Englisch.
- `README_zh.md` – 中文简介。
- `README_ru.md` – Russische Beschreibung.
