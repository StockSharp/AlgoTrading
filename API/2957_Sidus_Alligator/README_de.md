# Sidus Alligator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Sidus-Strategie reproduziert die klassische MetaTrader-"Sidus"-Expertenberater-Logik in StockSharp. Sie kombiniert den Bill Williams Alligator-Indikator mit einem 14-Perioden Relative Strength Index (RSI)-Filter. Das System sucht nach einem RSI-Kreuz über oder unter die 50-Mittellinie, während alle drei Alligator-Gleitenden-Durchschnitte sich in die gleiche Richtung ausdehnen. Jeder Einstieg berechnet sofort Schutz-Stops und optionales Trailing-Management, ausgedrückt in Pip-Abständen, die den Preisschritt des Instruments respektieren.

## Indikatoren und Daten
- **Alligator-Linien**: Drei geglättete gleitende Durchschnitte, berechnet auf dem Kerzen-Median-Preis (Hoch + Tief ÷ 2) mit unabhängigen Längen und Vorwärts-Shifts für Kiefer, Zähne und Lippen. Aufeinanderfolgende Werte werden verglichen, um Aufwärts- oder Abwärtsausdehnung zu erkennen.
- **Relative Strength Index (RSI)**: Ein 14-Perioden-Oszillator, ausgewertet auf Schlusspreisen. Nur fertige Kerzen nehmen an der Entscheidung teil, um Vorausschau-Bias zu vermeiden.
- **Kerzen**: Jeder Zeitrahmen kann über den Parameter `CandleType` ausgewählt werden. Standardmäßig verwendet die Strategie Kerzen im Einer-Minuten-Zeitrahmen.

## Handelslogik
1. **RSI-Bestätigung**
   - Long-Setup: RSI kreuzt aufwärts durch 50 (`RSI[t-2] < 50` und `RSI[t-1] > 50`).
   - Short-Setup: RSI kreuzt abwärts durch 50 (`RSI[t-2] > 50` und `RSI[t-1] < 50`).
2. **Alligator-Neigungsfilter**
   - Long-Einstieg erfordert, dass die Kiefer-, Zahn- und Lippensteigungs-Differenzen zwischen den zwei vorherigen abgeschlossenen Werten (unter Berücksichtigung der Shifts) den `Delta`-Schwellenwert überschreiten.
   - Short-Einstieg erfordert, dass die gleichen Steigungen unter dem Schwellenwert liegen, was Kompression oder Rückgang anzeigt.
3. **Positions-Handling**
   - Wenn ein Long-Signal erscheint, werden Shorts zuerst geschlossen wenn `CloseOpposite = true`. Die Strategie kauft dann das konfigurierte `OrderVolume` zu Markt.
   - Wenn ein Short-Signal erscheint, werden Longs geglättet wenn von `CloseOpposite` erlaubt, gefolgt von einem Markt-Verkauf von `OrderVolume`.

## Ausstieg und Risikomanagement
- **Anfangs-Stop-Loss**: Berechnet vom vorherigen Kerzen-Extrem minus/plus `OffsetPips` (umgerechnet mit dem Preisschritt des Instruments). Stops werden übersprungen wenn das berechnete Level den Trade ungültig machen würde (z.B. nicht-positiver Abstand).
- **Take-Profit**: Optionaler Abstand definiert durch `TakeProfitPips`. Das Setzen des Parameters auf null deaktiviert das Ziel.
- **Trailing-Stop**: Wenn `TrailingStopPips` und `TrailingStepPips` beide positiv sind, wird der Stop vorgerückt sobald der Preis mindestens `TrailingStopPips + TrailingStepPips` zugunsten der Position bewegt. Der neue Stop wird `TrailingStopPips` vom höchsten Hoch (Longs) oder niedrigsten Tief (Shorts) platziert, das während der Kerze erreicht wurde.
- **Glättungslogik**: Stop-Loss, Take-Profit und Trailing-Logik werden bei jeder fertigen Kerze anhand von Hoch/Tief-Bereichen ausgewertet, um Intrabar-Berührungen zu simulieren.

## Parameter
- `OrderVolume` (Standard **0.1**): Trade-Größe in Lots oder Kontrakten.
- `OffsetPips` (Standard **3**): Abstand vom vorherigen Kerzen-Extrem zum Stop-Loss. Null deaktiviert den anfänglichen Stop.
- `TakeProfitPips` (Standard **75**): Take-Profit-Abstand. Null deaktiviert das Ziel.
- `TrailingStopPips` (Standard **5**): Trailing-Stop-Abstand. Muss positiv sein wenn Trailing aktiviert ist.
- `TrailingStepPips` (Standard **15**): Zusätzliche Bewegung erforderlich bevor der Trailing-Stop vorrückt. Muss positiv sein wenn Trailing aktiviert ist.
- `Delta` (Standard **0.00003**): Minimale Steigungs-Differenz für jede Alligator-Linie zwischen aufeinanderfolgenden Proben.
- `CloseOpposite` (Standard **false**): Wenn `true`, werden entgegengesetzte Positionen vor dem Öffnen eines neuen Trades geschlossen; wenn `false`, wartet die Strategie darauf, dass die aktuelle Position natürlich geglättet wird.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: Längen der geglätteten gleitenden Durchschnitte für Alligator-Kiefer, -Zähne und -Lippen (Standard 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: Vorwärts-Shifts (Standard 8/5/3), die beim Abrufen von Steigungsvergleichen verwendet werden.
- `RsiPeriod` (Standard **14**): RSI-Mittelungsfenster.
- `CandleType`: Kerzen-Datentyp/Zeitrahmen zur Anmeldung (Standard 1 Minute).

## Implementierungshinweise
- Pip-basierte Abstände passen sich automatisch an die Preisgenauigkeit des Instruments an: Fünf- und Dreistellen-Instrumente multiplizieren den Preisschritt mit zehn, um der MQL-Pip-Definition zu entsprechen.
- Alligator-Steigungsprüfungen basieren auf gespeicherten historischen Werten, die die konfigurierten Vorwärts-Shifts respektieren, und vermeiden manuelles Array-Management über einen minimalen Ringpuffer hinaus.
- Orders werden mit den High-Level-Helfern `BuyMarket` und `SellMarket` ausgeführt, sodass die Strategie auf die Signalgenerierung fokussiert bleibt, während StockSharp das Routing übernimmt.
