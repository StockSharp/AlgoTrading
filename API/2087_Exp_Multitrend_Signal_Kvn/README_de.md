# Exp Multitrend Signal KVN-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert das MultiTrend Signal KVN-Konzept. Sie erstellt einen adaptiven Preiskanal mithilfe des Average Directional Index (ADX), um das Rückblickfenster zu bestimmen. Wenn der Preis oberhalb des Kanals schließt, eröffnet die Strategie eine Long-Position. Wenn der Preis unterhalb des Kanals schließt, wird eine Short-Position eröffnet.

Die Kanalbreite wird durch den Parameter **K** als Prozentsatz der Schwankung zwischen den jüngsten Hochs und Tiefs definiert. **KPeriod** legt die Basis-Anzahl der Balken für die Berechnungen fest, während der ADX-Wert das tatsächliche Fenster skaliert. **KStop** multipliziert die durchschnittliche Spanne und wird zu Ausbruchsgeschäften addiert, um den Stop-Abstand zu bestimmen.

Die Strategie ist sowohl für Long- als auch für Short-Handel ausgelegt und verwendet standardmäßig den 4-Stunden-Zeitrahmen. Es werden keine expliziten Stop-Loss- oder Take-Profit-Werte vorgegeben; der Schutz kann über die Plattform aktiviert werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Schlusskurs bricht über das obere adaptive Band.
  - **Short**: Der Schlusskurs bricht unter das untere adaptive Band.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Umgekehrtes Signal in der entgegengesetzten Richtung.
- **Stops**: Optional über den Strategie-Schutz.
- **Standardwerte**:
  - `K` = 48
  - `KStop` = 0.5
  - `KPeriod` = 150
  - `AdxPeriod` = 14
  - `Kerzentyp` = 4-Stunden-Kerzen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ADX, SMA, Max/Min
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
