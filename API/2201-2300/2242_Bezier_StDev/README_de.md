# Bezier Standardabweichung-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Wendepunkte in der Volatilität mithilfe eines Standardabweichungsindikators. Sie interpretiert lokale Minima und Maxima des Indikators als potenzielle Umkehrungen in der Preisbewegung. Wenn die Standardabweichung ein Tal bildet, erwartet das System eine Ausweitung der Volatilität nach oben und eröffnet eine Long-Position. Wenn ein Gipfel erscheint, wird eine Short-Position eröffnet in Erwartung einer Volatilitätskontrak­tion.

Der Ansatz ist standardmäßig für Long- und Short-Trades auf einem Vier-Stunden-Zeitrahmen ausgelegt. Er wendet keine Stop-Loss-Orders an und konzentriert sich stattdessen auf signalbasierte Ausstiege.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Standardabweichungswert am vorherigen Balken ist niedriger als seine Nachbarn (lokales Minimum).
  - **Short**: Der Standardabweichungswert am vorherigen Balken ist höher als seine Nachbarn (lokales Maximum).
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal löst eine Umkehr aus.
- **Stops**: Nein.
- **Standardwerte**:
  - `StdDev Period` = 9.
  - `Candle Type` = 4-Stunden-Kerzen.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Standardabweichung
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
