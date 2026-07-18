# Volatilität Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ansatz handelt rund um Schwankungen in der Marktvolatilität. Wenn der ATR deutlich von seinem gleitenden Durchschnitt abweicht, deutet dies darauf hin, dass die Volatilität ungewöhnlich hoch oder niedrig geworden ist und sich möglicherweise umkehrt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 73%. Die Strategie funktioniert am besten im Kryptomarkt.

Die Strategie geht long, wenn der ATR unter den Durchschnitt minus `DeviationMultiplier` mal die Standardabweichung fällt und der Preis unter dem gleitenden Durchschnitt liegt. Sie geht short, wenn der ATR das obere Band überschreitet und der Preis über dem Durchschnitt liegt. Positionen werden geschlossen, sobald der ATR in Richtung seines mittleren Niveaus zurückkehrt.

Solche Setups eignen sich für Trader, die lieber Volatilitätsextreme handeln als Preisrichtungen. Ein Schutz-Stop-Loss wird verwendet, falls die Volatilität weiter zunimmt.

## Details
- **Einstiegskriterien**:
  - **Long**: ATR < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Short**: ATR > Avg + DeviationMultiplier * StdDev && Close > MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn ATR > Avg
  - **Short**: Ausstieg wenn ATR < Avg
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
