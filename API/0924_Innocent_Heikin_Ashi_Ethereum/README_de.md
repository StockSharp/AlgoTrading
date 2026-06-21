# Innocent Heikin Ashi Ethereum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft Ethereum, wenn auf eine Sequenz bärischer Kerzen unterhalb der EMA50 eine bullische Kerze oberhalb der EMA50 folgt. Der Stop Loss wird am niedrigsten Tief der letzten 28 Bars gesetzt, das Take Profit mit dem `RiskReward`-Multiplikator berechnet. Der optionale **Moon Mode** erlaubt Einstiege oberhalb der EMA200. Die Position kann bei Verkaufs- oder Fallensignalen vorzeitig geschlossen werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Mindestens `ConfirmationLevel` rote Kerzen unterhalb der EMA50, gefolgt von einer grünen Kerze oberhalb der EMA50.
  - **Aggressiv**: Wenn `EnableMoonMode` wahr ist und der Preis über der EMA200 liegt.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Stop Loss am niedrigsten Tief der letzten 28 Bars.
  - Take Profit mit dem `RiskReward`-Multiplikator.
  - Optionale Verkaufs- oder Fallensignale für frühzeitigen Ausstieg.
- **Stops**: Ja.
- **Standardwerte**:
  - `RiskReward` = 1.
  - `ConfirmationLevel` = 1.
  - `EnableMoonMode` = true.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
