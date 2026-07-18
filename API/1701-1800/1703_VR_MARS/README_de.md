# VR MARS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Beispiel demonstriert eine vereinfachte Portierung des manuellen Trading-Panels **VR---MARS-EN** von MQL4 auf StockSharp.

Das ursprüngliche Skript bot fünf vordefinierte Lotgrößen und Schaltflächen zum Senden von Kauf- oder Verkaufsorders. Es zeigte auch mehrere Beschriftungen mit Trading-Statistiken. In dieser C#-Version wird das visuelle Panel entfernt, während die Grundidee — die Auswahl einer der fünf Lotgrößen und die Ausführung einer Marktorder — beibehalten wird.

## Parameter

- `Lot1` – Größe des ersten Lots.
- `Lot2` – Größe des zweiten Lots.
- `Lot3` – Größe des dritten Lots.
- `Lot4` – Größe des vierten Lots.
- `Lot5` – Größe des fünften Lots.
- `SelectedLot` – Zahl von 1 bis 5, die angibt, welche Lotgröße verwendet wird.
- `Buy` – wenn `true`, wird beim Strategie-Start eine Markt-Kauforder gesendet.
- `Sell` – wenn `true`, wird beim Strategie-Start eine Markt-Verkaufsorder gesendet.

Nur eines der Richtungs-Flags sollte gleichzeitig aktiviert sein. Wenn die Strategie startet, aktiviert sie den Positionsschutz und sendet die entsprechende Marktorder mithilfe von High-Level-Hilfsmethoden.

## Hinweise

Diese Strategie ist für Bildungszwecke gedacht und implementiert keine Handelslogik über die unmittelbare Orderausführung hinaus.
