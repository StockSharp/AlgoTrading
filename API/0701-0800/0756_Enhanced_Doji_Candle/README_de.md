# Verbesserte Doji-Kerze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Doji-Kerzen mit einfachen Bestätigungsregeln und festem Risiko-Ertrags-Management. Sie tritt ein, wenn ein Doji erscheint und die Kerze oder ihr Vorgänger die Richtung bestätigt, indem er mit kleinen Dochten über die Eröffnung hinaus schließt. Schutzorders verwenden einen Stop-Loss in Pips und einen Take-Profit, der durch ein Risiko-Ertrags-Verhältnis definiert ist.

## Details

- **Einstiegskriterien**: Doji-Kerze (Körper <= 30% der Spanne). Wenn bullisch mit unterem Docht <=1% oder vorherige Kerze bullisch, Long gehen. Wenn bearisch mit oberem Docht <=1% oder vorherige Kerze bearisch, Short gehen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Take-Profit oder Stop-Loss, oder ein neuer Doji, der die Position schließt.
- **Stops**: Ja.
- **Standardwerte**:
  - `RiskRewardRatio` = 2.0m
  - `StopLossPips` = 5
  - `SmaPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Candlestick
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
