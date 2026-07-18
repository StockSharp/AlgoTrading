# Autocorrelation Reversal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie analysiert die kurzfristige Preis-Autokorrelation, um zu beurteilen, ob aktuelle Bewegungen wahrscheinlich umkehren werden. Negative Autokorrelation deutet darauf hin, dass aufeinanderfolgende Preisänderungen dazu neigen, die Richtung zu wechseln, was Mean-Reversion-Bedingungen schafft.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 124%. Er funktioniert am besten auf dem Devisenmarkt.

Wenn die berechnete Autokorrelation unter den Schwellenwert fällt und der Preis unter einem gleitenden Durchschnitt liegt, kauft das System in Erwartung eines Rebounds. Wenn die Autokorrelation negativ ist und der Preis über dem Durchschnitt liegt, wird eine Short-Position eröffnet. Ausstiege erfolgen, sobald der Preis den Durchschnitt kreuzt oder die Autokorrelation über den Schwellenwert steigt.

Der Ansatz ist für Trader geeignet, die nach statistischen Vorteilen suchen, anstatt nach Chartmustern. Ein prozentualer Stop-Loss wird angewendet, um gegen anhaltende Trends zu schützen, die die erwartete Umkehr verletzen.

## Details
- **Einstiegskriterien**:
  - **Long**: Autocorrelation < Threshold && Close < MA
  - **Short**: Autocorrelation < Threshold && Close > MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn Close > MA oder autocorrelation > Threshold
  - **Short**: Ausstieg wenn Close < MA oder autocorrelation > Threshold
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `AutoCorrPeriod` = 20
  - `AutoCorrThreshold` = -0.3m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Autocorrelation, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

