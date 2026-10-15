# Exp SSL NRTR Tm Plus Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie repliziert den MetaTrader-Expertenberater "Exp_SSL_NRTR_Tm_Plus" unter Verwendung der StockSharp-High-Level-Infrastruktur. Sie
abonniert einen einzigen Zeitrahmen, berechnet den SSL NRTR-Kanal mit einer konfigurierbaren Glättungsmethode und reagiert auf die Farb-
wechsel des Indikators. Long-Einstiege werden ausgelöst, wenn der Kanal bullisch wird, während Short-Einstiege bei
bärischen Übergängen auftreten. Die Implementierung bewahrt die ursprünglichen Risikokontrollen, optionale Handelsfilter und den timer-basierten Ausstieg.

## Parameter

| Gruppe | Parameter | Beschreibung |
| --- | --- | --- |
| Trading | Money Management | Anteil des Portfolios (oder direkte Lots bei negativem Wert/`Lot`-Modus) zur Ordergrößenberechnung. |
| Trading | Margin Mode | Modus zur Umrechnung des Money-Management-Werts in eine Positionsgröße. Modi außer `Lot` werden mit portfoliobasierten Berechnungen approximiert. |
| Trading | Allow Long/Short Entries | Eröffnung von Positionen in der jeweiligen Richtung aktivieren oder deaktivieren. |
| Trading | Allow Long/Short Exits | Der Strategie erlauben, Positionen bei Indikatorumkehrungen in der jeweiligen Richtung zu schließen. |
| Risk | Stop Loss | Schutz-Stop-Abstand in Preisschritten. Die Strategie überwacht die Niveaus, anstatt native Stop-Orders zu platzieren. |
| Risk | Take Profit | Take-Profit-Abstand in Preisschritten. |
| Risk | Slippage | Informationsparameter aus dem Original-EA. |
| Risk | Use Time Exit | Den Timer aktivieren, der nach dem konfigurierten Haltezeitraum eine Flat-Position erzwingt. |
| Risk | Exit Minutes | Haltezeitraum in Minuten für den zeitbasierten Ausstieg. |
| Data | Candle Type | Arbeits-Zeitrahmen für Handel und Indikatorberechnungen. |
| Indicator | Smoothing Method | Gleitender Durchschnittstyp für den SSL NRTR-Kanal. Nicht unterstützte benutzerdefinierte Typen fallen auf EMA zurück. |
| Indicator | Length | Basisperiode des Glättungsalgorithmus. |
| Indicator | Phase | Hilfsparameter für adaptive Durchschnitte (T3, VIDYA, AMA). |
| Indicator | Signal Bar | Anzahl geschlossener Bars zum Zurückschauen bei der Auswertung von SSL-Farben. |

## Handelslogik

1. Den konfigurierten Zeitrahmen abonnieren und nur abgeschlossene Kerzen verarbeiten.
2. Die SSL NRTR gleitenden Durchschnitte berechnen und die Kanalfarbe ableiten (auf, ab oder neutral).
3. Wenn die Farbe auf bullisch wechselt (`0`), Short-Positionen optional schließen und, wenn aktiviert, eine Long-Position eröffnen.
4. Wenn die Farbe auf bärisch wechselt (`2`), Long-Positionen optional schließen und, wenn aktiviert, eine Short-Position eröffnen.
5. Stop-Loss/Take-Profit-Niveaus mit dem Einstiegspreis verfolgen und die Position schließen, wenn ein Niveau erreicht wird.
6. Positionen optional schließen, sobald die Haltezeit den `Exit Minutes`-Parameter überschreitet.
7. Wiederholte Einstiege innerhalb derselben Bar durch Drosselung mit der ursprünglichen MT5-"Zeitlevel"-Logik verhindern.

## Money Management

- Der `Lot`-Modus behandelt den Money-Management-Wert als direktes Volumen in Lots/Kontrakten.
- `FreeMargin` und `Balance` approximieren den angeforderten Kapitalanteil durch Division durch den letzten Schlusskurs.
- `LossFreeMargin` und `LossBalance` schätzen das handelbare Volumen aus dem erlaubten Verlust pro Trade mit dem konfigurierten Stop-Loss-Abstand.
- Negative Money-Management-Werte werden immer einer absoluten Lotgröße zugeordnet.

## Hinweise

- Nur die in StockSharp verfügbaren Glättungsmethoden werden direkt implementiert. `Jurx` und `Parma` fallen auf den exponentiellen gleitenden Durchschnitt zurück, und dieses Verhalten ist in Code-Kommentaren dokumentiert.
- Die Strategie hält Stop-Loss- und Take-Profit-Logik innerhalb der Strategieschleife, anstatt native Schutzaufträge zu senden, um plattformunabhängig zu bleiben.
- Slippage ist eine informationsbasierte Einstellung für Vollständigkeit; Aufträge werden als einfache Marktaufträge gesendet.
- Die Implementierung zeichnet standardmäßig Kerzen und eigene Trades im Chartbereich.
