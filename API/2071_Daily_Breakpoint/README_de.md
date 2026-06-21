# Täglicher Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche vom täglichen Eröffnungskurs. Zu Beginn eines jeden neuen Tages wird der Eröffnungskurs gespeichert. Wenn der Preis sich von diesem Level um eine benutzerdefinierte Anzahl von Punkten entfernt und der vorherige Balken innerhalb eines konfigurierbaren Größenbereichs liegt, tritt die Strategie in die Ausbruchsrichtung ein.

## Einstiegslogik

- Wenn der vorherige Balken bullisch ist und der Preis um **Break Point** Punkte über die Tageseröffnung steigt, wird eine Long-Position eröffnet.
- Wenn der vorherige Balken bärisch ist und der Preis um **Break Point** Punkte unter die Tageseröffnung fällt, wird eine Short-Position eröffnet.
- Die Größe des vorherigen Balkens muss zwischen **Last Bar Min** und **Last Bar Max** Punkten liegen.
- Das Ausbruchsniveau muss innerhalb des Körpers des vorherigen Balkens liegen.

## Risikomanagement

- Optionaler **Take Profit** und **Stop Loss** werden in Punkten vom Einstiegspreis gemessen.
- Ein Trailing Stop kann mit den Parametern **Trailing Start**, **Trailing Stop** und **Trailing Step** aktiviert werden. Wenn der Preis sich um *Trailing Start* Punkte zugunsten bewegt, wird der Stop auf *Trailing Stop* Punkte vom Einstieg gesetzt und folgt dann in *Trailing Step*-Schritten.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| Candle Type | Zeitrahmen der verarbeiteten Kerzen. |
| Break Point | Abstand von der Tageseröffnung zum Auslösen eines Trades (Punkte). |
| Last Bar Min | Mindestgröße des vorherigen Balkens (Punkte). |
| Last Bar Max | Maximale Größe des vorherigen Balkens (Punkte). |
| Trailing Start | Preisbewegung zum Starten des Trailing Stops (Punkte). |
| Trailing Stop | Anfänglicher Trailing-Abstand (Punkte). |
| Trailing Step | Schritt zum Bewegen des Trailing Stops (Punkte). |
| Take Profit | Take-Profit-Abstand (Punkte). |
| Stop Loss | Stop-Loss-Abstand (Punkte). |

## Hinweise

Die Strategie arbeitet nur auf abgeschlossenen Kerzen und verwendet Marktaufträge für Ein- und Ausstiege. Sie speichert interne Variablen für die Daten des vorherigen Balkens und das Trailing-Stop-Level.
