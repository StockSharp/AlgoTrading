# FatlSatlOsma-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel reproduziert die Logik des MetaTrader-Experten **Exp_FatlSatlOsma** mithilfe der StockSharp High-Level-API.  
Das ursprüngliche System arbeitet mit dem Fatl/Satl-Oszillator (ein benutzerdefinierter Indikator ähnlich dem MACD).  
Die Strategie sucht nach einer Richtungsänderung des Oszillators:

- Wenn der Oszillator zwei Balken lang steigt und der letzte Wert höher als der vorherige ist, wird eine Long-Position eröffnet und Short-Positionen werden geschlossen.
- Wenn der Oszillator zwei Balken lang fällt und der letzte Wert niedriger als der vorherige ist, wird eine Short-Position eröffnet und Long-Positionen werden geschlossen.

Der Oszillator wird durch den integrierten `MovingAverageConvergenceDivergenceSignal`-Indikator mit konfigurierbaren schnellen und langsamen Perioden implementiert.  
Standardwerte entsprechen den ursprünglichen FATL/SATL-Parametern.

## Details

- **Einstiegskriterien**: Oszillatorbeschleunigung.
- **Long/Short**: beide.
- **Ausstiegskriterien**: entgegengesetzte Beschleunigung.
- **Stops**: keine.
- **Standardwerte**:
  - `Fast` = 39
  - `Slow` = 65
  - `CandleType` = 12-Stunden-Zeitrahmen
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
