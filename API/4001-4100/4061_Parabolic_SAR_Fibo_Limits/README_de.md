# Parabolic SAR Fibo-Limits
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Parabolic SAR Fibo Limits ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `FT_0tk80i9uw4ep_Parabolic`. Der ursprüngliche Roboter kombiniert einen dualen Parabolic SAR-Stack mit Fibonacci-Retracement-Levels, um Limit-Einträge in wichtigen Pullback-Zonen zu stufen. Die C#-Strategie behält die gestaffelte Auftragserteilung, den integrierten Break-Even- und Trailing-Schutz sowie den optionalen Handelssitzungsfilter bei, sodass das Verhalten mit der Quelle EA übereinstimmt, wenn es an ein Diagramm mit fertigen Kerzen angehängt wird.

## Strategielogik
### Signalvorbereitung
* **Duale Parabolic SAR-Ausrichtung** – zwei Parabolic SAR-Indikatoren werden im selben Zeitrahmen berechnet. Das schnelle SAR dient als Frühwarnung, während das langsame SAR die Zustandsänderung bestätigt. Wenn der schnelle SAR über den Preis springt, während der langsame SAR darunter bleibt, bereitet die Strategie ein potenzielles Long-Setup vor. Wenn der schnelle SAR unter den Preis fällt, während der langsame SAR darüber bleibt, ist ein potenzielles Short-Setup möglich. Die Setups werden gelöscht, sobald der langsame SAR den Preis in die entsprechende Richtung kreuzt.
* **Swing-Erkennung** – Die Strategie fragt das höchste Hoch und das niedrigste Tief über dem konfigurierbaren `Bar Search`-Fenster ab, um den `MaximumMinimum`-Helfer aus dem EA zu replizieren. Die zuvor fertige Kerze liefert das entgegengesetzte Extrem (`High[1]` oder `Low[1]`), das die Fibonacci-Berechnungen verankert.

### Auftragserteilung und -verwaltung
* **Fibonacci ausstehende Orders** – sobald beide SARs auf der gleichen Seite des Preises liegen und ein Setup aktiviert ist, sendet die Strategie eine Limit-Order auf dem 50 % Fibonacci-Niveau (`Entry Fibonacci %`) des erkannten Swings. Der Schutzstopp wird vom Swing-Extrem um die konfigurierte Anzahl von Punkten versetzt und der Take-Profit wird auf der erweiterten Fibonacci-Projektion (`Target Fibonacci %`) platziert. Aufträge werden nur angenommen, wenn der aktuelle Preis, der geplante Stopp und das Ziel mindestens fünf Preisschritte voneinander entfernt sind, was den `Point*5`-Sicherheitsfilter von EA widerspiegelt.
* **Automatische Orderbereinigung** – Immer wenn der schnelle SAR den Preis wieder überschreitet, wird die ausstehende Limitorder für diese Richtung storniert, um den Eintritt in die falsche Marktphase zu vermeiden. Durch die Ausführung einer Limit-Order wird die entgegengesetzte ausstehende Order automatisch storniert.

### Risikomanagement
* **Anfänglicher Stop und Ziel** – die Stop-Loss- und Take-Profit-Parameter der ausstehenden Order des EA werden durch Anwendung der berechneten Stop- und Zielniveaus emuliert, sobald die Limit-Order ausgeführt wird.
* **Break-Even-Verschiebung** – wenn `Break Even (points)` größer als Null ist, bewegt sich der Stop zum Einstiegspreis plus einem Preisschritt (oder minus einem Schritt für Shorts), sobald der Trade die angegebene Anzahl von Punkten erreicht, wodurch die ursprüngliche BBU-Routine reproduziert wird.
* **Trailing Stop** – wenn `Trailing Stop (points)` aktiviert ist, folgt der Stop dem Preis um die gewählte Distanz. Der Stopp wird nur aktualisiert, wenn der neue Stopp den vorherigen um mindestens `Trailing Step (points)` verbessert und dem `TrailingShag`-Verhalten von EA entspricht.
* **Manuelle Exit-Trigger** – wenn der Preis die berechneten Stop- oder Zielniveaus einer fertigen Kerze berührt, wird die Position mit einer Marktorder geschlossen, um die automatische Orderausführung von MT4 zu simulieren.

### Zeitfilter
* **Optionale Sitzungssteuerung** – Durch die Aktivierung von `Use Time Filter` werden neue Einträge auf das inklusive Zeitfenster zwischen `Start Hour` und `Stop Hour` in der Austauschzeit beschränkt. Die Schutzlogik (Break-Even, Trailing, Exits) funktioniert auch außerhalb der Sitzung weiter, genau wie in der MQL-Implementierung.

## Parameter
* **Zeitfilter verwenden** – schaltet den Handelssitzungsfilter um.
* **Startstunde / Stoppstunde** – inklusive Sitzungsstunden, die verwendet werden, wenn der Zeitfilter aktiviert ist.
* **Fast SAR Step / Fast SAR Max** – Beschleunigungsfaktor und maximale Beschleunigung für den schnellen Parabolic SAR.
* **Langsamer SAR Schritt / Langsamer SAR Max** – Beschleunigungsfaktor und maximale Beschleunigung für den langsamen Parabolic SAR.
* **Bar-Suche** – Anzahl der Balken, die in die Swing-Hoch/Tief-Berechnung einbezogen werden.
* **Offset (Punkte)** – Anzahl der Preisschritte, die bei der Berechnung des Stop-Loss über das Swing-Extrem hinaus hinzugefügt werden.
* **Eingabe Fibonacci %** – Fibonacci Prozentsatz (ausgedrückt als 0–200+), der für den Limit-Order-Preis verwendet wird.
* **Ziel Fibonacci %** – Fibonacci Prozentsatz, der zur Berechnung der Take-Profit-Prognose angewendet wird.
* **Break Even (Punkte)** – Gewinn in Punkten, der erforderlich ist, bevor der Stop auf den Einstiegspreis springt (+/- eine Stufe). Zum Deaktivieren auf `0` setzen.
* **Trailing Stop (Punkte)** – Abstand zwischen Preis und Trailing Stop. Auf `0` setzen, um das Nachstellen zu deaktivieren.
* **Trailing Step (Punkte)** – minimale Verbesserung (in Punkten), bevor der Trailing Stop vorgezogen wird.
* **Kerzentyp** – Zeitrahmen, der die Indikator- und Swing-Berechnungen steuert.
* **Volumen** – Basisauftragsvolumen, geerbt von der Klasse StockSharp `Strategy` (Standard `0.1`).

## Zusätzliche Hinweise
* Alle punktbasierten Parameter werden mithilfe der Preisstufe des Instruments automatisch in Preisoffsets umgewandelt. Fünfstellige FX-Symbole, Indizes und andere Assets verwenden daher die EA-Einstellungen ohne manuelle Skalierung wieder.
* Die Strategie verarbeitet nur fertige Kerzen, die vom konfigurierten Abonnement bereitgestellt werden, was genau der balkenweisen Ausführung von EA entspricht.
* Es gibt keine Python-Version dieser Strategie; Im Paket API ist nur die C#-Implementierung verfügbar.
