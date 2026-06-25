# SAR Trading v2.0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **SAR Trading v2.0-Strategie** recreiert den klassischen Cronex Expert Advisor innerhalb der StockSharp High-Level-API. Sie kombiniert einen einfachen gleitenden Durchschnitt (SMA) mit dem Parabolic SAR, um Einstiege zu timen, und verwaltet dann die Position mit festen Schutzorders und einem pip-basierten Trailing-Stop.

- Indikatoren: Simple Moving Average, Parabolic SAR.
- Standard-Zeitrahmen: 15-Minuten-Kerzen (konfigurierbar über `CandleType`).
- Markt: Jedes Instrument, das einen aussagekräftigen `PriceStep` (Pip)-Wert liefert.

## Handelslogik
- Die Strategie bewertet Einstiege nur, wenn keine Position offen ist.
- **Long-Setup:** entweder der Parabolic-SAR-Wert fällt unter den SMA oder der Schlusskurs von `MaShift` Balken zuvor liegt unter dem SMA. Dies spiegelt die MQL-Regel `SAR < MA || Close[shift] < MA` wider.
- **Short-Setup:** entweder der Parabolic-SAR-Wert steigt über den SMA oder der Schluss von `MaShift` Balken zuvor liegt über dem SMA.
- Nach dem Absenden einer Ausstiegsorder wartet der Algorithmus, bis die Position flach ist, bevor er neue Signale berücksichtigt, was dem Ein-Positions-Verhalten des ursprünglichen EA entspricht.

## Risikomanagement
- `StopLossPips` und `TakeProfitPips` wandeln Pips in absolute Preisdistanzen über `Security.PriceStep` um.
- `TrailingStopPips` hält den Schutz-Stop in einem festen Pip-Abstand hinter dem Preis, sobald der Trade im Gewinn ist.
- `TrailingStepPips` erfordert einen zusätzlichen Pip-Puffer, bevor der Trailing-Stop wieder verschoben wird, was die "Trailing-Step"-Logik aus dem MQL-Code emuliert.
- Wenn der Markt die Stop-Loss- oder Take-Profit-Level erreicht, wird die Position zu Marktpreisen geschlossen.

## Parameter
- `MaPeriod` (Standard **18**): Anzahl der vom SMA verwendeten Balken.
- `MaShift` (Standard **2**): wie viele Balken zurück der Schlusskurs beim Vergleich mit dem SMA gelesen wird.
- `SarStep` (Standard **0.02**): Parabolic-SAR-Beschleunigungsfaktor.
- `SarMaxStep` (Standard **0.2**): maximaler Parabolic-SAR-Beschleunigungsfaktor.
- `StopLossPips` (Standard **50**): feste Stop-Loss-Distanz in Pips.
- `TakeProfitPips` (Standard **50**): feste Take-Profit-Distanz in Pips.
- `TrailingStopPips` (Standard **15**): Trailing-Stop-Distanz in Pips.
- `TrailingStepPips` (Standard **5**): zusätzlicher Pip-Gewinn erforderlich, bevor der Trailing-Stop wieder verschoben wird.
- `CandleType`: Kerzenabonnement für die Berechnungen.

## Zusätzliche Hinweise
- Die Strategie pflegt eine interne Schlusskurshistorie, um den `iClose(shift)`-Aufruf aus der MQL-Version zu reproduzieren.
- Entscheidungen basieren ausschließlich auf abgeschlossenen Kerzen, was Konsistenz mit dem ursprünglichen Expert Advisor sicherstellt.
- Das Volumen wird aus der `Volume`-Eigenschaft der Strategie entnommen; standardmäßig sendet jedes Signal eine Market-Order mit einem Lot.
