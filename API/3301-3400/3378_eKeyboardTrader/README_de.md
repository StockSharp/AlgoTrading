# eKeyboardTrader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert das Verhalten des Expertenberaters MetaTrader „eKeyboardTrader“ unter Verwendung des StockSharp-High-Level-API. Das ursprüngliche Skript wartete auf Tastaturkürzel zum Senden manueller Marktaufträge und zeigte Hilfstexte direkt im Diagramm an. In der StockSharp-Version werden die interaktiven Eingaben als Strategieparameter bereitgestellt, während die Ausführungslogik, Sicherheitsprüfungen und Auftragsschutzfunktionen der MQL-Implementierung treu bleiben.

## Handelslogik
1. **Level1-Abonnement** – Die Strategie abonniert Level1-Marktdaten, um die neuesten besten Geld- und Briefkurse zu erhalten. Diese Anführungszeichen sind erforderlich, bevor eine manuelle Anfrage ausgeführt werden kann, und ahmen die MetaTrader-Abhängigkeit von aktuellen Tick-Daten nach.
2. **Manuelle Befehle** – drei boolesche Parameter (`BuyRequest`, `SellRequest`, `CloseRequest`) repräsentieren die ursprünglichen Tastaturkürzel (B, S und C). Wenn ein Parameter auf `true` gesetzt ist, führt die Strategie die entsprechende Marktaktion aus und setzt das Flag sofort zurück.
3. **Ratenbegrenzung** – eine Abklingzeit von einer Sekunde schützt vor versehentlichen Doppeleinsendungen, identisch mit der in der MQL-Version implementierten Timer-Prüfung. Während der Abklingzeit gestellte Anforderungen warten auf den nächsten Verarbeitungszyklus.
4. **Auftragsschutz** – optionale Stop-Loss- und Take-Profit-Distanzen, ausgedrückt in MetaTrader Punkten, werden mit `Security.PriceStep` in absolute Preise umgerechnet. Wenn mindestens ein Schutzabstand konfiguriert ist, aktiviert die Strategie die integrierte `StartProtection`-Logik von StockSharp, sodass jede manuelle Eingabe automatisch die konfigurierten Schutzbefehle erhält.
5. **Slippage-Erkennung** – Der Parameter `SlippagePoints` wird aus Kompatibilitätsgründen beibehalten und im Protokoll erwähnt, wenn eine manuelle Bestellung gesendet wird, wobei er die vom Fachberater angezeigten Informationskommentare nachahmt.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Basisvolumen für manuelle Marktaufträge. |
| `StopLossPoints` | Abstand vom Einstiegspreis bis zum Schutzstopp in MetaTrader Punkten. Zum Deaktivieren auf `0` setzen. |
| `TakeProfitPoints` | Abstand vom Einstiegspreis zum Schutzziel in MetaTrader Punkten. Zum Deaktivieren auf `0` setzen. |
| `SlippagePoints` | Im Protokoll wird für jede manuelle Bestellung eine informative Schlupftoleranz angezeigt. |
| `BuyRequest` | Auf `true` setzen, um eine Marktkauforder zu senden (wird nach der Verarbeitung automatisch zurückgesetzt). |
| `SellRequest` | Auf `true` setzen, um einen Marktverkaufsauftrag zu senden (wird nach der Verarbeitung automatisch zurückgesetzt). |
| `CloseRequest` | Auf `true` setzen, um die Nettoposition zum Marktpreis zu reduzieren (wird nach der Verarbeitung automatisch zurückgesetzt). |

## Unterschiede zur MQL-Version
- Die Textaufforderungen und akustischen Benachrichtigungen auf der Karte werden nicht reproduziert. Stattdessen dokumentieren Protokollierungsmeldungen die durchgeführten Aktionen.
- Schutzaufträge werden über den `StartProtection`-Helper von StockSharp verwaltet, der Marktaufträge übermittelt, wenn der Schwellenwert erreicht wird, anstatt einzelne MetaTrader-Tickets zu ändern.
- Tastatureingaben werden durch Parameterumschaltungen ersetzt. Jede Benutzeroberfläche, die die Strategie hostet, kann Benutzerinteraktionen (Tastatur, Schaltflächen, Skripte) diesen Parametern zuordnen.
- Die MetaTrader-Handelsanfragediagnosen werden in Protokollierungsanweisungen zusammengefasst, um die Konvertierung möglichst einfach zu gestalten.

## Nutzungshinweise
- Weisen Sie sowohl `Security` als auch `Portfolio` zu, bevor Sie mit der Strategie beginnen. Diese Prüfungen spiegeln die Initialisierungsbedingungen des Fachberaters wider.
- Die manuellen Befehlsflags werden ausgewertet, wenn neue Level1-Daten eintreffen. In einem ruhigen Markt werden Aktionen zum nächsten verfügbaren Kurs ausgeführt.
- Das Anpassen von `StopLossPoints` oder `TakeProfitPoints` während der Ausführung der Strategie erfordert einen Neustart, um das Schutzmodul neu zu konfigurieren und es an die einmal pro Sitzung vorgenommene Schutzeinrichtung des ursprünglichen Skripts anzupassen.
