# Neue Zufallsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Neue Zufallsstrategie** emuliert den ursprünglichen MetaTrader-Experten "New Random" und bietet drei verschiedene Einstiegs-Auswahlmodi. Sie öffnet immer nur eine einzige Position und wartet, bis die aktuelle Position geschlossen ist, bevor sie die nächste Orderrichtung generiert. Markteinstiege werden bei Top-of-Book-Updates (Level-1-Daten) ausgelöst und verwenden die besten Bid/Ask-Kurse als Ausführungsanker. Die Strategie berechnet automatisch Stop-Loss- und Take-Profit-Abstände in Pips und passt sich an 3- und 5-stellige Forex-Kurse genauso wie die MQL-Version an.

## Einstiegsmodi
1. **Generator** – Die nächste Richtung wird durch einen Pseudozufallsgenerator gewählt, der beim Strategiestart geseeded wird. Jede Gelegenheit ist ein unabhängiger Münzwurf zwischen Kauf und Verkauf.
2. **Kauf-Verkauf-Kauf** – Positionen wechseln strikt zwischen Kauf und Verkauf. Die allererste Order ist ein Kauf, gefolgt von einem Verkauf, und so weiter.
3. **Verkauf-Kauf-Verkauf** – Positionen wechseln strikt, beginnend mit einem Verkauf, gefolgt von einem Kauf, und wiederholen sich.

## Parameter
- **Random Mode** (`Mode`) – Wählt einen der drei oben beschriebenen Einstiegsmechanismen aus. Standardmäßig der Zufallsgenerator.
- **Minimal Lot Count** (`MinimalLotCount`) – Multipliziert das minimale handelbare Volumen des Instruments. Ein Wert von `1` bedeutet, dass die Strategie genau `Security.VolumeMin` handelt, während höhere Werte die Ordergröße um ganze Vielfache skalieren.
- **Stop Loss (pips)** (`StopLossPips`) – Abstand in Pips unterhalb/oberhalb des ausgeführten Preises, bei dem die Strategie die Position beendet. Auf `0` setzen, um den Stop-Loss zu deaktivieren.
- **Take Profit (pips)** (`TakeProfitPips`) – Abstand in Pips, bei dem die Strategie Gewinne realisiert. Auf `0` setzen, um den Take-Profit zu deaktivieren.

## Handelslogik
1. Abonniert Level-1-Daten für das konfigurierte Wertpapier und speichert ständig die neuesten Bid-, Ask- und letzten Handelskurse.
2. Wenn keine Position offen und keine Order ausstehend ist, wertet die Strategie den ausgewählten Modus aus, um die nächste Richtung zu bestimmen.
3. Orders werden am Markt platziert, wobei der neueste beste Bid/Ask-Schnappschuss verwendet wird. Die Stop-Loss- und Take-Profit-Ziele werden sofort aus dem Einstiegskurs unter Verwendung der Pip-Distanzparameter berechnet.
4. Es darf nur eine einzige Position gleichzeitig existieren. Nachfolgende Einstiege werden unterdrückt, bis die aktive Position vollständig geschlossen ist.

## Positionsverwaltung
- Long-Positionen werden vorzeitig beendet, wenn der aktuelle Kurs auf den Stop-Loss fällt oder darunter, oder auf den Take-Profit steigt oder darüber.
- Short-Positionen werden beendet, wenn der aktuelle Kurs auf den Stop-Loss steigt oder darüber, oder auf den Take-Profit fällt oder darunter.
- Kursvergleiche verwenden immer die frischesten Level-1-Informationen: den letzten Handelskurs, wenn verfügbar, andernfalls den besten Bid/Ask für die jeweilige Seite.
- Nach dem Schließen eines Trades setzt die Strategie den internen Zustand zurück, wechselt optional die nächste Richtung (für Sequenzmodi) und wartet auf das nächste Kursupdate, bevor sie wieder einsteigt.

## Hinweise
- Die Strategie pyramidiert nie Positionen und hält das Verhalten für sequenzbasierte Modi deterministisch.
- Der Zufallsmodus wird mit dem aktuellen Tick-Zähler geseeded, so dass jeder Lauf einen einzigartigen Orderstrom erzeugt.
- Alle internen Kommentare und Protokolle sind auf Englisch, um den Repository-Richtlinien zu entsprechen.
