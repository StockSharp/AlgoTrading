# Amstell SL Durchschnittsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertierung des MetaTrader-Expertenberaters `exp_Amstell-SL`. Das System eröffnet sowohl Long- als auch Short-Positionen sofort, fügt jedes Mal neue Orders hinzu, wenn sich der Preis gegenüber dem letzten Eintrag um eine feste Anzahl von Punkten bewegt, und verlässt sich auf virtuelle (softwaregesteuerte) Take-Profit- und Stop-Loss-Levels, um jedes Ticket einzeln zu verlassen.

## Strategielogik

- **Ersteingaben**: Wenn die Strategie startet und keine offenen Geschäfte vorhanden sind, werden ein Marktkauf (bei Brief) und ein Marktverkauf (bei Geldkurs) übermittelt.
- **Pyramidenbildung beim Drawdown**:
  - Long-Seite: Immer wenn der aktuelle Briefkurs `ReentryPoints` (Standard 10 Punkte) unter dem letzten Long-Einstiegspreis liegt, wird eine neue Kauforder im gleichen Volumen gesendet.
  - Short-Seite: Immer wenn das aktuelle Gebot `ReentryPoints` über dem letzten Short-Einstiegspreis liegt, wird eine neue Verkaufsorder mit demselben Volumen eröffnet.
- **Exit-Regeln (virtuelle Verwaltung)**:
  - Für jedes Long-Ticket überwacht die Strategie den besten Geld- und Briefkurs. Steigt der Bid um `TakeProfitPoints` gegenüber dem Orderpreis oder sinkt der Ask um `StopLossPoints`, wird die Position zum Marktwert geschlossen.
  - Für jedes Short-Ticket wird geprüft, ob der Brief um `TakeProfitPoints` niedriger oder das Gebot um `StopLossPoints` höher ist; In beiden Fällen wird der Verkaufsauftrag zum Marktwert gedeckt.
- **Verarbeitungsreihenfolge**: Exits werden vor allen neuen Einträgen ausgewertet und replizieren das MetaTrader-Skript, das weitere Aktionen stoppt, nachdem eine Position am aktuellen Tick geschlossen wurde.

## Parameter

- `TakeProfitPoints` – Distanz (in Preisschritten), die zum Schließen profitabler Positionen verwendet wird. Standard: `30`.
- `StopLossPoints` – Entfernung (in Preisschritten) für Schutzausgänge. Standard: `30`.
- `Volume` – Losgröße für jede neu eröffnete Bestellung. Standard: `0.01`.
- `ReentryPoints` – Gegenbewegung (in Preisschritten), die erforderlich ist, um eine zusätzliche Order auf der entsprechenden Seite zu stapeln. Standard: `10`.

## Zusätzliche Hinweise

- Der Punktwert wird von `Security.PriceStep` abgeleitet; Wenn er nicht von der Börse bereitgestellt wird, wird der Wert `1` verwendet.
- Die Strategie kann gleichzeitig Long- und Short-Strategie sein, da sie Kauf- und Verkaufstickets unabhängig verfolgt und so dem Absicherungsverhalten des ursprünglichen Fachberaters entspricht.
- Take-Profit- und Stop-Loss-Level werden virtuell durch Marktaufträge ausgeführt; Sie werden nicht in das Börsenauftragsbuch aufgenommen.
- Das Risiko steigt schnell, wenn der Preis stark in eine Richtung tendiert, da zusätzliche Aufträge eröffnet werden, ohne das vorherige Risiko zu verringern.
- Funktioniert am besten bei Symbolen, bei denen der Begriff „Punkt“ einem minimalen Preisanstieg entspricht, zum Beispiel bei großen Forex-Paaren mit Preisen im MetaTrader-Stil.
