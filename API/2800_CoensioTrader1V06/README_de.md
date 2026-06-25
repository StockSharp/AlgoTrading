# CoensioTrader1 V06 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
CoensioTrader1 V06 ist eine Trendfolge-Ausbruch-Strategie, die ursprünglich als MetaTrader Expert Advisor verteilt wurde. Der StockSharp-Port behält die diskretionäre Mustererkennungslogik bei und entfernt die broker- und internetspezifischen Funktionen aus der MQL-Implementierung. Die Strategie operiert auf einem einzelnen Wertpapier und Zeitrahmen und verwendet Bollinger Bands zusammen mit einem doppelten exponentiellen gleitenden Durchschnitt (DEMA), um Erschöpfungsbewegungen gefolgt von Trendwiederaufnahme zu identifizieren.

Der ursprüngliche Roboter erlaubte den Handel mit bis zu sechs Währungspaaren mit individuellen Parametersätzen, unterstützte DLL-basierte Lizenzierung und meldete Optimierungsergebnisse an einen Remote-Server. Diese Hilfsdienste werden in diesem Port absichtlich weggelassen. Der Fokus liegt auf dem zentralen Einstiegs- und Ausstiegsworkflow, der auf Bollinger-Band-Ablehnungen reagiert, die durch Swing-Struktur und die DEMA-Steigung bestätigt werden.

## Strategielogik
1. **Datenabonnement** – Die Strategie abonniert den konfigurierten Kerzentyp (Standard: 1 Stunde) und bindet Bollinger Bands zusammen mit einem DEMA.
2. **Bollinger-Band-Ablehnung** – Signale werden auf der letzten vollständig geschlossenen Kerze ausgewertet.
   - **Long-Setup**
     - Die Kerze öffnete unterhalb des vorherigen unteren Bollinger Bands und schloss wieder darüber (fehlgeschlagener Ausbruch nach unten).
     - Die Kerze bildete ein höheres Tief im Vergleich zur vorherigen Bar, während diese frühere Bar ein niedrigeres Tief im Vergleich zu ihrem Vorgänger bildete (Doppelboden-Struktur).
     - Der DEMA steigt strikt über die letzten drei Beobachtungen (aktueller Wert > vorheriger > zweiter vorheriger).
   - **Short-Setup**
     - Die Kerze öffnete oberhalb des vorherigen oberen Bollinger Bands und schloss wieder darunter (fehlgeschlagener Ausbruch nach oben).
     - Die Kerze bildete ein niedrigeres Hoch im Vergleich zur vorherigen Bar, während diese frühere Bar ein höheres Hoch im Vergleich zu ihrem Vorgänger bildete (Doppeltop-Struktur).
     - Der DEMA fällt strikt über die letzten drei Beobachtungen.
3. **Orderausführung** – Marktorders werden unmittelbar nach Bestätigung des Signals auf einer fertigen Kerze gesendet. Optionale Positionsglättung bei entgegengesetzten Signalen kann aktiviert werden.
4. **Risikomanagement** – Optionale Stop-Loss- und Take-Profit-Abstände werden über `StartProtection` bereitgestellt. Beide sind absolute Preisabstände; die Trailing-Stop-Funktionalität des ursprünglichen Experts wird nicht reproduziert.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `BollingerPeriod` | Periode für die Bollinger-Band-Berechnung. | 30 |
| `BollingerDeviation` | Standardabweichungsmultiplikator für die Bänder. | 1.5 |
| `DemaPeriod` | Länge des doppelten exponentiellen gleitenden Durchschnitts zur Trendbestätigung. | 20 |
| `StopLossDistance` | Absoluter Stop-Loss-Abstand, der an `StartProtection` übergeben wird. Auf null setzen zum Deaktivieren. | `0 (absolute)` |
| `TakeProfitDistance` | Absoluter Take-Profit-Abstand, der an `StartProtection` übergeben wird. Auf null setzen zum Deaktivieren. | `0 (absolute)` |
| `CloseOnSignal` | Aktuelle Position schließen, bevor eine neue in entgegengesetzter Richtung eröffnet wird. | `false` |
| `CandleType` | Kerzendatentyp oder Zeitrahmen. Standard ist 1-Stunden-Zeitrahmen. | `1h` |

## Verwendungshinweise
- Die StockSharp-Version handelt nur das primäre `Strategy.Security`. Um das Multi-Symbol-Verhalten des ursprünglichen Experts nachzuahmen, starten Sie separate Strategieinstanzen mit unterschiedlichen Parametersätzen.
- Die MQL-Lot-Sizing-Logik (`RiskMax`, `LotSize`, `LotBalanceDivider`) wurde nicht übersetzt. Konfigurieren Sie `Volume` in der Strategie oder über den Risikomanager entsprechend Ihren Portfolioregeln.
- Die DLL-basierte Aktivierung, das Remote-Optimierungsprotokoll und die UI-Zeichenroutinen in den MQL-Dateien wurden absichtlich entfernt.
- Stop-Loss- und Take-Profit-Werte sind absolute Preisabstände. Passen Sie sie an die Tickgröße oder den Pip-Wert des Instruments an, wenn Sie die Strategie konfigurieren.
- Der ursprüngliche Trailing-Stop-Schritt-Mechanismus ist nicht implementiert. Wenn Trailing-Management erforderlich ist, lagern Sie ein dediziertes Risikomodul auf dieser Strategie auf.
- Alle Codekommentare und -logiken werden wie gewünscht auf Englisch gehalten; README-Übersetzungen werden separat bereitgestellt.

## Unterschiede zur MQL-Version
- **Multi-Symbol-Management**: durch ein Einzelinstrument-Design für Klarheit und einfacheres Testen ersetzt.
- **Netzwerk und Lizenzierung**: entfernt; es werden keine externen HTTP-Anfragen oder DLL-Aufrufe durchgeführt.
- **Order-Sizing**: vereinfacht, um auf StockSharps Standard-`Volume`-Handling zu setzen.
- **Visuelle Objekte**: Chart-Anmerkungen aus MetaTrader (Pfeile, Beschriftungen, Farbthemen) werden nicht nachgebaut. Verwenden Sie StockSharp-Chart-Helper, wenn Visualisierung erforderlich ist.
- **Trailing Stop**: nicht portiert; nur die anfänglichen Schutzorders werden konfiguriert.

Diese Dokumentation soll erschöpfend sein, damit der Port überprüft, getestet und erweitert werden kann, ohne den ursprünglichen MQL-Quellcode lesen zu müssen.
