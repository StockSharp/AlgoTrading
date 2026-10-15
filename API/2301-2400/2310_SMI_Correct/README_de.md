# SMI Correct Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die SMI Correct Strategie implementiert ein Handelssystem basierend auf dem Stochastic Momentum Index (SMI) Indikator. Die Strategie beobachtet die SMI-Linie und ihre gleitende Durchschnittssignallinie. Eine Long-Position wird eröffnet, wenn der SMI unter die Signallinie kreuzt. Eine Short-Position wird eröffnet, wenn der SMI über die Signallinie kreuzt.

## Parameter
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- **SMI Length** – Anzahl der Perioden für die Stochastik-Berechnung.
- **Signal Length** – Glättungsperiode für die Signallinie.

## Funktionsweise
1. Die Strategie abonniert Kerzen des angegebenen Typs.
2. Für jede abgeschlossene Kerze aktualisiert sie den Stochastik-Oszillator und den Signal-Gleitenden Durchschnitt.
3. Wenn der SMI unter die Signallinie kreuzt, wird jede Short-Position geschlossen und eine Long-Position eröffnet.
4. Wenn der SMI über die Signallinie kreuzt, wird jede Long-Position geschlossen und eine Short-Position eröffnet.

Das Beispiel zeichnet auch Kerzen und Indikatorlinien auf einem Chart zur Visualisierung.
