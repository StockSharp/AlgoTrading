# Trend-RDS-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Trend RDS ist eine sitzungsbasierte Umkehrstrategie, die ursprünglich für MetaTrader geschrieben wurde. Es sucht nach einer Drei-Balken-Momentum-Formation zu Beginn eines bestimmten Handelsfensters und schwächt die Bewegung ab, indem es in die entgegengesetzte Richtung eintritt. Der StockSharp-Port behält die ursprüngliche Geldverwaltungslogik bei, einschließlich optionaler Umkehrung der Signale, fester Stop-Loss- und Take-Profit-Levels, Break-Even-Schutz und eines Trailing Stops mit einstellbarer Schrittweite.

## Handelslogik
1. **Signalfenster** – Beim konfigurierten `Start Time` prüft die Strategie bis zu 100 kürzlich geschlossene Kerzen.
2. **Mustererkennung** – Es wird nach der ersten Sequenz von drei aufeinanderfolgenden Balken gesucht, bei denen entweder:
   - Die Höchstwerte steigen, während die Tiefstwerte steigen (`High[n] < High[n+1] < High[n+2]` und `Low[n] > Low[n+1] > Low[n+2]`).
   - Die Höchstwerte fallen, während die Tiefstwerte fallen (`High[n] > High[n+1] > High[n+2]` und `Low[n] < Low[n+1] < Low[n+2]`).
Eine symmetrische Ausdehnung in beide Richtungen wird als Konflikt behandelt und ignoriert. Die Signalrichtung wird optional umgekehrt, wenn `Reverse Signals` aktiviert ist.
3. **Einträge** – Die Strategie übermittelt eine Marktorder mit dem konfigurierten `Trade Volume`, wenn keine offene Position vorhanden ist. Ist die Gegenposition noch offen, wird diese zunächst geschlossen.
4. **Erzwungenes Ausstiegsfenster** – Zwischen `Close Time` und fünfzehn Minuten danach wird jede verbleibende Position liquidiert.
5. **Schutz** – Sobald die Position geöffnet ist, registriert die Strategie:
   - Eine Stop-Loss- und Take-Profit-Order bei den gewünschten Pip-Abständen.
   - Ein Break-Even-Trigger, der den Stop nach Erreichen von `Break-Even (pips)` auf den Einstiegspreis verschiebt.
   - Ein Trailing-Stop, der einen Abstand von `Trailing Stop (pips)` vom aktuellen Preis einhält und erst nach einer weiteren Bewegung von `Trailing Step (pips)` vorrückt.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| **Handelsvolumen** | Marktauftragsgröße, ausgedrückt in Lots oder Kontrakten. |
| **Stop-Loss (Pips)** | Abstand zum Schutzanschlag. Zum Deaktivieren auf Null setzen. |
| **Gewinnmitnahme (Pips)** | Abstand zum Gewinnziel. Zum Deaktivieren auf Null setzen. |
| **Startzeit** | Uhrzeit (Börsenzeit), zu der die Mustersuche beginnt. |
| **Schließzeit** | Tageszeit (Börsenzeit), zu der alle offenen Geschäfte innerhalb von 15 Minuten geschlossen werden. |
| **Rückwärtssignale** | Invertiert lange und kurze Einträge. |
| **Trailing Stop (Pips)** | Basis-Nachlaufdistanz. Null deaktiviert das Nachziehen. |
| **Trailing Step (Pips)** | Es ist zusätzliche Bewegung erforderlich, bevor der Trailing Stop erneut aktualisiert wird. |
| **Break-Even (Pips)** | Gewinnschwelle, um den Stop auf den Einstiegspreis zu verschieben. Null deaktiviert die Funktion. |
| **Kerzentyp** | Für die Analyse verwendete Kerzenserie. |

## Praktische Hinweise
- Die Strategie basiert auf dem Preisschritt des Instruments, um Pip-Abstände zu berechnen. Stellen Sie sicher, dass die Sicherheit einen gültigen `PriceStep`- oder `MinPriceStep`-Wert offenlegt.
- Es werden nur fertige Kerzen verarbeitet, sodass das Signal höchstens einmal pro Tag und Zeitrahmen erscheinen kann.
- Stop- und Take-Profit-Orders werden jedes Mal aktualisiert, wenn sich die Positionsgröße ändert, um sicherzustellen, dass bei Teilfüllungen ein gleichbleibender Schutz gewährleistet ist.
- Die Trailing- und Break-Even-Logik wird nur aktiviert, solange eine Position offen ist und ein gültiger Einstiegspreis bekannt ist.

## Dateien
- `CS/TrendRdsStrategy.cs` – StockSharp C#-Implementierung der Strategie.
- `README_zh.md` – Chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.
