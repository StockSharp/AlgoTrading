# VR BUCH Gleitender-Durchschnitt-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **VR BUCH Gleitender-Durchschnitt-Strategie** ist ein direkter Port des MetaTrader Expert Advisors *VR---BUCH*. Sie handelt Trendumkehrungen mit zwei konfigurierbaren gleitenden Durchschnitten und einem Kerzenpreis-Filter. Die StockSharp-Version behält den ursprünglichen Signalfluss bei: Die Strategie schließt offene Positionen, wenn ein entgegengesetztes Setup erscheint, und öffnet erst eine neue Position, nachdem das vorherige Exposure vollständig geschlossen ist.

Die Implementierung stützt sich auf StockSharp's High-Level-Kerzenabonnements, native gleitende Durchschnitts-Indikatoren und Echtzeit-Order-Helfer. Alle Indikatorwerte werden auf fertigen Kerzen verarbeitet, und die Strategie vermeidet manuelle historische Puffer außer einem kleinen Ringpuffer, der die MetaTrader-Verschiebungsparameter reproduziert.

## Handelslogik
1. **Indikatorberechnung**
   - Ein schneller gleitender Durchschnitt und ein langsamer gleitender Durchschnitt werden auf dem ausgewählten Kerzentyp berechnet.
   - Jeder gleitende Durchschnitt kann eine andere Preisquelle und Glättungsmethode (einfach, exponentiell, geglättet, gewichtet) verwenden.
   - Optionale horizontale Verschiebungen reproduzieren den MetaTrader-`ma_shift`-Parameter, indem Werte aus vergangenen Kerzen referenziert werden.
2. **Signalerfassung**
   - Ein *Kauf*-Setup tritt auf, wenn der verschobene schnelle MA über dem verschobenen langsamen MA liegt **und** der ausgewählte Bestätigungspreis über dem schnellen MA liegt.
   - Ein *Verkauf*-Setup tritt auf, wenn der verschobene schnelle MA unter dem verschobenen langsamen MA liegt **und** der Bestätigungspreis unter dem schnellen MA liegt.
3. **Positionsverwaltung**
   - Wenn bereits eine Position offen ist, schließt ein entgegengesetztes Signal zuerst das flache Exposure. Neue Einstiege werden bei nachfolgenden Signalen nur ausgewertet, wenn die Nettoposition auf null zurückkehrt.
   - Wenn keine Position besteht, gibt die Strategie eine Marktorder mit dem konfigurierten Volumen in Richtung des aktiven Signals auf.

Standardmäßig sind keine Stop-Loss- oder Take-Profit-Level enthalten. Benutzer können die Strategie mit StockSharp-Schutzblöcken (`StartProtection`) oder externen Risikomanagern kombinieren, falls erforderlich.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| **Fast Period** | Länge des schnellen gleitenden Durchschnitts. |
| **Fast Shift** | Anzahl der Kerzen, mit denen der schnelle MA-Wert in die Vergangenheit verschoben wird. |
| **Fast Price** | Kerzenkurs-Komponente für den schnellen MA (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet). |
| **Fast Method** | Glättungsmethode für den schnellen MA (einfach, exponentiell, geglättet, gewichtet). |
| **Slow Period** | Länge des langsamen gleitenden Durchschnitts. |
| **Slow Shift** | Anzahl der Kerzen für die Verschiebung des langsamen MA. |
| **Slow Price** | Kerzenkurs-Komponente für den langsamen MA. |
| **Slow Method** | Glättungsmethode für den langsamen MA. |
| **Signal Price** | Kerzenkurs zur Bestätigung des Einstiegs (Standardmäßig Schlusskurs). |
| **Candle Type** | Zeitrahmen oder benutzerdefinierter Kerzentyp für Berechnungen. |
| **Volume** | Ordervolumen für neue Trades. |

## Verwendungshinweise
- Signale werden nur auf fertigen Kerzen ausgewertet, um Intrabar-Rauschen zu vermeiden.
- Die Strategie erwartet, dass der Trading-Connector ausreichend historische Daten liefert, um beide gleitenden Durchschnitte und ihre Verschiebungspuffer aufzuwärmen.
- Der gewichtete Preis verwendet die Formel \((High + Low + 2 * Close) / 4\), was der MetaTrader-`PRICE_WEIGHTED`-Option entspricht.
- Der Klassenname und Namensraum folgen den StockSharp-Projektkonventionen und ermöglichen eine nahtlose Kompilierung innerhalb der `AlgoTrading`-Lösung.

## Ausführungsanleitung
1. Platzieren Sie die Strategie in einem StockSharp-Strategie-Container oder Beispiel-Runner.
2. Konfigurieren Sie das gewünschte Wertpapier, den Zeitrahmen (`Candle Type`) und das Ordervolumen.
3. Passen Sie die Einstellungen des gleitenden Durchschnitts an, um bei Bedarf die ursprüngliche MetaTrader-Vorlage zu entsprechen.
4. Starten Sie die Strategie. Sie wird Kerzen abonnieren, Indikatoren auf Diagrammen zeichnen (wenn verfügbar) und Marktorders basierend auf der beschriebenen Logik platzieren.

Für Portfolio- oder Multi-Symbol-Nutzung duplizieren Sie die Strategie-Instanz pro Instrument und weisen Sie dedizierte Wertpapiere zu.
