# OzymandiaTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Ozymandias-Indikator. Der Indikator kombiniert ATR mit gleitenden Durchschnitten von Hochs und Tiefs, um einen dynamischen Kanal aufzubauen. Wenn die Richtung von bearish zu bullish wechselt, kauft die Strategie und schließt Short-Positionen. Ein Wechsel zu bearish verkauft und schließt Long-Positionen. Optionale Take-Profit- und Stop-Loss-Parameter steuern das Risiko.

## Details

- **Einstiegskriterien**: Richtungswechsel des Ozymandias-Indikators.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder konfigurierte Stops.
- **Stops**: Take-Profit und Stop-Loss.
- **Standardwerte**:
  - `Length` = 2
  - `CandleType` = TimeSpan.FromHours(4)
  - `TakeProfitPoints` = 2000
  - `StopLossPoints` = 1000
  - `BuyEntry` = true
  - `SellEntry` = true
  - `BuyExit` = true
  - `SellExit` = true
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Ozymandias (ATR + MA)
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
