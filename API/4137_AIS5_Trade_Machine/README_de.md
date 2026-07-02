# AIS5-Handelsmaschine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
AIS5 Trade Machine portiert den MetaTrader 4 Expertenberater `AIS5TM.mq4` in die StockSharp High-Level-Strategie API. Das Original
Das Programm konzentrierte sich auf die Erstellung von Marktprofilhistogrammen für zwei Zeitrahmen und die Bereitstellung einer halbautomatischen Ausführungskonsole. Die
Die Version StockSharp behält die Idee bei, starke und schwache Preiszonen aus der Tick-Volumen-Aggregation hervorzuheben, und verwandelt sie in eine
Automatisiertes Breakout-System mit adaptiver Risikokontrolle basierend auf der Average True Range (ATR).

Die Strategie abonniert zwei Kerzenströme:
* Ein **Profil-Zeitrahmen** (Standard 15 Minuten), der das Volumen akkumuliert, um starke und schwache Zonen zu erkennen.
* Ein **Handelszeitraum** (Standard 1 Minute), der nach volumenbestätigten Ausbrüchen außerhalb dieser Zonen sucht.

Positionen werden durch ATR-proportionale Stopps und skalierbare Ziele geschützt. Volumenkontraktionen lösen frühzeitige Ausstiege aus, um dies nachzuahmen
Überwachungsdisziplin im MT4-Code.

## Strategielogik
### Erkennung von Volumenzonen (Profilzeitrahmen)
* Jede abgeschlossene Kerze in einem höheren Zeitrahmen aktualisiert zwei einfache gleitende Durchschnitte (SMA) des Tick-Volumens.
* Eine Kerze wird als **starke Zone** bezeichnet, wenn ihr Volumen den Durchschnitt um den konfigurierbaren Multiplikator (`Strong Volume Mult`) überschreitet.
Der Schlusskurs der Kerze wird zum jüngsten starken Niveau.
* Eine Kerze wird als **Schwachzone** bezeichnet, wenn ihr Volumen unter den Durchschnitt dividiert durch den konfigurierten Teiler fällt
(`Weak Volume Divider`). Der Schlusskurs dieser Kerze wird zum jüngsten schwachen Niveau.
* Es nehmen nur fertige Kerzen teil. Die Strategie ignoriert Zonen, bis das Profil SMA vollständig gebildet ist, um vorzeitige Ereignisse zu vermeiden
Signale während der Aufwärmphase.

### Breakout-Einträge (Handelszeitraum)
* Der untere Zeitrahmen wartet darauf, dass sich sowohl sein Volumen SMA als auch der Indikator ATR vollständig bilden.
* Bei einem Long-Setup muss der Schlusskurs das letzte starke Niveau um die Summe der **Zonen-Basispunkte** und überschreiten
**Zone Step Points** Puffer (umgerechnet über den Preisschritt des Instruments). Die Kerze muss auch einen relativen Volumenanstieg liefern
zum Intraday-Durchschnitt.
* Ein Short-Setup spiegelt die Logik rund um das letzte schwache Niveau wider und erfordert einen Durchbruch über den kombinierten Puffer hinaus und eine Bestätigung
Volumenerweiterung.
* Der ursprüngliche MT4-Experte erlaubte manuelle Befehle und Multi-Order-Raster. Der StockSharp-Port behält ein Einzelpositionsmodell bei, also a
Auf einen Ausbruch wird nur dann reagiert, wenn die aktuelle Nettoposition flach ist.

### Exit-Management
* Beim Einstieg speichert die Strategie den Füllpreis, berechnet einen ATR-basierten Schutzstopp (ATR multipliziert mit `ATR Multiplier` und
festgeklemmt durch den Basiszonenpuffer) und legt das Ziel als Stoppdistanz multipliziert mit dem schwachen Volumenteiler fest. Das hält
Risiko und Ertrag sind auf die Volumenstruktur abgestimmt.
* Während eine Position offen ist, überwacht die Strategie jede abgeschlossene Handelskerze:
  * Wenn der Preis das Gewinnziel oder den Schutzstopp erreicht, wird die Position sofort abgeflacht.
  * Wenn das Tick-Volumen den Schwellenwert für schwaches Volumen unterschreitet, bevor eines der beiden Niveaus erreicht wird, wird der Handel frühzeitig geschlossen, um dies zu vermeiden
Verweilen in inaktiven Zonen.
* Wenn die Nettoposition auf Null zurückkehrt, wird der interne Zustand zurückgesetzt, sodass der nächste Ausbruch von Grund auf neu bewertet werden kann.

## Parameter
* **Profilkerze** – Kerzentyp, der das Volumenprofil speist (Standard: 15-Minuten-Kerzen).
* **Handelskerze** – kürzerer Zeitrahmen für Ausbrüche und Ausstiege (Standard: 1-Minuten-Kerzen).
* **Volumenrückblick** – Anzahl der Kerzen für beide Volumen-SMAs und den Zeitraum ATR.
* **Strong Volume Mult** – Multiplikator über dem durchschnittlichen Volumen, das eine starke Zone markiert (entspricht `Parameter.1` in MQL).
* **Schwächer-Volumen-Teiler** – Teiler unter dem durchschnittlichen Volumen, der Schwachstellen markiert und das Gewinnziel bestimmt (entspricht
`Parameter.2`).
* **ATR Multiplikator** – Skalierungsfaktor, der bei der Berechnung des adaptiven Stoppabstands auf ATR angewendet wird (entspricht `Parameter.3`).
* **Zonenbasispunkte** – Mindestpuffer in Punkten, der der Zonenebene hinzugefügt wird, bevor Ausbrüche überprüft werden (entspricht `ZoneBasePoints`).
* **Zone Step Points** – zusätzlicher Ausbruchspuffer in Punkten, der den Abstand von der Zone vor Eintritten vergrößert
ausgelöst (entspricht `ZoneStepPoints`).
* **Volume** – geerbt von der Basisklasse `Strategy`; definiert die Ordergröße für Markteintritte.

## Zusätzliche Hinweise
* Die Strategie greift automatisch auf einen Standardpreisschritt von `0.0001` zurück, wenn das Wertpapier keinen solchen vorgibt. Dadurch bleibt die
punktbasierte Parameter, die mit den meisten FX-Symbolen kompatibel sind.
* Alle Indikatorberechnungen basieren auf fertigen Kerzen, um der MT4-Implementierung zu entsprechen, die bei vollständig geschlossenen Balken funktionierte.
* Im Gegensatz zum ursprünglichen EA gibt es kein manuelles Bedienfeld oder dateibasierten Profillader. Zonen werden rein live neu aufgebaut
Kerzendaten, um den Port in sich geschlossen zu halten.
* Die StockSharp-Version enthält keine Python-Übersetzung.
