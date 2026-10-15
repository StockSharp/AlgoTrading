# EMA Cross-Contest-Hedged-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den MetaTrader Expertenberater **EMA_CROSS_CONTEST_HEDGED** innerhalb von StockSharp. Der Roboter sucht nach einem bullischen/bärischen Crossover zwischen einem schnellen und einem langsamen exponentiellen gleitenden Durchschnitt (EMA) und überprüft optional das MACD-Histogramm als Trendbestätigung. Wenn ein Signal erscheint, eröffnet die Strategie sofort eine Marktposition und platziert eine Reihe von Stop-Orders, die den Handel absichern, indem sie mehr Risiko eingehen, wenn der Preis weiterhin im Trend liegt.

## Handelslogik
- Berechnen Sie ein kurzes EMA und ein langes EMA für die konfigurierte Kerzenserie. Signale können entweder vom zuvor abgeschlossenen Balken (Standard) oder vom aktuellen Balken übernommen werden, sobald die Kerze schließt.
- Erkennen Sie einen **bullischen Crossover**, wenn der Short-Kurs EMA über den Long-Kurs EMA steigt, und einen **bärischen Crossover**, wenn er unter den Long-Kurs EMA fällt.
- Optional muss die Zeile MACD für Long-Trades über Null und für Short-Trades unter Null liegen, um den Filter MQL zu replizieren.
- Wenn die bullische Bedingung erfüllt ist, kaufen Sie zum Marktwert, legen Sie Stop-Loss- und Take-Profit-Ziele fest und stellen Sie vier ausstehende Buy-Stop-Orders im Abstand der Absicherungsdistanz in die Warteschlange.
- Wenn die rückläufige Bedingung erfüllt ist, verkaufen Sie zum Markt, legen Sie Risikoziele fest und stellen Sie vier ausstehende Verkaufsstopp-Orders unter dem Preis in die Warteschlange.
- Ausstehende Bestellungen werden nach ihrer Ablaufzeit storniert, wenn sie nicht ausgelöst werden.
- Trailing-Stops verschärfen sich, wenn die offenen Gewinne steigen, und entgegengesetzte Crossovers können zu einem frühen Ausstieg führen, wenn `Use Close` aktiviert ist.

## Parameter
- **Kerzentyp** – für alle Berechnungen verwendeter Zeitrahmen.
- **Auftragsvolumen** – Handelsvolumen für die Anfangsposition und jede Absicherungsorder.
- **Take Profit (Pips)** – Take-Profit-Distanz in Pips.
- **Stop-Loss (Pips)** – Stop-Loss-Abstand in Pips.
- **Trailing Stop (Pips)** – Trailing-Stop-Distanz (0 deaktiviert Trailing).
- **Hedge Level (Pips)** – Abstand zwischen den abzusichernden ausstehenden Aufträgen.
- **Schließen verwenden** – bestehende Positionen schließen, wenn ein entgegengesetzter Crossover auftritt.
- **Verwenden Sie MACD** – erfordern Sie eine MACD-Bestätigung für Handelseinträge.
- **Ablauf(e)** – Gültigkeitsdauer für ausstehende Absicherungsaufträge.
- **Kurz EMA** – Länge des schnellen EMA.
- **Lange EMA** – Länge des langsamen EMA (muss größer sein als die des schnellen EMA).
- **Signalbalken** – Wählen Sie, ob Signale auf dem aktuellen Balken (0) oder dem vorherigen Balken (1) ausgewertet werden sollen.

## Notizen
- Alle Kommentare im Code werden wie gewünscht auf Englisch bereitgestellt.
- Die ausstehende Absicherungsstruktur folgt dem Verhalten des ursprünglichen MQL-Expertenberaters und platziert vier Aufträge in gleichen Abstandsschritten.
- Bei Preisumrechnungen aus Pips werden die `PriceStep` und `Decimals` des Symbols berücksichtigt, um den MetaTrader-Punktberechnungen zu entsprechen.
