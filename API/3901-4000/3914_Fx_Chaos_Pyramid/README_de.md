# FX-Chaos-Pyramiden-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

FX Chaos Pyramid ist eine mehrstufige Breakout-Strategie, die vom MetaTrader 4 „FX-CHAOS“-Expertenberater mit Sitz in `MQL/8055` übernommen wurde. Der Port behält die ursprüngliche Logik mit mehreren Zeitrahmen bei: Die primäre Ausführung erfolgt im 4-Stunden-Zeitrahmen, während der tägliche Zeitrahmen die Breakout-Filter auf höherer Ebene bereitstellt. Die Eingaben werden mit dem Impulsfilter „Awesome Oscillator“ bestätigt, bevor die erste Stufe geöffnet wird. Zusätzliche Stufen gliedern sich in die bestehende Position ein, wenn der Trend im primären Zeitrahmen anhält.

Die StockSharp-Implementierung verwendet das High-Level-API mit Kerzenabonnements, Indikatorbindung und nativen Order-Helfern, sodass die Strategie sowohl für Backtesting als auch für den Live-Handel ohne zusätzlichen Infrastrukturcode verwendet werden kann.

## Handelslogik

### Filter für höhere Zeitrahmen

* Abonnieren Sie tägliche Kerzen und berechnen Sie den letzten bestätigten ZigZag-Schwung mit einem 5-Kerzen-Fraktaldetektor.
* Speichern Sie die Höchst- und Tiefstwerte des Vortages. Auf beiden Ebenen wird ein konfigurierbarer Puffer in Preisschritten hinzugefügt, bevor Breakout-Prüfungen durchgeführt werden.

### Ausführung des primären Zeitrahmens

* Abonnieren Sie 4-Stunden-Kerzen und binden Sie den Awesome Oscillator (Standardkonfiguration 5/34).
* Verfolgen Sie den neuesten fraktalen Schwung im 4-Stunden-Zeitrahmen als Analogon zum ursprünglichen benutzerdefinierten Indikator `zzf`.
* Erfassen Sie die erste 4-Stunden-Kerze, die für jeden neuen Handelstag geöffnet ist. Dieser Wert spielt die gleiche Rolle wie `iOpen(NULL, 1440, 0)` in MQL.

### Einreisebestimmungen

* **Anfängliche Long-Phase**: Der aktuelle Tag beginnt unter dem gepufferten vorherigen Tageshoch, der 4-Stunden-Schluss durchbricht über diesem gepufferten Niveau, der Preis bleibt immer noch unter dem letzten täglichen Aufwärtsfraktal und der Awesome Oscillator ist negativ. Bestehende Short-Positionen werden geschlossen, bevor die Long-Positionen eröffnet werden.
* **Anfängliche kurze Phase**: Spiegellogik mit dem Tagestief und dem Awesome Oscillator über Null.

### Pyramidenstufen

Nachdem die Anfangsphase gefüllt ist, wertet die Strategie jede abgeschlossene 4-Stunden-Kerze aus:

* Eine Long-Addition wird platziert, wenn die Kerze unterhalb des gepufferten vorherigen 4-Stunden-Hochs öffnet und darüber schließt, während der Schlusskurs unter dem letzten Aufwärtsfraktal des primären Zeitrahmens bleibt.
* Eine kurze Addition nutzt das gepufferte 4-Stunden-Tief und das letzte Abwärts-Fraktal.
* Optionaler Eigenkapitalfilter: Weitere Stufen sind nur zulässig, wenn das Portfolioeigenkapital größer als der Saldo ist, was der `AccountEquity() > AccountBalance()`-Anforderung des MQL-Experten entspricht.

Die Anzahl der zusätzlichen Stufen ist konfigurierbar (bis zu fünf, passend zur ursprünglichen Losmatrix). Die Stufen werden zurückgesetzt, wenn die Position geschlossen wird oder wenn ein Umkehrsignal die Gegenseite schließt.

## Money-Management

Der ursprüngliche Experte passt die Losmatrix je nach Kontostand an. Dieser Port behält die gleichen stückweisen Definitionen bei und stellt den Basissaldo, den Saldoschritt und den globalen Volumenmultiplikator als Parameter bereit. Das aktuelle Portfolio-Eigenkapital wird einem `MAX_Lots`-Bucket zugeordnet (im Bereich von 3,0 bis 15,0 Lots) und der entsprechende Lot-Vektor wird ausgewählt:

| `MAX_Lots` Bereich | Stufe 1 | Stufe 2 | Stufe 3 | Stufe 4 | Stufe 5 |
|------------------|---------|---------|---------|---------|---------|
| < 2             | 0,10    | 0,50    | 0,40    | 0,30    | 0,20    |
| [2, 4)           | 0,20    | 1,00    | 0,80    | 0,60    | 0,40    |
| [4, 5)           | 0,30    | 1,50    | 1,20    | 0,90    | 0,60    |
| [5, 7)           | 0,40    | 2,00    | 1,60    | 1,20    | 0,80    |
| [7, 8)           | 0,50    | 2,50    | 2,00    | 1,50    | 1,00    |
| [8, 10)          | 0,60    | 3,00    | 2,40    | 1,80    | 1,20    |
| [10, 11)         | 0,70    | 3,50    | 2,80    | 2.10    | 1,40    |
| [11, 13)         | 0,80    | 4.00    | 3.20    | 2,40    | 1,60    |
| [13, 14)         | 0,90    | 4,50    | 3,60    | 2,70    | 1,80    |
| ≥ 14             | 1,00    | 5.00    | 4.00    | 3,00    | 2,00    |

Durch Multiplikation mit dem Parameter `VolumeScale` kann dieselbe relative Verteilung auf verschiedene Broker oder Anlageklassen angewendet werden.

## Parameter

| Name | Beschreibung |
|------|-------------|
| **Primärkerze** | Für Eingaben verwendeter Handelszeitraum (Standard 4 Stunden). |
| **Tägliche Kerze** | Kerzen mit höherem Zeitrahmen, die frühere Höchst-/Tiefstwerte liefern (Standard 1 Tag). |
| **AO schnell / AO langsam** | Kurze und lange Perioden des Awesome Oscillator. |
| **Breakout-Puffer** | Puffer in Preisschritten zu früheren Höchst-/Tiefstständen hinzugefügt. |
| **Maximale Stufen** | Maximale Anzahl an Pyramideneinträgen (1–5). |
| **Gewinn erforderlich** | Erlauben Sie zusätzliche Stufen nur, wenn das Eigenkapital den Saldo übersteigt. |
| **Volumenskala** | Globaler Multiplikator, der auf den ausgewählten Losvektor angewendet wird. |
| **Grundguthaben** | Dem kleinsten Losvektor zugewiesener Saldo. |
| **Balance-Schritt** | Balance-Inkrement, das zum nächsten Vektor wechselt. |

## Unterschiede zum MQL4 Expert

* Die StockSharp-Version verwendet integrierte Kerzenabonnements anstelle direkter `iClose`/`iHigh`-Aufrufe und speichert die erforderlichen Preisniveaus intern.
* Der ursprüngliche benutzerdefinierte `zzf`-Indikator wird durch einen leichten Fraktaldetektor emuliert, der Schwankungen von fünf Kerzen bestätigt.
* Stop-Loss- und Take-Profit-Management sind nicht enthalten; Die ursprüngliche Expert-Modifikation stoppt dynamisch, der Algorithmus hing jedoch stark von Broker-spezifischen Funktionen ab. Händler können bei Bedarf ihr eigenes Risikomodul hinzufügen.
* Auf akustische Benachrichtigungen und globale Terminalvariablen wird bewusst verzichtet.

## Nutzungstipps

1. Hängen Sie die Strategie an ein Portfolio an, das sowohl Saldo als auch Eigenkapital meldet, sodass sich die Lot-Matrix genau wie in MetaTrader verhält.
2. Verwenden Sie beim Backtesting realistische 4-Stunden- und Tageshistoriendaten. Gemischte Auflösungen beeinträchtigen die Pyramidenlogik.
3. Experimentieren Sie mit dem Parameter `BreakoutBuffer`, wenn Sie zu Märkten wechseln, die andere Tick-Größen oder Spreads verwenden.
4. Aktivieren Sie das Diagramm beim Debuggen: Die Strategie zeichnet automatisch Kerzen, das Awesome Oscillator-Histogramm und ausgeführte Trades auf.
