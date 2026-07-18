# VWAP Mean Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Diese Strategie handelt gegen Bewegungen weg vom volumengewichteten Durchschnittspreis. ATR wird verwendet, um zu messen, wie weit der Preis vom VWAP abweichen muss, bevor ein Umkehrhandel in Betracht gezogen wird.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 58%. Er funktioniert am besten auf dem Aktienmarkt.

Eine Long-Position wird eröffnet, wenn der Preis mehr als `K` mal den ATR unter den VWAP fällt. Eine Short wird eingegangen, wenn der Preis um denselben Betrag über den VWAP steigt. Trades werden beendet, sobald der Preis zur VWAP-Linie zurückkehrt.

Der Ansatz ist für Intraday-Trader konzipiert, die erwarten, dass die Preise um den VWAP oszillieren anstatt stark zu trenden. Stops, die als Vielfaches des ATR dimensioniert sind, helfen, Verluste zu kontrollieren, wenn die Bewegung gegen den Trade weitergeht.

## Details
- **Einstiegskriterien**:
  - **Long**: Close < VWAP - K * ATR
  - **Short**: Close > VWAP + K * ATR
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg wenn close >= VWAP
  - **Short**: Ausstieg wenn close <= VWAP
- **Stops**: Ja, ATR-basierter Stop.
- **Standardwerte**:
  - `K` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AtrPeriod` = 14
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: VWAP, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

