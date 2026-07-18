# Strategie zur Auflistung von Positionen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Strategie zur Auflistung von Positionen** reproduziert das Verhalten des ursprünglichen MetaTrader-Skripts, indem sie die aktuellen Portfolio-Positionen regelmäßig in das Strategieprotokoll schreibt. Sie ist ein reines Überwachungs-Helper, der niemals Aufträge platziert. Stattdessen erstellt sie einen Snapshot der offenen Positionen, damit der Operator Symbol, Richtung, Größe, Einstiegspreis und aktuellen Gewinn direkt aus dem Designer oder den StockSharp-Protokollen inspizieren kann.

## Hauptmerkmale
- Timer-gesteuertes Positions-Reporting, wobei der erste Snapshot unmittelbar nach dem Start der Strategie geliefert wird.
- Optionale Filterung nach dem Strategie-Wertpapier oder nach Strategie-Identifier (das Analogon der MetaTrader-Magic-Number).
- Detaillierte Log-Ausgabe einschließlich Positions-Identifier, letztem Änderungszeitpunkt, Seite, Menge, Durchschnittspreis und Gewinn.
- Thread-sichere Verarbeitung, die überlappende Timer-Callbacks verhindert, wenn die Umgebung beschäftigt ist.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `StrategyIdFilter` | Zu überspringender Strategie-Identifier. Wenn leer gelassen, werden alle Positionen gemeldet. | Leerer String |
| `SelectionMode` | Steuert, ob Positionen von jedem Symbol oder nur von `Strategy.Security` gemeldet werden. | `AllSymbols` |
| `TimerInterval` | Intervall zwischen aufeinanderfolgenden Positions-Snapshots. | 6 Sekunden |

## Funktionsweise
1. Während `OnStarted` überprüft die Strategie, ob ein Portfolio angehängt ist und das Timer-Intervall positiv ist.
2. Ein `System.Threading.Timer` wird mit null Verzögerung erstellt, sodass der erste Bericht sofort produziert und dann im konfigurierten Intervall wiederholt wird.
3. Jeder Timer-Tick ruft `ProcessPositions` auf, das über `Portfolio.Positions` iteriert, die optionalen Symbol- und Strategie-ID-Filter anwendet und formatierte Zeilen an einen `StringBuilder` anhängt.
4. Wenn mindestens eine Position die Filter passiert, wird die erstellte Tabelle mit `LogInfo` in das Protokoll geschrieben. Wenn nichts übereinstimmt, wird stattdessen eine prägnante Benachrichtigung protokolliert.
5. Timer-Überlappungen werden mit einem Interlocked-Guard verhindert, sodass langsame I/O keine parallelen Ausführungen auslösen kann.

## Verwendungshinweise
- Weisen Sie sowohl `Portfolio` als auch `Connector` zu, bevor Sie die Strategie starten. Wenn `SelectionMode` auf `CurrentSymbol` gesetzt ist, setzen Sie auch `Strategy.Security` auf das zu überwachende Instrument.
- Um den MetaTrader `magic`-Filter zu emulieren, füllen Sie `StrategyIdFilter` mit dem String-Wert, der als `StrategyId` verwendet wird, wenn andere Strategien Orders einreichen. Diese Positionen werden vom Bericht ausgeschlossen.
- Die Strategie ändert nie Positionen oder registriert Orders, was es sicher macht, sie neben Live-Trading-Logik als informationales Widget auszuführen.
- Die Log-Ausgabe wird unter dem Spaltenheader `Idx | Symbol | PositionId | LastChange | Side | Quantity | AvgPrice | PnL` gruppiert, sodass sie bei Bedarf leicht von externen Tools geparst werden kann.

## Unterschiede zur MQL-Version
- MetaTrader verwendet eine vorzeichenlose 64-Bit `magic`-Zahl. StockSharp-Positionen exponieren den Strategie-Identifier als String, daher akzeptiert der Filter Textwerte.
- Anstatt in den Chart-Kommentar zu schreiben, zeichnet dieser Port den Snapshot über `LogInfo` auf, was in Designer, Runner oder jedem Log-Listener sichtbar ist.
- Die StockSharp-Version schützt gegen überlappende Timer-Aufrufe, um unter hoher Last reaktionsfähig zu bleiben.
- Zeitstempel basieren auf `Position.LastChangeTime`, was StockSharp-Positions-Updates widerspiegelt, während das MQL-Skript die Ticket-Erstellungszeit anzeigte.
