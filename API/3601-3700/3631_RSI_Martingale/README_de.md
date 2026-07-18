# Strategie RSI Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
RSI Martingale ist eine Portierung des MetaTrader 5 Expertenberaters `RSI&Martingale1.5`. Die Strategie sucht nach Momentumumkehrungen, indem sie wartet, bis der Relative Strength Index (RSI) innerhalb eines konfigurierbaren Lookback-Fensters einen Extremwert erreicht. Wenn ein Extrem auftritt, eröffnet es einen Handel in Richtung der erwarteten Mittelwertumkehr und endet, wenn RSI die Mittellinie von 50 überschreitet oder wenn ein festes Stop/Take-Ziel erreicht wird. Ein Martingale-Modul kann die Position nach einem Verlusthandel optional in die entgegengesetzte Richtung mit einem erhöhten Volumen wieder öffnen. Tägliche Gewinn- und Verlustlimits sowie stündliche Filter ermöglichen es, den Handel während riskanterer Sitzungen oder nach Erreichen der Kapitalerhaltungsziele auszusetzen.

## Strategielogik
### RSI Extreme
* **Indikator** – ein einzelner RSI, berechnet für den ausgewählten Kerzentyp. Der Indikator muss gebildet werden (ausreichend historische Daten), bevor Trades berücksichtigt werden.
* **Mindesterkennung** – wenn der letzte RSI-Wert kleiner oder gleich jedem RSI-Wert innerhalb des konfigurierten `Bars For Extremes`-Fensters ist und der Wert unter 50 liegt, eröffnet die Strategie eine Long-Position.
* **Maximale Erkennung** – wenn der letzte RSI-Wert größer oder gleich jedem Wert innerhalb des Lookback-Fensters ist und der Wert über 50 liegt, eröffnet die Strategie eine Short-Position.

### Positionsmanagement
* **Ausstiegsauslöser** – Positionen werden geschlossen, wenn RSI die neutrale 50-Linie auf die entgegengesetzte Seite überschreitet (Long-Positionen steigen über 50 aus, Short-Positionen steigen unter 50 aus).
* **Feste Ziele** – optionale Stop-Loss- und Take-Profit-Abstände, ausgedrückt in Pips. Wenn diese Option aktiviert ist, vergleicht die Strategie das Hoch/Tief der letzten Kerze mit diesen Zielpreisen und schließt die Position, wenn eines dieser Niveaus erreicht wird.
* **Volumenausrichtung** – jedes Auftragsvolumen wird vor der Übermittlung an die Schritt-, Mindest- und Höchsteinstellungen des Wertpapiers angepasst.

### Martingale Wiederherstellung
* **Auslöser** – Nachdem eine Position mit einem negativen Gewinn geschlossen wurde, merkt sich die Strategie die Richtung und das Volumen des Verlusthandels.
* **Wiedereintritt** – bei der nächsten geeigneten Kerze und nur wenn keine Position offen ist, kann sofort ein Handel in die entgegengesetzte Richtung eröffnet werden. Das Volumen ist entweder das Verlustvolumen multipliziert mit `Martingale Multiplier` oder dem Basiswert `Initial Volume`, abhängig vom Schalter `Enable Martingale`.
* **Zurücksetzen** – Sobald der Martingal-Befehl übermittelt wurde, werden die gespeicherten Verlustinformationen gelöscht, um wiederholte Versuche zu vermeiden.

### Tägliche Kapitalkontrolle
* **Baseline** – Die Strategie erfasst das Kontoguthaben zu Beginn jedes Handelstages und setzt die Aussetzungsmarkierung zurück.
* **Überwachungsfenster** – Tageslimits werden nur zwischen `Daily Control Start` und `Daily Control End` Stunden ausgewertet.
* **Aussetzung** – wenn das Eigenkapital über `Daily Profit %` steigt oder unter `Daily Loss %` fällt, schließt die Strategie alle offenen Positionen und überspringt neue Trades bis zum nächsten Tag.

### Sitzungsfilter
* **Handelsfenster** – neue Positionen sind nur zulässig, wenn die aktuelle Stunde zwischen `Trading Start` und `Trading End` (einschließlich) liegt.
* **Stundenvermeidung** – 24 boolesche Parameter spiegeln die „Nachrichtenvermeidungs“-Einstellungen der Quelle EA wider und blockieren den Handel während der ausgewählten Stunden.

## Parameter
* **Anfangsvolumen** – Basisauftragsvolumen für Standardeinträge.
* **RSI-Periode** – Anzahl der Perioden, die vom RSI-Indikator verwendet werden.
* **Bars For Extremes** – wie viele fertige Kerzen gescannt werden, wenn nach dem neuesten Minimum oder Maximum von RSI gesucht wird.
* **Take Profit (Pips)** – Abstand zum festen Take-Profit; zum Deaktivieren auf `0` setzen.
* **Stop-Loss (Pips)** – Abstand zum festen Stop-Loss; zum Deaktivieren auf `0` setzen.
* **Aktivieren Sie Martingale** – aktiviert das Martingale-Wiederherstellungsmodul nach einem verlorenen Trade.
* **Martingale Multiplikator** – Multiplikator, der auf das vorherige Verlustvolumen angewendet wird, wenn Martingal aktiv ist.
* **Tägliche Ziele** – schaltet die tägliche Gewinn-/Verlust-Aussetzungslogik um.
* **Tagesgewinn %** – Gewinnprozentsatz, der den Handel für den aktuellen Tag stoppt.
* **Täglicher Verlust %** – Verlustprozentsatz, der den Handel für den aktuellen Tag stoppt.
* **Täglicher Kontrollbeginn / Tägliches Kontrollende** – Stundengrenzen zur Auswertung der Tagesgrenzen.
* **Handelsbeginn / Handelsende** – Stundengrenzen, die neue Positionen ermöglichen.
* **Vermeiden Sie Stunde 00 … Vermeiden Sie Stunde 23** – Deaktivieren Sie den Handel während der entsprechenden Uhrstunde.
* **Kerzentyp** – Kerzenabonnement, das für den Indikator RSI und alle Berechnungen verwendet wird.

## Zusätzliche Hinweise
* Die Strategie gilt nur für fertige Kerzen und wertet keine Intrabar-Ticks aus.
* Tägliche Gewinnberechnungen kombinieren realisierte Strategie-PnL mit variablem PnL basierend auf dem letzten Schlusskurs.
* Das Paket enthält keine Python-Implementierung für diese Strategie. Es wird nur die C#-Version bereitgestellt.
