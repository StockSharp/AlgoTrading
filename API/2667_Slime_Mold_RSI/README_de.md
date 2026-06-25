# Slime Mold RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine direkte Konvertierung des MQL4 Expert Advisors "Slime_Mold_RSI_v1.1". Die Strategie erstellt ein einzelnes Perzeptron, indem vier RSI-Werte (12, 36, 108 und 324) kombiniert werden, die auf dem Median-Preis berechnet werden. Jeder RSI-Wert wird vom ursprünglichen 0–100-Bereich auf -1…+1 normalisiert und mit einem konfigurierbaren Gewicht multipliziert. Ein Nulldurchgang der gewichteten Summe dreht die Position um.

## Funktionsweise
- Berechne den Median-Preis jeder fertigen Kerze und speise ihn in vier Relative Strength Index-Indikatoren mit Längen von 12, 36, 108 und 324 ein.
- Normalisiere jeden RSI-Wert auf das Intervall -1…+1 und wende das entsprechende Gewicht an. Die Standardwerte (-100) reproduzieren die ursprünglichen Perzeptron-Koeffizienten (`x - 100`).
- Summiere die vier gewichteten Eingaben, um den Perzeptron-Ausgang für die aktuelle Kerze zu erzeugen.
- Vergleiche den neuesten Wert mit dem Perzeptron-Ausgang der vorherigen Kerze, um Nulldurchgänge zu erkennen und Handelssignale zu generieren.

## Handelsregeln
- **Long-Einstieg**: Vorheriger Perzeptron-Wert ist unter null und der aktuelle Wert steigt über null. Die Strategie schließt jedes Short-Engagement und eröffnet eine Long-Position der Größe `Volume`.
- **Short-Einstieg**: Vorheriger Perzeptron-Wert ist über null und der aktuelle Wert fällt unter null. Die Strategie beendet jede Long-Position und eröffnet eine Short-Position der Größe `Volume`.
- **Positionsmanagement**: Es gibt keine expliziten Gewinnziele oder Stop-Loss-Orders. Positionen werden nur geändert, wenn ein neuer Nulldurchgang auftritt.

## Parameter
- `Weight1` – Koeffizient, der auf den normalisierten 12-Perioden-RSI-Eingang angewendet wird.
- `Weight2` – Koeffizient, der auf den normalisierten 36-Perioden-RSI-Eingang angewendet wird.
- `Weight3` – Koeffizient, der auf den normalisierten 108-Perioden-RSI-Eingang angewendet wird.
- `Weight4` – Koeffizient, der auf den normalisierten 324-Perioden-RSI-Eingang angewendet wird.
- `CandleType` – Zeitrahmen der der Strategie zugeführten Kerzen. Standard sind 1-Stunden-Kerzen.

## Details
- **Einstiegskriterien**: Nulldurchgang des gewichteten RSI-Perzeptrons.
- **Long/Short**: Beide (nach dem ersten Signal immer im Markt).
- **Ausstiegskriterien**: Der entgegengesetzte Nulldurchgang dreht die Position um.
- **Stops**: Keine.
- **Standardwerte**:
  - `Weight1` = -100
  - `Weight2` = -100
  - `Weight3` = -100
  - `Weight4` = -100
  - `CandleType` = 1-Stunden-Kerzen
- **Filter**:
  - Kategorie: Perzeptron / Oszillator
  - Richtung: Bidirektional
  - Indikatoren: RSI (Median-Preis)
  - Stops: Nein
  - Komplexität: Mittel (erfordert vier Langzeit-Indikatoren)
  - Zeitrahmen: Konfigurierbar (Standard Intraday stündlich)
  - Saisonalität: Nein
  - Neuronale Netze: Lineares Perzeptron
  - Divergenz: Nein
  - Risikolevel: Abhängig von gewähltem Volumen und Gewichten

## Hinweise
- Die Implementierung verfolgt den vorherigen Perzeptron-Ausgang auch wenn der Handel deaktiviert ist, um die Zustandskontinuität zu gewährleisten, sobald der Handel wiederaufgenommen wird.
- Der Median-Preis wird verwendet, um der `PRICE_MEDIAN`-Einstellung des ursprünglichen MetaTrader-Skripts zu entsprechen.
- Die Strategie dreht Positionen sofort um, also berücksichtigen Sie potenzielle Slippage bei der Wahl von Gewichten und Volumen.
