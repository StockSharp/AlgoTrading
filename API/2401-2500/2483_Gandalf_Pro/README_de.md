# Gandalf PRO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den Gandalf PRO Expert Advisor aus MQL. Sie berechnet zwei gleitende Durchschnitte (LWMA und SMA) auf dem
Kerzenschlusskurs und speist sie in die originale rekursive Glättungslogik ein, um einen zukünftigen Zielpreis zu prognostizieren.
Wenn sich das projizierte Ziel weit genug vom aktuellen Schlusskurs entfernt (15 Preisschritte oder mehr), öffnet die Strategie
eine Marktorder und speichert die aus der Prognose abgeleiteten Stop-Loss- und Take-Profit-Niveaus.

Long-Trades erfordern, dass das geglättete Ziel mindestens 15 Schritte über dem aktuellen Schlusskurs liegt; Short-Trades erfordern,
dass das Ziel um denselben Betrag unter dem Schlusskurs liegt. Stop-Losses werden in Preisschritten definiert und mithilfe des
Preisschritts des Instruments umgerechnet. Take-Profit-Niveaus entsprechen dem projizierten Ziel und werden bei jeder abgeschlossenen
Kerze überwacht. Die Risikomultiplikatoren skalieren das Basisvolumen der Strategie und ermöglichen einfache Money-Management-Regeln.

## Parameter
- Kerzentyp
- Kauf aktivieren
- Kauf-Länge
- Kauf-Preisfaktor
- Kauf-Trendfaktor
- Kauf-Stop-Loss
- Kauf-Risikomultiplikator
- Verkauf aktivieren
- Verkauf-Länge
- Verkauf-Preisfaktor
- Verkauf-Trendfaktor
- Verkauf-Stop-Loss
- Verkauf-Risikomultiplikator
