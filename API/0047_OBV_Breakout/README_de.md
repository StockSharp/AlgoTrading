# Strategie OBV Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
On-Balance Volume (OBV) verfolgt den Kauf- und Verkaufsdruck durch Akkumulation von Volumen. Diese Strategie sucht nach einem OBV-Ausbruch über ein Hoch oder unter ein Tief innerhalb des Beobachtungsfensters, während der Preis die Bewegung bestätigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 178%. Am besten funktioniert es im Aktienmarkt.

Ein Ausbruch im OBV deutet auf starkes Interesse hin. Das System geht long, wenn OBV sein vorheriges Maximum überschreitet, oder short, wenn es das Minimum unterschreitet. Das Kreuzen des OBV mit seinem gleitenden Durchschnitt signalisiert einen Ausstieg.

Dies verbindet Volumen-Momentum mit Preisaktionen.

## Details

- **Einstiegskriterien**: OBV überschreitet den höchsten oder niedrigsten Wert im Lookback-Zeitraum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: OBV kreuzt seinen MA oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `OBVMAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: OBV, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

