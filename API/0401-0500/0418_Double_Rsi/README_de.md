# Double RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Double RSI verwendet zwei Relative Strength Index-Berechnungen: eine auf dem
Handelschart und eine auf einem höheren Zeitrahmen. Trades werden nur ausgeführt,
wenn beide RSI-Werte dieselbe Richtung unterstützen, wodurch kurzfristige Einstiege
mit dem längerfristigen Momentum abgestimmt werden.

Der Hauptzeitrahmen sucht nach RSI-Kreuzungen aus überkauften oder überverkauften
Zonen. Wenn der RSI des höheren Zeitrahmens die Bewegung bestätigt, öffnet die
Strategie eine Position. Ein optionaler Take-Profit kann Gewinne nach einer
vordefinierten Bewegung sichern.

## Details
- **Daten**: Kurskerzen auf zwei Zeitrahmen.
- **Einstiegskriterien**:
  - **Long**: Niedrigerer-Zeitrahmen-RSI verlässt überverkaufte Zone UND höherer-Zeitrahmen-RSI ist bullisch.
  - **Short**: Niedrigerer-Zeitrahmen-RSI verlässt überkaufte Zone UND höherer-Zeitrahmen-RSI ist bärisch.
- **Ausstiegskriterien**: Entgegengesetztes RSI-Signal oder Take-Profit wenn `UseTP` wahr ist.
- **Stops**: Standardmäßig keine.
- **Standardwerte**:
  - `CandleType` = tf(5)
  - `RSILength` = 14
  - `MTFTimeframe` = tf(15)
  - `UseTP` = False
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long/Short
  - Indikatoren: RSI (multi‑timeframe)
  - Komplexität: Moderat
  - Risikolevel: Mittel
