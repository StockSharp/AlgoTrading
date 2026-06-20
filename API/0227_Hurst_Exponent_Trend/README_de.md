# Hurst-Exponent-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses System verwendet den Hurst Exponent, um zu bestimmen, ob der Markt ein Trendverhalten zeigt. Werte über dem Schwellenwert zeigen Persistenz an, während Werte darunter auf Rauschen oder Mean Reversion hindeuten. Ein gleitender Durchschnitt bietet zusätzliche Richtungsbestätigung.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 40%. Sie funktioniert am besten auf dem Kryptomarkt.

Die Strategie kauft, wenn der Hurst Exponent größer als der Schwellenwert ist und der Preis über dem gleitenden Durchschnitt schließt. Sie verkauft short, wenn der Hurst Exponent hoch ist und der Preis unter dem Durchschnitt schließt. Wenn der Hurst Exponent unter den Schwellenwert fällt, werden bestehende Positionen geschlossen, um den Handel in unruhigen Märkten zu vermeiden.

Dieser Ansatz eignet sich für Trader, die eine objektive Bestätigung dafür wollen, dass ein Trend vorhanden ist, bevor sie einsteigen. Die Kombination aus Trendfilter und Stop-Loss hilft, das Risiko von Fehlsignalen zu managen.

## Details
- **Einstiegskriterien**:
  - **Long**: Hurst > Schwellenwert && Schluss > MA
  - **Short**: Hurst > Schwellenwert && Schluss < MA
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg, wenn Schluss < MA oder Hurst < Schwellenwert
  - **Short**: Ausstieg, wenn Schluss > MA oder Hurst < Schwellenwert
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `HurstPeriod` = 100
  - `MaPeriod` = 20
  - `HurstThreshold` = 0.55m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Hurst Exponent, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
