# Einfacher ATR Stop Take
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Basic ATR Stop Take portiert den MetaTrader 4 Expert Advisor **„Basic ATR stop_take Expert Advisor“** zur StockSharp High-Level-Strategie API. Das System ist bewusst minimal: Es eröffnet genau eine Marktposition in der gewählten Richtung, berechnet einen Average True Range (ATR) für die Arbeitskerzen und fügt schützende Stop-Loss- und Take-Profit-Levels hinzu, die von ATR-Multiplikatoren abgeleitet werden. Sobald der Handel auf einer der beiden Ebenen abgeschlossen ist, bereitet sich die Strategie sofort auf den nächsten Aufbau in die gleiche Richtung vor.

## Strategielogik
### Indikatorfundament
* **Durchschnittliche wahre Reichweite (ATR)** – berechnet für den abonnierten Kerzentyp mit einem konfigurierbaren Lookback. Der Indikator misst die aktuelle Volatilität und skaliert sowohl die Stopp- als auch die Zielentfernung.

### Einreisebestimmungen
* Wird beim Schließen jeder fertigen Kerze ausgeführt, nachdem ATR vollständig gebildet wurde.
* Wenn keine Position offen ist und der Richtungsparameter auf **Kaufen** eingestellt ist, wird eine Marktkauforder mit dem konfigurierten Volumen gesendet.
* Wenn keine Position offen ist und der Richtungsparameter auf **Verkaufen** eingestellt ist, wird ein Marktverkaufsauftrag mit dem konfigurierten Volumen gesendet.
* Wenn Sie **Keine** wählen, werden neue Einträge deaktiviert, während bestehende Positionen bis zu ihrer Schließung verwaltet bleiben.

### Ausgangsregeln
* **ATR Stop-Loss** – Distanz entspricht `ATR × Stop Factor`. Bei Long-Positionen wird der Stop unterhalb des Einstiegs platziert; Bei Shorts wird es oberhalb des Einstiegs platziert. Wenn das Extrem der Kerze das Niveau überschreitet, wird die Position zum Marktwert geschlossen.
* **ATR Take-Profit** – Distanz entspricht `ATR × Take Factor`. Bei Long-Positionen liegt das Gewinnziel über dem Einstiegspunkt; Bei Shorts sitzt es unten. Bei Erreichen dieses Niveaus wird der Handel zum Marktwert geschlossen.
* Wenn einer der Multiplikatoren auf `0` gesetzt ist, ist die entsprechende Stufe deaktiviert; Die Strategie überwacht weiterhin den verbleibenden Füllstand, falls vorhanden.

### Positionsmanagement
* Es ist jeweils nur eine Position zulässig. Nach einem Ausstieg wartet die Strategie auf den nächsten Kerzenschluss, bevor sie in die gleiche Richtung wieder einsteigt.
* `StartProtection()` wird beim Start aufgerufen, damit externe manuelle Positionen vom Schutzsubsystem StockSharp überwacht werden.

## Parameter
* **Handelsrichtung** – Seite des Marktes, auf der gehandelt werden soll (`None`, `Buy` oder `Sell`).
* **Handelsvolumen** – Auftragsvolumen für den Einzelmarkteintritt.
* **ATR Zeitraum** – Anzahl der Kerzen, die in der ATR-Berechnung verwendet werden.
* **Stop-Faktor** – ATR-Multiplikator, angewendet auf die Stop-Loss-Distanz. Null deaktiviert den Schutzstopp.
* **Take-Faktor** – ATR-Multiplikator, angewendet auf die Take-Profit-Distanz. Null deaktiviert das Gewinnziel.
* **Kerzentyp** – Zeitrahmen der Kerzen, die für die ATR-Berechnung und Handelsverwaltung verwendet werden.

## Zusätzliche Hinweise
* Die Standardparameter replizieren das Verhalten von EA (Long-Only-Modus, 0,01 Lot-Volumen, ATR-Periode 14, Stoppfaktor 1,5, Take-Faktor 2,0).
* Bei Preisvergleichen werden Kerzenhochs und -tiefs verwendet, was bedeutet, dass Stop-Loss- und Take-Profit-Auslöser auftreten, sobald das Niveau innerhalb der Kerzenspanne durchbrochen wird.
* Bei der Strategie werden keine Positionen gestapelt oder umgekehrt; Stattdessen flacht es immer ab und wartet auf den nächsten Barschluss, bevor es eine neue Bestellung aufgibt.
* In diesem Paket wird nur die C#-Implementierung bereitgestellt; Für diese Strategie gibt es keine Python-Version.
