# Good Mode RSI v2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt RSI-Extreme mit benutzerdefinierten Take-Profit- und Trailing-Schwellenwerten. Sie verkauft, wenn der RSI ein hohes Niveau übersteigt, und schließt, wenn der RSI auf einen Gewinnmitnahme-Wert fällt. Sie kauft, wenn der RSI auf ein niedriges Niveau sinkt, und schließt, wenn der RSI auf ein Gewinnziel steigt. In beiden Fällen folgt ein Trailing-Stop dem günstigsten Preis, um Gewinne zu schützen.

## Details

- **Einstiegskriterien**:
  - **Long**: `RSI < buy level`.
  - **Short**: `RSI > sell level`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: `RSI > take profit level buy` oder Trailing-Stop ausgelöst.
  - **Short**: `RSI < take profit level sell` oder Trailing-Stop ausgelöst.
- **Stops**: Trailing-Stop in Ticks.
- **Standardwerte**:
  - `RSI Period` = 2
  - `Sell Level` = 96
  - `Buy Level` = 4
  - `Take Profit Level Sell` = 20
  - `Take Profit Level Buy` = 80
  - `Trailing Stop Offset` = 100
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
