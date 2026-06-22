# Vector-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Vector-Strategie ist ein Multi-Währungs-Trendfolgesystem, das vom MetaTrader 5 "Vector"-Experten konvertiert wurde. Es handelt vier große Forex-Paare — EURUSD, GBPUSD, USDCHF und USDJPY — gleichzeitig. Die Strategie berechnet geglättete gleitende Durchschnitte auf dem Medianpreis jedes Paares und öffnet synchronisierte Positionen, wenn der kombinierte Trend in dieselbe Richtung weist. Ein dynamisches Pip-Ziel basierend auf der Vier-Stunden-Volatilität und Gewinn- und Verlust-Schwellenwerte auf Portfolioebene steuern die Ausstiege.

## Kernideen
- Geglättete gleitende Durchschnitte (SMMA) auf Medianpreisen verwenden, um die Richtung jedes Währungspaares zu messen.
- Die schnellen und langsamen Durchschnitte aller Instrumente summieren, um eine gemeinsame bullische oder bearische Ausrichtung zu bestimmen.
- Eine einzige Marktorder pro Paar eingeben, wenn die globale Ausrichtung und der lokale Schnell-/Langsam-Crossover übereinstimmen.
- Positionen mit einem gleitenden Pip-Ziel verwalten, das aus dem durchschnittlichen Bereich von 50 abgeschlossenen 4-Stunden-Kerzen auf EURUSD abgeleitet wird.
- Alle Trades gleichzeitig schließen, wenn der gleitende Gewinn oder Verlust den konfigurierten Prozentsatz des Startguthabens erreicht.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| **Fast MA** | Länge des geglätteten gleitenden Durchschnitts für den schnellen Trend jedes Paares. |
| **Slow MA** | Länge des geglätteten gleitenden Durchschnitts für den langsamen Trend jedes Paares. |
| **MA Shift** | Zusätzliche Anzahl abgeschlossener Kerzen, die benötigt werden, bevor Signale ausgewertet werden, entsprechend der Shift-Einstellung im ursprünglichen EA. |
| **Equity Take Profit %** | Gleitender Gewinnprozentsatz, der das Schließen aller offenen Positionen auslöst. |
| **Equity Stop Loss %** | Gleitender Verlustprozentsatz, der einen Notausstieg für alle Trades auslöst. |
| **Signal Timeframe** | Kerzen-Zeitrahmen für die geglätteten gleitenden Durchschnitte (Standard 15 Minuten). |
| **Range Timeframe** | Kerzen-Zeitrahmen für die Volatilitätsmittelung (Standard 4 Stunden). |
| **Range Period** | Anzahl höherer Zeitrahmen-Kerzen für die Berechnung des durchschnittlichen Pip-Ziels. |
| **EURUSD / GBPUSD / USDCHF / USDJPY** | Wertpapiere, die den einzelnen gehandelten Instrumenten entsprechen. |

Alle Parameter unterstützen Optimierungsbereiche identisch zum ursprünglichen Expert Advisor, wo anwendbar.

## Handelslogik
1. **Indikatoraktualisierung** — Jede abgeschlossene Kerze auf einem Trading-Zeitrahmen aktualisiert die schnellen und langsamen geglätteten gleitenden Durchschnitte für das entsprechende Paar. Werte werden erst nach Abschluss des konfigurierten Aufwärmens (MA Shift) berücksichtigt.
2. **Bias-Berechnung** — Die Strategie summiert die letzten schnellen Durchschnitte aller Paare und subtrahiert die Summe der langsamen Durchschnitte. Ein positives Ergebnis deutet auf bullischen Druck hin, ein negatives auf bearischen Druck.
3. **Einstiegsbedingungen** — Wenn für ein Paar keine Position existiert, gibt die Strategie eine Kauforder ein, wenn die globale Ausrichtung bullisch ist und der schnelle Durchschnitt des Paares über dem langsamen liegt. In umgekehrtem Fall wird eine Verkaufsorder eröffnet.
4. **Pip-Ziel-Ausstieg** — Das EURUSD-Vier-Stunden-Abonnement berechnet den durchschnittlichen Kerzenbereich über den konfigurierten Zeitraum. Das aktuelle Pip-Ziel ist das größere dieses Durchschnitts und 13 Pips. Longs schließen, sobald der Preis mindestens die Zielanzahl an Pips gewinnt, und Shorts schließen nach einer entsprechenden günstigen Bewegung.
5. **Kapitalschutz** — Wenn der gleitende Gewinn den Take-Profit-Prozentsatz überschreitet oder der gleitende Verlust den Stop-Loss-Prozentsatz überschreitet, schließt die Strategie sofort alle verwalteten Positionen.

## Verwendungshinweise
- Fügen Sie die Strategie einem Portfolio hinzu, das Zugang zu allen vier Forex-Instrumenten bietet, und setzen Sie jeden Sicherheitsparameter explizit.
- Der Standard-Signal-Zeitrahmen ist 15 Minuten; stellen Sie sicher, dass passende Kerzen für jedes Währungspaar verfügbar sind.
- Pro Paar wird jederzeit nur eine offene Position gehalten. Der Volumenparameter der Basisstrategie wird für jeden Einstieg verwendet.
- Da Ausstiege auf dem gleitenden G/V beruhen, ist die Strategie für den Dauerbetrieb und nicht nur für Kerzen-für-Kerzen-Backtesting gedacht.
- Das dynamische Pip-Ziel verwendet die EURUSD-Volatilität entsprechend der ursprünglichen Implementierung. Passen Sie den Range-Zeitrahmen oder den Zeitraum an, wenn Sie das Ziel an eine andere Marktumgebung anpassen möchten.
