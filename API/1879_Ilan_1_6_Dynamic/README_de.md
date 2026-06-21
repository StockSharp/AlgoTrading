# Ilan 1.6 Dynamic Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Ilan 1.6 Dynamic-Strategie ist ein klassischer Grid- und Martingale-Expertenberater. Sie eröffnet einen ersten Trade in einer gewählten Richtung und platziert zusätzliche Orders, sobald sich der Preis um einen festen Schritt gegen die Position bewegt. Das Volumen neuer Orders wächst geometrisch nach einem Lot-Exponenten. Alle Positionen im Korb werden geschlossen, wenn der Preis zum durchschnittlichen Einstiegspreis zuzüglich einer Take-Profit-Distanz zurückkehrt. Ein Trailing-Stop kann optional Gewinne schützen, wenn der Preis weit genug in die günstige Richtung läuft.

Der Algorithmus beruht ausschließlich auf Kursbewegungen und verwendet keine Indikatoren. Da die Positionsgröße nach jeder negativen Bewegung zunimmt, trägt das System ein hohes Risiko, kann aber schnelle Gegenbewegungen einfangen.

## Details

- **Einstieg**
  - Die erste Order wird in der konfigurierten Richtung eröffnet.
  - Zusätzliche Orders werden alle `PipStep` Punkte gegen die aktuelle Position hinzugefügt, bis zu `MaxTrades`.
  - Volumen jeder neuen Order = `InitialVolume * LotExponent^N`.
- **Ausstieg**
  - Alle schließen, wenn der Preis `AveragePrice ± TakeProfit` berührt.
  - Optionaler Trailing-Stop startet nach `TrailStart` Punkten Gewinn und folgt dem Preis im Abstand von `TrailStop`.
- **Positionsverwaltung**
  - Nur Long- oder nur Short-Serie zur gleichen Zeit.
  - Nach dem Schließen des Korbs startet die Strategie erneut in die Ausgangsrichtung.
- **Parameter**
  - `InitialVolume` – Volumen der ersten Order (Standard 1).
  - `LotExponent` – Multiplikator für nachfolgende Ordergrößen (Standard 1.6).
  - `PipStep` – Abstand in Punkten zwischen Grid-Ebenen (Standard 30).
  - `TakeProfit` – Gewinnziel vom Durchschnittspreis in Punkten (Standard 10).
  - `MaxTrades` – Maximale Anzahl aktiver Orders (Standard 10).
  - `StartLong` – Ersten Trade als Long eröffnen, wenn true (Standard true).
  - `UseTrailingStop` – Trailing-Stop aktivieren (Standard false).
  - `TrailStart` – Gewinn in Punkten zum Starten des Trailings (Standard 10).
  - `TrailStop` – Trailing-Abstand in Punkten (Standard 10).
  - `CandleType` – Zeitrahmen der Kerzen (Standard 1 Minute).
- **Filter**
  - Kategorie: Grid
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Optional
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
