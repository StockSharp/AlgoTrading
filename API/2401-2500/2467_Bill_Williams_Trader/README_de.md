# Bill Williams Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert eine vereinfachte Version von Bill Williams' Handelsansatz basierend auf dem **Alligator**-Indikator und **Fractals**.

## Funktionsweise

- Berechnet Alligator-Linien mit geglätteten gleitenden Durchschnitten (SMMA):
  - **Jaw**-Länge (Standard 13)
  - **Teeth**-Länge (Standard 8)
  - **Lips**-Länge (Standard 5)
- Erkennt bullische und bärische Fraktale auf abgeschlossenen Kerzen.
- **Kaufen**, wenn der Kurs über das letzte obere Fraktal ausbricht, das über der Teeth-Linie des Alligators liegt.
- **Verkaufen**, wenn der Kurs unter das letzte untere Fraktal fällt, das unter der Teeth-Linie des Alligators liegt.
- **Long-Positionen schließen**, wenn der Schlusskurs unter die Lips-Linie fällt.
- **Short-Positionen schließen**, wenn der Schlusskurs über die Lips-Linie steigt.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `JawLength` | Periode der Alligator-Jaw-SMMA | 13 |
| `TeethLength` | Periode der Alligator-Teeth-SMMA | 8 |
| `LipsLength` | Periode der Alligator-Lips-SMMA | 5 |
| `CandleType` | Kerzentyp für Berechnungen | 15-Minuten-Kerzen |

Alle Parameter können über die Strategieparameter-Schnittstelle optimiert werden.

## Verwendung

1. Lösung kompilieren:
   ```bash
   dotnet build
   ```
2. Strategie in der StockSharp-Umgebung starten und das gewünschte Instrument sowie den Zeitrahmen auswählen.

## Hinweise

Dieses Beispiel demonstriert die Verwendung der High-Level-API mit Indikatorbindungen und implementiert keine Positionsgrößenberechnung oder erweitertes Risikomanagement über einfache Ausstiege hinaus.
