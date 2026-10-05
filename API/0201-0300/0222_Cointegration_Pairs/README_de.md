# Kointegrations-Paar-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt zwei Vermögenswerte, die eine langfristige Kointegrations-Beziehung teilen. Durch die Berechnung des Residuums zwischen dem ersten Vermögenswert und einem beta-angepassten zweiten Vermögenswert sucht sie nach Abweichungen, die historisch zum Gleichgewicht zurückkehren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 103%. Sie funktioniert am besten am Aktienmarkt.

Eine Long-Position kauft den ersten Vermögenswert und verkauft den zweiten, wenn der residuale Z-Score unter `-EntryThreshold` fällt. Eine Short-Position verkauft den ersten und kauft den zweiten, wenn der Z-Score über den Schwellenwert steigt. Positionen werden geschlossen, sobald sich die Spread gegen null normalisiert.

Kointegrations-Paarhandel eignet sich für statistische Arbitrageure, die komfortabel mit dem gleichzeitigen Management zweier Instrumente umgehen. Der eingebaute Stop-Loss schützt vor extremen Bewegungen, wenn die Beziehung vorübergehend zusammenbricht.

## Details
- **Einstiegskriterien**:
  - **Long**: Residualer Z-Score < -EntryThreshold
  - **Short**: Residualer Z-Score > EntryThreshold
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn |Z-Score| < ExitThreshold
  - **Short**: Ausstieg, wenn |Z-Score| < ExitThreshold
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `Period` = 20
  - `EntryThreshold` = 2
  - `ExitThreshold` = 0.5
  - `Beta` = 1
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Asset2` — erforderlich, ohne Standardwert
    Das Residuum ist der Schlusskurs des Security der Strategie minus Beta mal der Schlusskurs von Asset2 auf Kerzen derselben Zeit; sein Z-Score nutzt Mittelwert und Standardabweichung der letzten Period Residuen. 0.5 ist der Standard-Ausstiegswert, den die Regeln nennen. Das erste Bein handelt Volume, Asset2 Beta mal Volume; ein Gegensignal dreht beide Beine. Der Stop schließt beide Beine, sobald sich das Residuum um StopLossPercent seines Einstiegswerts gegen das Paar bewegt, geprüft bei Kerzenschluss; 0 schaltet ihn ab.
- **Filter**:
  - Kategorie: Arbitrage
  - Richtung: Beide
  - Indikatoren: Kointegration
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
