# Berlin Range Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Berlin-Range-Index-Strategie filtert den Standard-Choppiness-Index mit einem ATR-basierten Faktor, um Trend- und Seitwärtsphasen hervorzuheben. Wenn der gefilterte Index unter einen Mindestschwellenwert fällt, öffnet die Strategie eine Position in Richtung der aktuellen Kerze. Positionen werden geschlossen, wenn der Index eine Seitwärtsbewegung oder einen sich abschwächenden Trend anzeigt.

## Details

- **Einstiegskriterien**:
  - Gefilterter Bereichsindex unterhalb von `ChopMin` und Kerzenrichtung bestimmt Long oder Short.
- **Ausstiegskriterien**:
  - Bereichsindex oberhalb von `ChopMax` oder sich abschwächender Trend.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 9
  - `ChopMax` = 40
  - `ChopMin` = 10
  - `AtrLength` = 14
  - `LowLookback` = 14
  - `UseNormalized` = true
  - `StdDevLength` = 14
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Choppiness Index, ATR, Standard Deviation
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
