# Kauf-Verkauf-Gitter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie implementiert einen einfachen Gitteransatz, bei dem immer eine Long- und eine Short-Position offen gehalten werden. Wenn der Markt sich weit genug bewegt, um den Take-Profit einer Seite zu erreichen, wird auch die entgegengesetzte Seite geschlossen und das nächste Gitterlevel mit einem größeren Volumen eröffnet. Das Volumen wächst geometrisch entsprechend dem Parameter `VolumeMultiplier`.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `TakeProfitPoints` | Take-Profit-Abstand gemessen in Preisschritten. |
| `InitialVolume` | Volumen für das erste Orderpaar. |
| `VolumeMultiplier` | Multiplikator, der auf das Volumen für jedes neue Gitterlevel angewendet wird. |
| `MaxTrades` | Maximale Anzahl erlaubter Gitterlevel. |
| `CandleType` | Kerzen-Datentyp zum Auslösen der Strategielogik. |

## Handelslogik

1. **Start** – Die Strategie abonniert die angegebene Kerzenserie und öffnet das erste Paar von Kauf- und Verkaufs-Marktorders.
2. **Überwachung** – Bei jeder abgeschlossenen Kerze wird der letzte Kurs gegen die Einstiegspreise geprüft. Wenn das Gewinnziel auf einer Seite erreicht wird, werden beide Positionen geschlossen.
3. **Gitterprogression** – Nach dem Schließen aller Positionen wird das nächste Gitterlevel mit einem durch `VolumeMultiplier` multiplizierten Volumen eröffnet.
4. **Grenzen** – Der Prozess wiederholt sich, bis `MaxTrades` Level eröffnet wurden.

Die Strategie verwendet keine Indikatoren oder komplexe Berechnungen, was sie für die Demonstration von Order- und Positionsmanagement in StockSharp geeignet macht.

## Hinweise

- Alle Kommentare im Code sind wie gefordert auf Englisch geschrieben.
- Die Strategie verwendet die High-Level-API mit `SubscribeCandles` für Marktdaten.
