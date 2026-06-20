# Pairs-Trading-Strategie mit Länder-ETFs
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Mean-Reversion-Strategie handelt ein Paar von Länder-ETFs basierend auf dem Z-Score ihres Kursquotienten. Wenn der Quotient einen Schwellenwert überschreitet, eröffnet das System eine Long/Short-Position in der Erwartung, dass sich der Spread seinem Durchschnitt annähert.

Der Kursquotient wird mit einem gleitenden Fenster verfolgt und Positionen werden geschlossen, wenn der Z-Score das Ausstiegsniveau kreuzt.

## Details

- **Universum**: genau zwei Länder-ETFs.
- **Signal**: Z-Score des gleitenden Kursquotienten überschreitet `EntryZ`.
- **Ausstieg**: schließen wenn Z-Score auf `ExitZ` zurückkehrt.
- **Daten**: Tageskerzen, standardmäßig 60-Tage-Fenster.
- **Risikokontrolle**: Aufträge übersprungen, wenn der Handelswert unter `MinTradeUsd` liegt.
