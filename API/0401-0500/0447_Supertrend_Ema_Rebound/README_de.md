# SuperTrend + EMA Rebound-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das System handelt in Richtung des SuperTrend und sucht nach Pullbacks zu einem
exponentiellen gleitenden Durchschnitt. Eine Position wird entweder eröffnet, wenn die
SuperTrend-Linie die Richtung wechselt, oder wenn der Kurs von der EMA zurückprallt,
während er im vorherrschenden SuperTrend-Bias verbleibt. Diese Kombination versucht,
das erste Bein einer neuen Bewegung sowie anschließende Korrekturen innerhalb eines
etablierten Trends zu erfassen.

Ein prozentuales Take Profit kann über das integrierte Schutzmodul aktiviert werden,
indem der Take-Profit-Typ auf "%" gesetzt wird. Die Standardwerte bevorzugen Long-
Trades, aber Short-Einstiege können ebenfalls aktiviert werden. Da die Strategie auf
Richtungswechseln beruht, ist sie am effektivsten in Trendmärkten, wo SuperTrend
schnell auf Momentum-Verschiebungen reagiert.

## Details

- **Einstiegskriterien**:
  - SuperTrend wechselt zum Aufwärtstrend, oder Kurs prallt über EMA bei Aufwärtstrend zurück.
  - SuperTrend wechselt zum Abwärtstrend, oder Kurs prallt unter EMA bei Abwärtstrend zurück.
- **Long/Short**: Long standardmäßig aktiviert, Short optional.
- **Ausstiegskriterien**:
  - Entgegengesetzter SuperTrend-Flip.
  - Optionales Take Profit über das Schutzmodul.
- **Stops**: Prozentuales Take Profit über Schutz; kein Stop Loss enthalten.
- **Standardwerte**:
  - ATR-Periode = 10, ATR-Faktor = 3.0.
  - EMA-Länge = 20, TP = 1.5%.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide (Long Standard)
  - Indikatoren: SuperTrend, EMA
  - Stops: Optionaler TP
  - Komplexität: Moderat
  - Zeitrahmen: Kurz/mittel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
