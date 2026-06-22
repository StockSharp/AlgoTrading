# Forex Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Übersetzung des MetaTrader Expert Advisors „Forex Profit". Die Strategie wartet auf die Ausrichtung dreier exponentieller gleitender Durchschnitte und die Bestätigung durch den Parabolic SAR, bevor sie beim Schluss jeder abgeschlossenen Kerze in Trades einsteigt. Das Risiko wird durch asymmetrische Stop-Loss- und Take-Profit-Abstände, einen Trailing-Stop und eine zusätzliche EMA-basierte Gewinnsperre kontrolliert.

## Details

- **Einstiegskriterien**:
  - Long: `EMA10` über `EMA25` und `EMA50`, `EMA10` des vorherigen Balkens bei oder unter `EMA50`, und Parabolic SAR unter dem vorherigen Schlusskurs.
  - Short: `EMA10` unter `EMA25` und `EMA50`, `EMA10` des vorherigen Balkens bei oder über `EMA50`, und Parabolic SAR über dem vorherigen Schlusskurs.
  - Signale werden nur einmal pro abgeschlossener Kerze ausgewertet.
- **Ausstiegskriterien**:
  - Long schließen, wenn `EMA10` unter seinen vorherigen Wert dreht *und* der aktuelle Gewinn `ProfitThreshold` übersteigt.
  - Short schließen, wenn `EMA10` über seinen vorherigen Wert dreht *und* der aktuelle Gewinn `ProfitThreshold` übersteigt.
  - Schützende Stop-Loss- und Take-Profit-Level werden bei der Order-Eröffnung gesetzt (unterschiedliche Abstände für Long vs. Short).
  - Trailing-Stop aktiviert sich, nachdem der Preis `TrailingStopPoints` über den Einstieg hinaus bewegt hat, und wird in `TrailingStepPoints`-Schritten aktualisiert.
- **Stops**: Ja — fester Stop-Loss, fester Take-Profit und Trailing-Stop-Verwaltung.
- **Standardwerte**:
  - `FastEmaLength` = 10
  - `MediumEmaLength` = 25
  - `SlowEmaLength` = 50
  - `TakeProfitBuyPoints` = 55
  - `TakeProfitSellPoints` = 65
  - `StopLossBuyPoints` = 60
  - `StopLossSellPoints` = 85
  - `TrailingStopPoints` = 74
  - `TrailingStepPoints` = 5
  - `ProfitThreshold` = 10
  - `SarAcceleration` = 0.02
  - `SarMaxAcceleration` = 0.2
  - `Volume` = 1
  - `CandleType` = 1-Stunden-Zeitrahmen
- **Zusätzliche Hinweise**:
  - Stop/Ziel-Abstände werden in Instrument-Preisschritten angegeben und mithilfe der Tick-Größe des Instruments automatisch konvertiert.
  - Gewinnbasierte Ausstiege basieren auf dem Gesamtgewinn der Position (einschließlich Volumen), der von Preis-Ticks in die Kontowährung umgerechnet wird.
  - Die Trailing-Logik hält den Stop hinter Preisbewegungen, ohne den konfigurierten Schritt zu überschreiten.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long/Short
  - Indikatoren: EMA, Parabolic SAR
  - Stops: Ja (fest + Trailing)
  - Komplexität: Mittel
  - Zeitrahmen: Konfigurierbar (Standard: 1 Stunde)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
