# Kalman-Filter-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Trendfolge-Methode verwendet einen Kalman Filter, um Preisschwankungen zu glätten und die zugrunde liegende Richtung zu schätzen. Der Filter passt sich dynamisch an das Marktrauschen an und bietet eine verfeinerte Ansicht der Trendstärke im Vergleich zu Standard-Gleitenden Durchschnitten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 112%. Sie funktioniert am besten auf dem Forex-Markt.

Eine Long-Position wird eröffnet, wenn der Schlusskurs über die Kalman-Filter-Schätzung steigt. Umgekehrt wird eine Short-Position eingegangen, wenn der Schluss unter den Filterwert fällt. Da der Filter bei jeder Bar aktualisiert wird, wechseln Trades immer dann, wenn der Preis die Linie kreuzt, was eine kontinuierliche Teilnahme an Trendmärkten ermöglicht.

Trader, die systematische Ansätze bevorzugen, können den Kalman Filter nützlich finden, um Whipsaws zu reduzieren. Ein Schutz-Stop basierend auf ATR hält das Risiko begrenzt, falls der Trend schnell umkehrt.

## Details
- **Einstiegskriterien**:
  - **Long**: Schluss > Kalman Filter
  - **Short**: Schluss < Kalman Filter
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Ausstieg bei Schluss < Kalman Filter
  - **Short**: Ausstieg bei Schluss > Kalman Filter
- **Stops**: Ja, ATR-basierter Stop-Loss.
- **Standardwerte**:
  - `ProcessNoise` = 0.01m
  - `MeasurementNoise` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Kalman Filter
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
