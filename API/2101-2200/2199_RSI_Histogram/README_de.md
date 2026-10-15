# RSI-Histogramm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet das RSI-Histogramm (Relative Strength Index), um Umkehrungen zu erkennen, wenn der Oszillator extreme Zonen verlässt. Das Histogramm färbt den RSI-Wert basierend auf zwei Schwellenwerten: einem hohen Niveau für die überkaufte Zone und einem niedrigen Niveau für die überverkaufte Zone. Wenn die Farbe von Grün (überkauft) zu Grau oder Rot wechselt, schließt die Strategie Short-Positionen und eröffnet eine Long-Position. Wenn die Farbe von Rot (überverkauft) zu Grau oder Grün wechselt, werden Long-Positionen geschlossen und eine Short-Position eröffnet.

Die Implementierung basiert auf der hochrangigen StockSharp-API und abonniert Kerzendaten eines ausgewählten Zeitrahmens. Ein RSI-Indikator verarbeitet die Kerzen und generiert Signale, wenn sein Wert die definierten Zonen verlässt. Optionale Parameter ermöglichen es, Ein- und Ausstiege für jede Seite separat zu aktivieren oder zu deaktivieren.

Die Strategie dient zu Bildungszwecken und demonstriert, wie man einen MQL-Expert Advisor in das StockSharp-Framework konvertiert.

## Details

- **Einstiegskriterien**:
  - **Long**: Der vorherige Balken war über dem hohen Niveau und der letzte Balken fiel darunter.
  - **Short**: Der vorherige Balken war unter dem niedrigen Niveau und der letzte Balken stieg darüber.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Ein entgegengesetztes Signal schließt die aktuelle Position, wenn erlaubt.
- **Stops**: Keine integrierten Stops; das `StartProtection`-Framework ist für die Hinzufügung vorbereitet.
- **Standardwerte**:
  - `RSI period` = 14
  - `High level` = 60
  - `Low level` = 40
  - `Timeframe` = 4 hours
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Optional
  - Komplexität: Einfach
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
