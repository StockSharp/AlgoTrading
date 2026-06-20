# Pairs-Trading-Aktien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese vereinfachte Pairs-Trading-Strategie operiert auf mehreren Aktienpaaren. Für jedes Paar wird der Kursquotient über ein gleitendes Fenster verfolgt und dessen Z-Score berechnet. Wenn der Z-Score einen Einstiegsschwellenwert überschreitet, wird ein Long/Short-Handel eröffnet; Positionen werden geschlossen, wenn der Z-Score zurückkehrt.

Der Algorithmus unterstützt den gleichzeitigen Handel mehrerer unabhängiger Paare.

## Details

- **Universum**: Liste von Aktienpaaren.
- **Signal**: Z-Score des Kursquotienten kreuzt `EntryZ`.
- **Ausstieg**: schließen wenn Z-Score `ExitZ` erreicht.
- **Daten**: Tageskerzen mit standardmäßig 60-Tage-Rückblick.
- **Risikokontrolle**: Handel übersprungen, wenn der Auftragswert unter `MinTradeUsd` liegt.
