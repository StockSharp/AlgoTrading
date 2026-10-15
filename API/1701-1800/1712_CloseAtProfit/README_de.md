# Strategie zum Schließen bei Gewinn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überwacht den realisierten Gewinn und Verlust aller von der Strategie ausgeführten Trades. Wenn der aufgelaufene Gewinn einen benutzerdefinierten Schwellenwert überschreitet, schließt sie sofort alle offenen Positionen und storniert optional aktive Aufträge. Dasselbe Verhalten kann für Drawdowns aktiviert werden, indem ein Verlustlimit gesetzt wird.

Die Strategie analysiert keine Indikatoren oder Preisbewegungen. Stattdessen fungiert sie als Schutzschicht, die den Markt verlässt, sobald ein monetäres Ziel oder ein Stop-Niveau erreicht ist. Ein einfaches Kerzen-Abonnement wird nur für periodische Überprüfungen des aktuellen PnL-Werts verwendet.

## Parameter

- **UseProfitToClose** – Schließen durch Gewinnziel aktivieren oder deaktivieren. Standard: `true`.
- **ProfitToClose** – Gewinnwert in Währungseinheiten, der einen vollständigen Ausstieg auslöst. Standard: `20`.
- **UseLossToClose** – Schließen durch Verlustlimit aktivieren oder deaktivieren. Standard: `false`.
- **LossToClose** – Verlustwert in Währungseinheiten, der bei Überschreitung einen vollständigen Ausstieg auslöst. Standard: `100`.
- **ClosePendingOrders** – alle aktiven Aufträge beim Schließen von Positionen stornieren. Standard: `true`.
- **CandleType** – Kerzentyp für periodische Überprüfungen. Standard: `1`-Minuten-Zeitrahmen.

## Handelslogik

1. Kerzen des ausgewählten Zeitrahmens abonnieren.
2. Bei jeder abgeschlossenen Kerze den aktuellen realisierten PnL berechnen.
3. Wenn der Gewinn größer oder gleich `ProfitToClose` ist, die gesamte Position schließen und optional ausstehende Aufträge stornieren.
4. Wenn die Verlustüberwachung aktiviert ist und der aktuelle PnL kleiner oder gleich `-LossToClose` ist, die gesamte Position schließen und optional ausstehende Aufträge stornieren.

## Zusätzliche Hinweise

- Die Strategie schließt nur die Position des zugeordneten Wertpapiers.
- Ausstehende Aufträge werden mit der integrierten Methode `CancelActiveOrders` storniert.
- Die Logik kann mit anderen Einstiegsstrategien kombiniert werden, um Gewinnmitnahmen oder Portfolioschutz zu implementieren.

## Filter

- Kategorie: Risikomanagement
- Richtung: Beide
- Indikatoren: Keine
- Stops: Ja
- Komplexität: Grundlegend
- Zeitrahmen: Beliebig
- Saisonalität: Nein
- Neuronale Netze: Nein
- Divergenz: Nein
- Risikolevel: Mittel
