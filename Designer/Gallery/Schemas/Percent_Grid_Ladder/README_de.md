# Strategiediagramm einer prozentualen Grid-Leiter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Diagramm baut ein symmetrisches Prozent-Grid um den ersten fertigen Fünfminutenschluss auf. Drei Kauf-Limits liegen unter und drei Verkauf-Limits über dem Anker; nach der ersten Ausführung werden die übrigen Stufen storniert und die Position prozentual geschützt.

![schema](schema.svg)

## Strategieübersicht

- Der Schluss der ersten fertigen Fünfminutenkerze wird als Anker gespeichert; Level1 und Bid/Ask-Mittelwert werden nicht verwendet.
- Ein Abstand von 1,5% erzeugt bis zu drei Kaufstufen unter und drei Verkaufsstufen über dem Anker.
- Grid Levels per Side aktiviert die Stufen eins bis drei, während Long- und Short-Schalter beide Seiten getrennt steuern.
- Die erste ausgeführte Order storniert alle übrigen Grid-Orders und startet 2% Gewinn- sowie 3% Verlustschutz.
- Ein Schutzausstieg storniert Reste, speichert den letzten Schluss als neuen Anker und registriert eine frische Leiter.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Bei aktiviertem Long werden Kauf-Limits über eine Einheit bei anchor × (1 − spacing × Stufe) gesetzt; eine bis drei untere Stufen sind aktiv.
- **Short-Einstieg**: Bei aktiviertem Short werden Verkauf-Limits über eine Einheit bei anchor × (1 + spacing × Stufe) gesetzt; eine bis drei obere Stufen sind aktiv.
- **Ausstieg**: Die erste Ausführung beginnt den einzigen aktiven Positionszyklus. Der Schutz schließt bei +2% oder −3% und verankert danach sechs neue Orders am letzten fertigen Schluss.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Grid Spacing, % | 1.5 | Prozentualer Abstand benachbarter Stufen; 1,5 bedeutet 1,5%. |
| Grid Levels per Side | 3 | Aktive Stufen je Seite, von eins bis zum Diagrammmaximum drei. |
| Enable Long | true | Aktiviert Kauf-Limits unter dem Anker. |
| Enable Short | true | Aktiviert Verkauf-Limits über dem Anker. |
| Take Profit, % | 2 | Gewinnabstand vom Einstieg für den Positionsschutz. |
| Stop Loss, % | 3 | Verlustabstand vom Einstieg für den Positionsschutz. |

## Diagrammdetails

- Die C#-Strategie hält virtuelle Niveaus und sendet Market-Orders, sobald ein Schluss sie erreicht. Das Diagramm nutzt bewusst echte Pending Limits, damit Orderbausteine und Stornierung sichtbar sind.
- Die Quelle kann nacheinander mehrere Niveaus auslösen. Hier gilt bewusst ein Positionszyklus: Eine Ausführung storniert alle anderen Stufen, handelt fest eine Einheit und wartet vor dem Neuaufbau auf den Schutz.
- Grid Levels per Side unterstützt eins bis drei. Da drei Stufen gezeichnet sind, erzeugen höhere Werte keine zusätzlichen Bausteine.
- Neuankern bedeutet Stornieren und anschließendes Registrieren, ohne Orderersetzung; Preisrundung ist für Replay-Instrumente ohne Preisschritt aus.
- Der exakte Kerzenschluss ist erster und späterer Anker, entsprechend dem Rücksetzpreis der Quelle und ohne Level1-Abhängigkeit.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
