# Beta-adjustierte Pairs-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Beta-adjustierte Pairs-Trading-Strategie verwendet das Beta zusammen mit Volatilitätsfiltern. Sie tritt nur in Trades ein, wenn bestimmte Bedingungen übereinstimmen.

Signale erfordern, dass der Indikator einen Schwellenwert überschreitet, während die Volatilität vordefinierte Kriterien erfüllt. Positionen können Long oder Short sein und haben eingebaute Stops.

Für Trader entwickelt, die Risikokontrolle schätzen; die Strategie steigt aus, sobald der Indikator zur Mitte zurückkehrt oder sich die Volatilität verschiebt. Anfangseinstellung `Asset2` = (Security.

## Details

- **Einstiegskriterien**: Der Indikator kreuzt zurück in Richtung Mittelwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Der Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `Asset2` = (Security
  - `Asset2Portfolio` = (Portfolio
  - `BetaAsset1` = 1.0m
  - `BetaAsset2` = 1.0m
  - `LookbackPeriod` = 20
  - `EntryThreshold` = 2.0m
  - `StopLoss` = 2.0m
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Beta
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
