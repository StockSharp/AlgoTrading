# Ampel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Trendfolge-Ansatz, der einen Satz von gleitenden Durchschnitten verwendet, die wie eine Ampel eingefärbt sind, um die Handelsrichtung zu bestimmen.
Die Strategie wartet darauf, dass der Preis innerhalb einer vordefinierten Zone liegt, und prüft dann die Reihenfolge der Durchschnitte, bevor sie in den Markt einsteigt.

## Details

- **Einstiegszone**:
  - Standard: Der Preis muss zwischen dem roten (langsamen) und dem gelben (mittleren) SMA liegen.
  - Wenn `UseBlueRange` aktiviert ist: Der Preis muss zwischen den Hoch- und Tieflinien des blauen EMA-Kanals liegen.
- **Einstiegskriterien**:
  - Long: `green EMA > blueHigh EMA > yellow SMA > red SMA` und `price > green EMA`.
  - Short: `green EMA < blueLow EMA < yellow SMA < red SMA` und `price < green EMA`.
- **Ausstiegskriterien**:
  - Optional: Wenn `CloseOnCross` aktiviert ist, wird die Position geschlossen, wenn der grüne EMA den gelben SMA in der entgegengesetzten Richtung kreuzt.
- **Stops**: Optionaler Take-Profit und Stop-Loss gemessen in Preisschritten.
- **Long/Short**: Beide Richtungen.
- **Standardwerte**:
  - `RedMaPeriod` = 120
  - `YellowMaPeriod` = 55
  - `GreenMaPeriod` = 5
  - `BlueMaPeriod` = 24
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TakeProfitTicks` = 120
  - `StopLossTicks` = 60
  - `UseBlueRange` = false
  - `CloseOnCross` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gleitende Durchschnitte
  - Stops: Optional
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
