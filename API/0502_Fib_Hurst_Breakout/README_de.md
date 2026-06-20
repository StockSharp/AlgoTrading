# Fib Hurst Ausbruch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Fib Hurst Ausbruch kombiniert Fibonacci-Retracement-Niveaus aus dem Tages-Zeitrahmen mit einem Hurst-Exponenten-Filter. Das Kreuzen des Preises über die wichtigsten Fibonacci-Niveaus in Richtung des vorherrschenden Trends löst Einstiege aus, während ein 2%-Stop und ein 1:2-Risiko-Ertrags-Verhältnis das Risiko steuern.

## Details

- **Einstiegskriterien**:
  - Long: Schlusskurs kreuzt über das 61,8%-Niveau und täglicher Hurst > 0,5
  - Short: Schlusskurs kreuzt unter das 38,2%-Niveau und täglicher Hurst < 0,5
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: Ja
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `HurstPeriod` = 50
  - `MaxTradesPerDay` = 5
  - `MaxTotalTrades` = 510
  - `RiskPercent` = 2m
  - `RiskReward` = 2m
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Hurst, Fibonacci
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
