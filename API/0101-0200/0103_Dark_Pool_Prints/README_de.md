# Dark Pool Prints Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Handelt große außerbörsliche Transaktionsmeldungen in Richtung der gemeldeten Aggressorseite; SMA20 des gehandelten Instruments filtert den Trend.

`Security` bezeichnet das Handelsinstrument, `DarkPoolSecurity` einen getrennten Meldungsstrom für denselben Basiswert. Die Wertpapier-IDs müssen verschieden sein. Der Strom muss echte außerbörsliche Ticks (`IsSystem = false`), Preis, Volumen, Zeit und `OriginSide` liefern. Fehlende oder identische Quellen verhindern den Start. Börsliche Trades, Meldungen ohne Seite, ungültige Werte, Duplikate und verspätete Meldungen lösen keine Einstiege aus.

Das Volumen muss strikt größer sein als `1.5 × Mittelwert der vorherigen 20 Meldungsvolumen`; die aktuelle Meldung bleibt unberücksichtigt. Gemeldeter Kauf oberhalb SMA20 führt zum Kauf, gemeldeter Verkauf unterhalb SMA20 zum Verkauf. Kerzenfarbe und Kerzenvolumen ersetzen diese Informationen nicht. Ein Gegensignal dreht die vollständig ausgeführte Position mit `Volume + abs(Position)`.

Die SMA verwendet abgeschlossene 15-Minuten-Kerzen. Long wird unter SMA20 geschlossen, Short darüber, unabhängig vom Meldungsvolumen und Einstiegscooldown. Standard ist `CooldownBars = 0`; positive Werte verzögern weitere Einstiege um abgeschlossene Kerzen. Offene Aufträge blockieren überlappende Signale.

`StopLoss = 2` setzt lokalen nativen Schutz von 2% ab dem tatsächlichen Einstiegskurs. Der Schutz verfolgt Bid/Ask und sendet einen Marktausstieg; zwischen Kerzen ist ein Kursstrom erforderlich. Kein Take-Profit.
