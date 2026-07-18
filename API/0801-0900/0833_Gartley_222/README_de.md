# Gartley 222-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht long, wenn sich ein bullisches harmonisches Gartley 222-Muster bildet.
Das Muster wird mithilfe von Pivot-Hochs und -Tiefs erkannt, die durch Fibonacci-Verhältnisse validiert werden.

Eine Long-Position wird `PivotLength` Bars nach der Bestätigung eröffnet, wenn der Preis oberhalb von Punkt C schließt.
Der Schutz schließt die Position bei einem Fibonacci-Extensionsziel oder einem festen prozentualen Stop-Loss.

## Details

- **Einstiegskriterien**:
  - Bullisches Gartley 222-Muster bestätigt
  - Einstieg um `PivotLength` Bars verzögert
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - Stop-Loss oder Take-Profit
- **Stops**:
  - `Stop Loss %` unterhalb des Einstiegs
  - `TP Fib Extension` oberhalb des Einstiegs
- **Standardwerte**:
  - `Pivot Length` = 5
  - `Fib Tolerance` = 0.05
  - `TP Fib Extension` = 1.27
  - `Stop Loss %` = 2

- **Filter**:
  - Kategorie: Muster
  - Richtung: Nur Long
  - Indikatoren: Pivot points, Fibonacci
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
