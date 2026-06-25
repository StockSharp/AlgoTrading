# Stopreversal Trailing Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Stopreversal Trailing Strategie reproduziert den MT5-Expert `Exp_Stopreversal.mq5`. Sie verwendet den benutzerdefinierten Stopreversal-Indikator, um eine dynamische Trailing-Stop-Linie um den ausgewählten Kerzenkurs zu bauen. Wenn der Preis diese Trailing-Linie nach oben durchbricht, behandelt die Strategie dies als bullische Umkehr, schließt optional Short-Positionen und eröffnet eine neue Long-Position. Ein Durchbruch nach unten erzeugt die symmetrische bärische Aktion. Signale können um eine konfigurierbare Anzahl geschlossener Balken verzögert werden, um das Verhalten des Original-Expert-Advisors zu replizieren.

## Details

- **Einstiegslogik**: reagiert auf Stopreversal-Indikator-Pfeile, die erzeugt werden, wenn der Preis den adaptiven Trailing Stop kreuzt.
- **Long/Short**: beide Richtungen werden mit unabhängigen Schaltern für Long- oder Short-Einstiege unterstützt.
- **Ausstiegslogik**: entgegengesetzte Stopreversal-Signale können bestehende Positionen schließen; schützende Stop-Loss- und Take-Profit-Niveaus sind ebenfalls verfügbar.
- **Stops**: statischer Stop-Loss und Take-Profit in Kursschritten plus indikatorgesteuerte Umkehrungen.
- **Datenquelle**: beliebiger Zeitrahmen; Standard verwendet 4-Stunden-Zeitrahmen-Kerzen und spiegelt den Multi-Timeframe-Aufruf des Original-Experts wider.
- **Signalverzögerung**: der Parameter `SignalBar` verzögert die Orderausführung um die angegebene Anzahl abgeschlossener Balken (Standard 1 Balken).
- **Risikomanagement**: optionale harte Stops in Kursschritten des Instruments; der Positionsschutzdienst wird beim Start aktiviert.
- **Indikatorparameter**: der Trailing-Offset `Npips` steuert den Abstand zwischen Preis und Stop; `PriceMode` wählt den vom Trailing Stop verwendeten Kerzenkurs.
- **Standardwerte**:
  - `Volume` = 1
  - `StopLossSteps` = 1000
  - `TakeProfitSteps` = 2000
  - `BuyPositionOpen` = true
  - `SellPositionOpen` = true
  - `BuyPositionClose` = true
  - `SellPositionClose` = true
  - `Npips` = 0.004
  - `PriceMode` = Close
  - `SignalBar` = 1

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzenabonnement für Stopreversal-Berechnungen und Handel. Standard ist ein 4-Stunden-Zeitrahmen. |
| `Volume` | Basisordergröße beim Einstieg in eine neue Position. |
| `StopLossSteps` | Abstand vom Einstieg zum Stop-Loss in Kursschritten; auf 0 setzen zum Deaktivieren. |
| `TakeProfitSteps` | Abstand vom Einstieg zum Take-Profit in Kursschritten; auf 0 setzen zum Deaktivieren. |
| `BuyPositionOpen` | Aktiviert das Öffnen von Long-Positionen bei einem bullischen Signal. |
| `SellPositionOpen` | Aktiviert das Öffnen von Short-Positionen bei einem bärischen Signal. |
| `BuyPositionClose` | Schließt bestehende Long-Positionen, wenn ein bärisches Signal empfangen wird. |
| `SellPositionClose` | Schließt bestehende Short-Positionen, wenn ein bullisches Signal empfangen wird. |
| `Npips` | Fraktionaler Multiplikator für den Trailing Stop zur Erweiterung oder Verengung des Umkehrabstands. |
| `PriceMode` | Angewendete Preisvariante (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet, einfacher Durchschnitt, Vierteldurchschnitt, Trendfolge oder Demark). |
| `SignalBar` | Anzahl vollständig geschlossener Kerzen, die vor der Reaktion auf ein Signal gewartet werden, entsprechend dem MT5-Parameter. |

## Filter

- **Kategorie**: Trendfolgende Umkehr
- **Richtung**: Bidirektional
- **Indikatoren**: Stopreversal (ATR-gestützter Trailing Stop)
- **Stops**: Statischer Stop-Loss und Take-Profit, optional
- **Zeitrahmen**: Konfigurierbar (Standard H4)
- **Saisonalität**: Keine
- **Neuronale Netze**: Nein
- **Divergenz**: Nein
- **Komplexität**: Mittel aufgrund der benutzerdefinierten Trailing-Logik
- **Risikolevel**: Einstellbar durch Stop-Abstand und Trailing-Offset
