# EMA/SMA + RSI Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verfolgt drei exponentielle gleitende Durchschnitte (schnell, mittel
und langsam) zusammen mit einem RSI-Filter, um an entstehenden Trends teilzuhaben.
Ein Trade wird ausgelöst, wenn der schnelle Durchschnitt den mittleren in Richtung des
vorherrschenden langsamen Durchschnitts kreuzt, was darauf hindeutet, dass das
Momentum zunimmt. Nur Kerzen, die in Richtung der Kreuzung schließen, werden
berücksichtigt, um Whipsaws zu vermeiden.

Ein schützender Ausstieg kann Positionen optional nach einer benutzerdefinerten Anzahl
von Bars schließen, wenn sie profitabel bleiben. Der RSI dient als Überkauft-/Überverkauft-
Schutz, um auszusteigen, wenn das Momentum zu weit gedehnt wird.

Backtests zeigen, dass die Technik am besten an liquiden Krypto-Paaren während
Trendphasen funktioniert, bei denen gleitende Durchschnitte eine klare Trennung bieten.

## Details

- **Einstiegskriterien**:
  - **Long**: `EMA_fast > EMA_medium` und `EMA_fast(t-1) <= EMA_medium(t-1)` und `Close > EMA_slow` und `Close > Open`
  - **Short**: `EMA_fast < EMA_medium` und `EMA_fast(t-1) >= EMA_medium(t-1)` und `Close < EMA_slow` und `Close < Open`
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: `RSI > 70` oder `X Bars im Gewinn und Close > entry`
  - **Short**: `RSI < 30` oder `X Bars im Gewinn und Close < entry`
- **Stops**: Keine.
- **Standardwerte**:
  - `EMA_fast` = 10
  - `EMA_medium` = 20
  - `EMA_slow` = 100
  - `RSI_length` = 14
  - `X bars` = 24
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, RSI
  - Stops: Optional zeitbasiert
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
