# Ivan CCI-Mittelwertbildungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port des MetaTrader-Expertenberaters "Ivan", der CCI-Extremwerte mit Mittelwertbildungs-Einstiegen und einem geglätteten gleitenden Durchschnitt als Stop handelt. Die Strategie überwacht einen langfristigen CCI(100), um globale Kauf- oder Verkaufsregimes zu etablieren, legt optional zusätzliche Positionen bei Rücksetzern des CCI(13) auf, und verwaltet das Risiko mit Break-Even- und Trailing-Logik um einen geglätteten gleitenden Durchschnitt. Die Positionsgröße spiegelt das ursprüngliche Prozentrisiko-Modell wider, und ein Gewinnschutz-Koeffizient schließt das Buch, wenn das Eigenkapital sich vervielfacht.

## Details

- **Einstiegskriterien**:
  - **Globales Long-Signal**: CCI(100) steigt über `GlobalSignalLevel`, während kein Kaufregime aktiv ist. Eine Long-Marktorder wird mit dem initialen Stop beim Wert des geglätteten MA gesendet, vorausgesetzt der Stop liegt mindestens `MinStopDistance` unter dem Preis.
  - **Long-Mittelwertbildung**: Wenn `UseAveraging` aktiviert ist und das globale Kauf-Flag gesetzt ist, fügt jeder Rückgang des CCI(13) unter `-GlobalSignalLevel` ein weiteres Long mit derselben Stop-Vorlage hinzu.
  - **Globales Short-Signal**: CCI(100) fällt unter `-GlobalSignalLevel`, während kein Verkaufsregime aktiv ist, was einen Short-Einstieg auslöst, wenn der MA-Stop mindestens `MinStopDistance` über dem Preis liegt.
  - **Short-Mittelwertbildung**: Mit aktivem `UseAveraging` fügt eine Rally des CCI(13) über `GlobalSignalLevel` innerhalb eines Verkaufsregimes zur Short-Exposition hinzu.
- **Long/Short**: Handelt in beide Richtungen und kann Positionen innerhalb des aktiven Bias pyramidisieren.
- **Ausstiegskriterien**:
  - Das Zurückkreuzen in `±ReverseLevel` bei CCI(100) bricht beide Regimes ab und erzwingt eine flache Exposition.
  - Portfolio-Eigenkapital, das das `ProfitProtectionFactor`-fache des Startguthabens übersteigt, erzwingt die Liquidierung aller Positionen.
  - Das Erreichen des verfolgten Stop-Preises (Break-Even oder getrailter MA) schließt die Positionsseite.
- **Stops**:
  - Der initiale Stop kommt von einem `StopLossMaPeriod`-geglätteten gleitenden Durchschnitt (SMMA).
  - Break-Even verschiebt den Stop auf den Einstandspreis, sobald der Preis um `BreakEvenDistance` vorgerückt ist (auf null setzen, um zu deaktivieren).
  - Trailing zieht den Stop nur nach, wenn der MA um mindestens `TrailingStep` über den aktuellen Stop hinaus voranschreitet.
- **Filter**:
  - `UseZeroBar` repliziert die MT5-Option, entweder den frisch geöffneten Balken oder den letzten geschlossenen Balken für Signalvergleiche zu lesen.
  - `MinStopDistance` verhindert Trades, wenn der MA-Stop zu nah am Preis liegt.
- **Positionsgrößenbestimmung**:
  - Jede neue Order riskiert `RiskPercent` des aktuellen Portfolio-Werts geteilt durch den Abstand zwischen Preis und Stop, mit `MinimumVolume` als Sicherheitsuntergrenze.

## Parameter

- **Use Averaging** *(bool, Standard: true)* — Zusätzliche Mittelwertbildungs-Orders während eines aktiven globalen Regimes aktivieren.
- **Stop MA Period** *(int, Standard: 36)* — Periode des geglätteten MA, der zur Ableitung von Stop-Levels verwendet wird.
- **Risk %** *(decimal, Standard: 10)* — Prozentsatz des Konto-Eigenkapitals, der bei jedem neuen Trade riskiert wird.
- **Use Zero Bar** *(bool, Standard: true)* — Falls wahr, werden die neuesten Kerzenwerte verwendet; andernfalls stützen sich Signale auf den vorherigen geschlossenen Balken.
- **Reverse Level** *(decimal, Standard: 100)* — Absoluter CCI-Schwellenwert, der beide Regimes bricht und alle Positionen schließt.
- **Global Level** *(decimal, Standard: 100)* — Absoluter CCI-Schwellenwert, der ein neues globales Kauf- oder Verkaufssignal aktiviert.
- **Min Stop Distance** *(decimal, Standard: 0.005)* — Mindest-Preislücke zwischen Einstieg und MA-Stop (0.005 ≈ 50 Pips bei 5-stelligen FX-Paaren).
- **Trailing Step** *(decimal, Standard: 0.001)* — Mindestverbesserung erforderlich, bevor der MA-Trailing-Stop vorgerückt wird.
- **BreakEven Distance** *(decimal, Standard: 0.0005)* — Preisbewegung, die erforderlich ist, um den Stop auf den Einstandspreis zu verschieben; auf 0 setzen, um Break-Even zu deaktivieren.
- **Profit Protection** *(decimal, Standard: 1.5)* — Eigenkapital-Vielfaches, das eine vollständige Liquidierung zur Gewinnmitnahme auslöst.
- **Minimum Volume** *(decimal, Standard: 1)* — Fallback-Handelsgröße, wenn die risikobasierte Dimensionierung ein kleines oder null Volumen ergibt.
- **Candle Type** *(DataType)* — Für Indikatoren verwendete Kerzenserie (Standard: 15-Minuten-Zeitrahmen).

## Hinweise

- Distanzen wie `MinStopDistance`, `TrailingStep` und `BreakEvenDistance` werden in Preiseinheiten ausgedrückt und sollten an die Tick-Größe des Instruments angepasst werden.
- Die Strategie setzt synchrone Ausführungen von `BuyMarket`/`SellMarket`-Orders voraus; Ausführungseinstellungen anpassen, wenn Slippage oder Teilausführungen erwartet werden.
- Portfolio-basierte Dimensionierung erfordert einen verbundenen Portfolio-Adapter; andernfalls wird `MinimumVolume` für alle Orders verwendet.
