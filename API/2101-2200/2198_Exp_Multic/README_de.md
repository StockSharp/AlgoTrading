# Exp Multic-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Multi-Währungs-Strategie, die eine feste Auswahl wichtiger Forex-Paare ohne technische Indikatoren handelt.
Für jedes Paar verwaltet der Algorithmus eine Richtung und ein Volumen. Nach jeder profitablen Bewegung wird das Volumen erhöht; nach einem Verlust wird die Richtung umgekehrt. Der Handel wird gestoppt und alle Positionen werden geschlossen, sobald der Gesamtgewinn oder -verlust die angegebenen Schwellenwerte überschreitet.

## Details

- **Einstiegskriterien**:
  - Wenn keine Position vorhanden und das Kontokapital über `Margin` liegt, wird eine Position in der vordefinierten Richtung mit `MinVolume` eröffnet.
- **Long/Short**: Beide, je nach interner Richtung pro Paar.
- **Ausstiegskriterien**:
  - Position schließen, wenn der Gewinn `KClose * MinVolume` übersteigt.
  - Richtung umkehren und schließen, wenn der Verlust `KChange * aktuelles Volumen` übersteigt.
- **Stops**: Keine expliziten Stops; das Risiko wird durch Gewinn-/Verlustschwellen kontrolliert.
- **Standardwerte**:
  - `Loss` = 1900
  - `Profit` = 4000
  - `Margin` = 5000
  - `MinVolume` = 0.01
  - `KChange` = 2100
  - `KClose` = 4600
- **Filter**:
  - Kategorie: Geldmanagement
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Tick-basiert
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
