# Tuyul Gap Ende der Woche
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Tuyul Gap End Of Week portiert den MetaTrader 5 Expertenberater `TuyulGAP` nach StockSharp. Die Strategie bereitet sich auf die wöchentliche Markteröffnung vor, indem sie am Freitagabend eine konfigurierbare Anzahl aktueller Kerzen scannt und ein Paar Breakout-Stop-Orders um das höchste Hoch und das niedrigste Tief platziert. Es ist nur eine Handelssitzung pro Woche zulässig; Sobald die Aufträge bereitgestellt wurden, wartet die Strategie darauf, dass der Preis eines der beiden Niveaus durchbricht. Jede offene Position, die ein sicheres Gewinnziel in der Kontowährung erreicht, wird sofort geschlossen und alle verbleibenden ausstehenden Aufträge werden am Montag storniert, um den Workflow für die nächste Woche zurückzusetzen.

## Strategielogik
* **Wöchentlicher Sitzungstrigger** – das Setup wird an einem konfigurierbaren Wochentag (standardmäßig Freitag) ausgeführt, wenn die Börsenuhr die konfigurierte Stunde erreicht. Während des Minutenfensters (standardmäßig 23:00–23:15 Uhr) bereitet die Strategie die Breakout-Levels einmal pro Sitzung vor.
* **Dynamische Ausbruchsniveaus** – das höchste Hoch und das niedrigste Tief der vorherigen `Lookback Bars` fertigen Kerzen definieren die Auslösepreise. Der Kaufstopp wird einen Tick über dem Hoch platziert, der Verkaufsstopp einen Tick unter dem Tief und ahmt den MetaTrader-Punktversatz nach.
* **Pending-Order-Hygiene** – wenn für die Woche bereits eine Stop-Order vorliegt, wird diese nicht neu erstellt. Die entgegengesetzte ausstehende Order bleibt aktiv, nachdem eine Seite ausgelöst wurde, sodass die Strategie in beide Richtungen der Lücke handeln kann.
* **Sicherer Gewinnausstieg** – offene Positionen werden bei jeder abgeschlossenen Kerze überwacht. Wenn der nicht realisierte Gewinn für eine Position das sichere Gewinnziel (in der Portfoliowährung) erreicht, wird er unabhängig von der Richtung auf den Markt abgeflacht.
* **Wöchentliches Zurücksetzen** – bei der ersten Kerze am Montag storniert die Strategie alle noch aktiven ausstehenden Aufträge und aktiviert die Sitzungsflagge wieder, sodass die Einrichtung am nächsten Freitag durchgeführt werden kann.

## Parameter
* **Volumen** – Ordervolumen für die Breakout-Stop-Orders.
* **Stop-Loss (Punkte)** – Abstand vom Einstiegspreis, ausgedrückt in Instrumentenpunkten, der zum Platzieren eines Schutzstopps nach der Eröffnung einer Position verwendet wird. Auf `0` setzen, um den Stopp zu deaktivieren.
* **Lookback-Balken** – Anzahl der überprüften fertigen Kerzen, um die wöchentlichen Höchst- und Tiefststände zu berechnen.
* **Setup Day Of Week** – Tagesindex (0=Sonntag … 6=Samstag), der die wöchentliche Einrichtung auslöst. Der Standardwert von `5` behält das ursprüngliche Freitagsverhalten bei.
* **Setup-Stunde** – Börsenstunde, die als Anker für die Bereitstellung der Breakout-Orders dient.
* **Setup-Minutenfenster** – Anzahl der Minuten nach `Setup Hour`, in denen das Setup gültig bleibt. Mit dem Standardwert `15` läuft die Strategie zwischen 23:00 und 23:15 Uhr.
* **Sicheres Gewinnziel** – minimaler nicht realisierter Gewinn pro Position (in Portfoliowährung), der einen sofortigen Marktausstieg auslöst.
* **Kerzentyp** – Zeitrahmen, der für den High/Low-Scan und die Überwachungsschleife verwendet wird.

## Zusätzliche Hinweise
* Die Stop-Loss-Order wird erst übermittelt, nachdem eine Position eröffnet wurde, da StockSharp das direkte Anhängen eines Schutzstopps an eine ausstehende Stop-Order nicht unterstützt.
* Volumen, Preis und Stop-Level werden mithilfe der Schritt- und Präzisionsinformationen des Wertpapiers normalisiert, die StockSharp bereitstellt.
* Für diese Strategie gibt es keine Python-Übersetzung. In diesem Paket ist nur die C#-Implementierung enthalten.
