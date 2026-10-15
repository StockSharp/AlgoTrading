# MACFibo-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert das MACFibo-Handelssystem. Sie wartet auf eine Kreuzung zwischen dem 5-Perioden-EMA und dem 20-Perioden-SMA. Nach der Kreuzung misst der Algorithmus den Schwung vom Schlusskurs des Kreuzungsbalkens (Punkt A) bis zum letzten Extrem (Punkt B) und erstellt Fibonacci-Expansionsniveaus. Positionen werden zum Marktpreis mit Take-Profit und Stop-Loss aus diesen Niveaus eröffnet. Ein optionaler Ausstieg schließt Verlustpositionen, wenn der schnelle EMA den mittleren SMA in entgegengesetzter Richtung kreuzt.

## Details

- **Eintrittsbedingungen:**
  - **Long:** 5 EMA kreuzt über 20 SMA. Punkt B ist das niedrigste Tief seit Beginn der Abwärtsbewegung.
  - **Short:** 5 EMA kreuzt unter 20 SMA. Punkt B ist das höchste Hoch seit Beginn der Aufwärtsbewegung.
- **Ausstiegsbedingungen:**
  - Take-Profit auf dem 161,8%-Fibonacci-Niveau oder der minimalen Take-Profit-Distanz.
  - Stop-Loss auf dem 38,2%-Fibonacci-Niveau oder der maximalen Stop-Loss-Distanz.
  - Optionaler Abschluss, wenn 5 EMA 8 SMA gegen die Position kreuzt und der Trade Verluste macht.
- **Filter:**
  - Handel nur zwischen konfigurierten Start- und Endzeiten.
  - Handel am Montag oder Freitag kann deaktiviert werden.
- **Parameter:**
  - `FastLength` – Länge des schnellen EMA.
  - `MidLength` – Länge des mittleren SMA für Schutzausstieg.
  - `SlowLength` – Länge des langsamen SMA für Trenderkennung.
  - `MinTakeProfit` – minimaler Take-Profit in Preiseinheiten.
  - `MaxStopLoss` – maximaler Stop-Loss in Preiseinheiten.
  - `StartHour` / `EndHour` – erlaubtes Handelszeitfenster.
  - `FridayTrade` / `MondayTrade` – Handel an diesen Tagen aktivieren.
  - `CloseAtFastMid` – Verlustpositionen bei Fast-Mid-Kreuzung schließen.
  - `CandleType` – Kerzentyp für Berechnungen.
