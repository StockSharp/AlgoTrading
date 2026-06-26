# MACD No Sample
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
MACD No Sample ist ein Port des MetaTrader 5-Expertenberaters `MACD No Sample`. Die Strategie kombiniert eine Steigungsüberprüfung des gleitenden Durchschnitts mit MACD-Signallinie-Crossovern und erzwingt eine minimale MACD-Amplitude in Pips. Wenn ein bullisches Setup bestätigt ist, wird bestehende Short-Exposition geschlossen, bevor Long eingegangen wird; bärische Setups machen das Gegenteil. Das Risikomanagement spiegelt den originalen EA mit pip-basierter Stop-Loss-, Take-Profit- und Trailing-Logik wider, plus einem optionalen risikobasierten Positionsgrößenmodus.

## Strategielogik
### Indikatorvorbereitung
* **Gleitender Durchschnittsfilter** – ein konfigurierbarer gleitender Durchschnitt (SMA, EMA, SMMA oder LWMA) auf einen auswählbaren Kerzenpreis (Schluss, Eröffnung, Hoch, Tief, Median, Typisch oder Gewichtet) angewendet. Die Steigung (`MA[0] > MA[1]` oder `<`) definiert die Trendrichtung.
* **MACD-Signal** – MACD wird aus unabhängigen schnellen/langsamen EMA-Längen und Signallänge berechnet, unter Verwendung des gewählten angewendeten Preises. Die rohen MACD- und Signallinien werden überwacht, um frische Crossover zu erkennen, und die absolute MACD-Amplitude wird mit einem pip-basierten Schwellenwert verglichen.

### Einstiegsregeln
* **Long-Einstiege**
  * Der gleitende Durchschnitt steigt auf der letzten fertigen Kerze.
  * MACD ist unter null, hat aber gerade die Signallinie von unten gekreuzt (aktueller MACD > aktuelle Signal, während vorheriger MACD < vorherige Signal).
  * Der absolute MACD-Wert übersteigt den konfigurierten Pip-Schwellenwert (in Preiseinheiten umgerechnet).
  * Bestehende Short-Positionen werden geschlossen, bevor eine Long-Order platziert wird.
* **Short-Einstiege**
  * Der gleitende Durchschnitt fällt auf der letzten fertigen Kerze.
  * MACD ist über null, hat aber gerade die Signallinie von oben gekreuzt (aktueller MACD < aktuelle Signal, während vorheriger MACD > vorherige Signal).
  * Der absolute MACD-Wert übersteigt den Pip-Schwellenwert.
  * Bestehende Long-Positionen werden geschlossen, bevor eine Short-Order platziert wird.

### Ausstiegsverwaltung
* **Fester Stop-Loss / Take-Profit** – optionale Pip-Abstände, die in Preisoffsets vom Einstiegspreis umgerechnet werden. Das Setzen eines der Parameter auf `0` deaktiviert das entsprechende Niveau.
* **Trailing Stop** – aktiviert sich, wenn der Trailing-Stop-Abstand positiv ist. Die Strategie verfolgt den besten seit dem Einstieg erreichten Preis und verschiebt den Stop um mindestens die Trailing-Step-Distanz (beide in Pips), ohne ihn jemals zu lockern.
* **Risikobasierte Dimensionierung (optional)** – wenn aktiviert, wird das Ordervolumen aus dem Portfolio-Wert, dem Stop-Loss-Abstand und dem konfigurierten Risikoprozentsatz abgeleitet. Volumina werden am `VolumeStep` des Instruments ausgerichtet und durch `MinVolume`/`MaxVolume` begrenzt.

## Implementierungshinweise
* Verwendet die High-Level-API über `SubscribeCandles()` mit einer manuellen Indikatorpipeline innerhalb des `ProcessCandle`-Callbacks; keine `GetValue`-Aufrufe von Indikatoren werden verwendet.
* Indikatoreingaben berücksichtigen die angewendeten Preisauswahlen und stützen sich auf StockSharpss gleitende Durchschnitts- und MACD-Indikatorimplementierungen.
* Pip-Größenerkennung spiegelt die originale EA-Logik wider, indem der Preisschritt bei Drei- und Fünf-Dezimalstellen-Instrumenten mit zehn multipliziert wird.
* Stop- und Trailing-Logik schließt die Position über Marktorders, wenn die berechneten Niveaus verletzt werden; keine separaten Stop-Orders werden registriert.
* Nur die C#-Implementierung wird bereitgestellt; es gibt keine Python-Version für diese Strategie.

## Parameter
* **Volume** – festes Handelsvolumen für Marktorders.
* **Stop Loss (pips)** – schützender Stop-Abstand; `0` deaktiviert ihn.
* **Take Profit (pips)** – Gewinnziel-Abstand; `0` deaktiviert ihn.
* **Trailing Stop (pips)** – Trailing-Abstand; `0` deaktiviert den Trailing.
* **Trailing Step (pips)** – minimale Pip-Verbesserung, bevor der Trailing Stop angepasst wird.
* **Position Sizing** – Wahl zwischen festem Volumen und risikoprozentbasierter Dimensionierung.
* **Risk Percent** – Portfolio-Prozentsatz, der bei aktiver Risikogrößenbestimmung verwendet wird.
* **MA Period / Method / Price** – Konfiguration für den gleitenden Durchschnittsfilter.
* **MACD Fast / Slow / Signal** – EMA-Längen für MACD.
* **MACD Price** – angewendeter Preis für die MACD-Berechnung.
* **MACD Level (pips)** – minimale absolute MACD-Amplitude zur Validierung eines Trades.
* **Candle Type** – Zeitrahmen, der die Indikatoraktualisierungen antreibt.
