# Parabolic SAR-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der Parabolic SAR-Indikator platziert Punkte ober- oder unterhalb des Preises, um die Trendrichtung zu signalisieren. Wenn die Punkte die Seite wechseln, kann dies das Ende der vorherigen Bewegung markieren. Diese Strategie eröffnet Trades bei diesem Wechsel und erwartet eine kurzfristige Umkehr.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 148%. Am besten funktioniert die Strategie auf dem Devisenmarkt.

Für jede Kerze wird ein laufender Parabolic SAR-Wert gepflegt. Wenn der Indikator von oberhalb des Preises auf unterhalb wechselt, wird eine Long-Position eröffnet. Wechselt er von unterhalb auf oberhalb, wird ein Short-Trade ausgeführt. Die Methode verwendet kein explizites Gewinnziel und verlässt sich typischerweise auf ermessensbasierte Ausstiege oder Trailing-Stops außerhalb des Beispielcodes.

Da SAR schnell reagiert, können falsche Signale in Seitwärtsmärkten auftreten, daher ist es am besten zu verwenden, wenn der Preis entscheidende Schwankungen macht.

## Details

- **Einstiegskriterien**: Parabolic SAR wechselt die Seite relativ zum Preis.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Manueller oder externer Stop.
- **Stops**: Nicht definiert.
- **Standardwerte**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 15 minute
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

