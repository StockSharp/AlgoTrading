# Scalpel EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine vereinfachte Konvertierung des ursprünglichen *Scalpel EA*, der für MetaTrader geschrieben wurde.
Sie kombiniert einen Commodity Channel Index (CCI) Filter mit Multi-Timeframe-Ausbruchsanalyse. Das Ziel ist es, in Richtung des kurzfristigen Momentums zu handeln, wenn mehrere höhere Zeitrahmen die Bewegung bestätigen.

## Logik

1. **Indikator** – CCI wird auf dem primären Zeitrahmen berechnet. Trades sind nur erlaubt, wenn der CCI-Wert innerhalb eines konfigurierbaren Bandes um null bleibt.
2. **Trendbestätigung** – Für 30-Minuten-, 1-Stunden- und 4-Stunden-Kerzen werden die aktuellen Hochs und Tiefs mit den vorherigen verglichen.
   - Long-Trades erfordern steigende Tiefs in allen drei Zeitrahmen.
   - Short-Trades erfordern fallende Hochs in allen drei Zeitrahmen.
3. **Ausbruch** – Der Einstieg wird ausgelöst, wenn der Schlusskurs der primären Kerze das Hoch (für Longs) oder das Tief (für Shorts) der vorherigen Kerze bricht.
4. **Risikokontrolle** – `StartProtection` platziert einen festen Take-Profit und Stop-Loss in Preiseinheiten.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `CciPeriod` | Periode des Commodity Channel Index. |
| `CciLimit` | Absoluter CCI-Schwellenwert. Einstiege sind nur innerhalb von ±Limit erlaubt. |
| `TakeProfit` | Take-Profit-Wert in Preiseinheiten. |
| `StopLoss` | Stop-Loss-Wert in Preiseinheiten. |
| `CandleType` | Primärer Zeitrahmen für den Handel (Standard 1 Minute). |

## Hinweise

- Die Strategie abonniert zusätzliche 30-Minuten-, 1-Stunden- und 4-Stunden-Kerzen zur Bewertung höherer Zeitrahmen-Trends.
- Das Volumen wird aus der Eigenschaft `Strategy.Volume` der Basisklasse entnommen.
- Es ist immer nur eine Position gleichzeitig offen. Gegenläufige Signale schließen die bestehende Position und eröffnen eine neue.
