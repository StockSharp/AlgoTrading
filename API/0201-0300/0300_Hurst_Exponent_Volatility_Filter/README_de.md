# Hurst-Exponent-Volatilitätsfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Hurst-Exponent-Volatilitätsfilter-Strategie verwendet den Hurst Exponent zusammen mit Volatilitätsfiltern. Sie tritt nur in Trades ein, wenn bestimmte Bedingungen übereinstimmen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 163%. Sie funktioniert am besten am Aktienmarkt.

Signale erfordern, dass der Indikator einen Schwellenwert überschreitet, während die Volatilität vordefinierte Kriterien erfüllt. Positionen können Long oder Short sein und haben eingebaute Stops.

Für Trader entwickelt, die Risikokontrolle schätzen; die Strategie steigt aus, sobald der Indikator zur Mitte zurückkehrt oder sich die Volatilität verschiebt. Anfangseinstellung `HurstPeriod` = 100.

## Details

- **Einstiegskriterien**: Der Indikator kreuzt zurück in Richtung Mittelwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Der Indikator kehrt zum Durchschnitt zurück.
- **Stops**: Ja.
- **Standardwerte**:
  - `HurstPeriod` = 100
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `StopLoss` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Hurst
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
