# Alexav SpeedUp M1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Alexav SpeedUp M1-Strategie** ist eine direkte Portierung des MetaTrader 4-Expertenberaters „Alexav_SpeedUp_M1“. Es wertet die Körper abgeschlossener Intraday-Kerzen aus und reagiert sofort mit Marktaufträgen, wenn der Kerzenkörper einen konfigurierbaren Schwellenwert überschreitet. Nach einem Einstieg emuliert die Strategie ein Risikomanagement im MetaTrader-Stil, indem sie der offenen Position Stop-Loss-, Take-Profit- und Trailing-Stop-Orders hinzufügt.

Die Konvertierung basiert auf der hohen Ebene StockSharp API. Kerzen werden über `SubscribeCandles` verbraucht, Preisinformationen für das Trailing werden aus Level-1-Daten empfangen und Schutzaufträge werden über die Standardhelfer `BuyStop`, `SellStop`, `BuyLimit` und `SellLimit` aufgegeben. Es sind keine manuellen Indikatorberechnungen erforderlich.

## Signalerzeugung
1. Jede fertige Kerze im konfigurierten Zeitrahmen wird überprüft.
2. Wenn die Kerze um mehr als den **Körperschwellenwert** über ihrem Eröffnungskurs schließt, eröffnet die Strategie eine Long-Position am Markt (oder kehrt sich in diese um).
3. Wenn die Kerze um mehr als denselben Schwellenwert unter ihrem Eröffnungskurs schließt, eröffnet die Strategie eine Short-Position am Markt (oder kehrt sich in diese um).
4. Bestehendes Engagement in die entgegengesetzte Richtung wird automatisch durch Erhöhung des Market-Order-Volumens geschlossen, wodurch das Verhalten des ursprünglichen Expertenberaters originalgetreu reproduziert wird.

## Auftragsverwaltung
* **Anfänglicher Stop-Loss**: Sobald das Positionsvolumen steigt, wird eine schützende Stop-Order zum Einstiegspreis minus (für Longs) bzw. plus (für Shorts) der konfigurierten Anzahl von Punkten registriert.
* **Take-Profit**: Eine passende Limit-Order wird in der Richtung des Handels in dem durch **Take-Profit (Punkte)** angegebenen Abstand platziert.
* **Trailing Stop**: Bid/Ask-Aktualisierungen der Stufe 1 überwachen den aktuellen Gewinn. Wenn der nicht realisierte Gewinn die Trailing-Distanz überschreitet, wird der Schutzstopp in Richtung des Preises verschoben, wobei die konfigurierte Lücke beibehalten wird, ohne jedoch einen Rückschritt zu machen.
* Alle Schutzaufträge werden storniert, wenn die Position wieder flach wird.

Die Konvertierung hält die Logik bewusst einfach: Es werden keine zusätzlichen Filter, Indikatoren oder Risikokontrollen hinzugefügt, die über das hinausgehen, was in der MQL-Implementierung vorhanden war.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| **Losgröße** | Basishandelsvolumen (in Lots), das für jede Marktorder verwendet wird. Beim Umkehren einer bestehenden Position wird das benötigte Volumen automatisch hinzugefügt. |
| **Take Profit (Punkte)** | Abstand vom Einstiegspreis zum Take-Profit-Level, gemessen in MetaTrader Punkten (umgerechnet anhand der Wertpapierpreisstufe). |
| **Erster Stopp (Punkte)** | Abstand vom Einstiegspreis bis zum anfänglichen Schutzstopp, ausgedrückt in Punkten. |
| **Trailing Stop (Punkte)** | Der Nachlaufabstand wird beibehalten, nachdem sich der Preis zugunsten der Position bewegt hat. Ein Wert von Null deaktiviert die nachgestellte Logik. |
| **Körperschwelle** | Minimale absolute Differenz zwischen Kerzenschluss und -eröffnung, die erforderlich ist, um einen neuen Handel auszulösen. |
| **Kerzentyp** | Zur Signalauswertung verwendete Kerzenserie (Zeitrahmen). Der Standardwert entspricht dem ursprünglichen Ein-Minuten-Diagramm. |

## Nutzungshinweise
* Stellen Sie sicher, dass die Sicherheit einen gültigen `PriceStep` bereitstellt. Wenn sie nicht verfügbar ist, greift die Strategie darauf zurück, Punktabstände als Rohpreis-Offsets zu interpretieren.
* Die Trailing-Stop-Logik erfordert Level-1-Daten (bester Geld-/Briefkurs). Wenn nur Kerzendaten verfügbar sind, bleibt die Trailing-Funktionalität inaktiv.
* Die Strategie ist für den Intraday-Betrieb konzipiert und spiegelt das Ein-Trade-pro-Kerze-Verhalten wider, das der ursprüngliche MQL-Experte über seine internen Zähler durchgesetzt hat.
