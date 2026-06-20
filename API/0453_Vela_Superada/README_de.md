# Vela Superada-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Vela Superada-Strategie handelt ein Zwei-Kerzen-Umkehrmuster. Ein bullisches Setup
entsteht, wenn einer bearischen Kerze unmittelbar eine bullische folgt, die über dem
vorherigen Eröffnungskurs schließt. Trades werden mit einem kurzfristigen EMA, RSI und
MACD-Trend gefiltert, um Gegentrend-Signale zu vermeiden. Sowohl Long- als auch Short-
Seiten können aktiviert werden.

Die Strategie setzt prozentuale Take-Profit- und Stop-Loss-Niveaus ein und zieht einen
Trailing Stop dynamisch nach, sobald sich der Kurs günstig entwickelt. Dies ermöglicht
es, ausgedehnte Bewegungen zu erfassen und gleichzeitig vor Umkehrungen zu schützen.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorherige Kerze bearisch, aktuelle bullisch, Schlusskurs und vorheriger Schlusskurs über EMA, RSI < 65, MACD steigend.
  - **Short**: Vorherige Kerze bullisch, aktuelle bearisch, Schlusskurs und vorheriger Schlusskurs unter EMA, RSI > 35, MACD fallend.
- **Long/Short**: Konfigurierbar (Long standardmäßig).
- **Ausstiegskriterien**:
  - Trailing Stop oder entgegengesetztes Signal.
- **Stops**: Prozentualer Stop Loss und Take Profit.
- **Standardwerte**:
  - `EmaLength` = 10
  - `RsiLength` = 14
  - `ShowLong` = True
  - `ShowShort` = False
  - `TpPercent` = 1.2
  - `SlPercent` = 1.8
- **Filter**:
  - Kategorie: Muster + Indikatoren
  - Richtung: Beide
  - Indikatoren: EMA, RSI, MACD
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
