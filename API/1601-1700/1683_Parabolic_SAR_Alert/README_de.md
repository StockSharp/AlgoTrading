# Parabolic SAR-Alarm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht den Parabolic SAR (Stop and Reverse) Indikator, um potenzielle Trendumkehrungen zu erkennen. Wenn der SAR-Wert von oberhalb des Preises nach unterhalb wechselt, interpretiert der Algorithmus dies als bullisches Signal und eröffnet eine Long-Position. Wenn der SAR sich von unterhalb des Preises nach oben bewegt, wird eine Short-Position eröffnet.

Der Standard-Beschleunigungsfaktor (0.02) und die maximale Beschleunigung (0.2) folgen der klassischen Parabolic SAR-Konfiguration. Diese Parameter steuern, wie schnell sich der Indikator dem Preis nähert: höhere Werte lassen den SAR schneller reagieren, können aber zu Fehlsignalen führen. Die Strategie verarbeitet nur abgeschlossene Kerzen und speichert vorherige SAR- und Preiswerte, um Kreuzungen ohne historische Datenbankabfragen zu identifizieren.

Das Risikomanagement ist nicht explizit definiert; das Beispiel verlässt sich auf entgegengesetzte Signale zum Ausstieg. Zusätzlicher Schutz kann über die integrierten Mechanismen des Frameworks aktiviert werden.

## Details

- **Einstiegskriterien**: Parabolic SAR kreuzt den Schlusskurs.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal.
- **Stops**: Nicht definiert.
- **Standardwerte**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 5 minute
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Parabolic SAR
  - Stops: Optional
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
