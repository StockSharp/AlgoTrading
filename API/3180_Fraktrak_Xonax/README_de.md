# Fraktrak XonaX Advanced-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine C#-Konvertierung des MetaTrader 5 Expert Advisors **Fraktrak xonax.mq5**. Der ursprüngliche Roboter verfolgt Williams-Fraktale und öffnet Trades, wenn der Preis das jüngste Fraktal-Niveau durchbricht. Die StockSharp-Version behält die gleiche Idee bei und nutzt High-Level-API-Funktionen wie Kerzenabonnements, integrierte Geldverwaltungs-Helper und automatischen Trade-Schutz.

## Handelslogik

1. **Fraktal-Erkennung** – der Algorithmus pflegt ein Fünf-Kerzen-Fenster. Wenn die mittlere Kerze ein höheres Hoch (oder tieferes Tief) als ihre Nachbarn erzeugt, wird der Preis als letztes oberes (oder unteres) Fraktal gespeichert.
2. **Ausbruchssignale** – wenn eine abgeschlossene Kerze das aktuelle Fraktal-Niveau berührt oder überschreitet, bereitet sich die Strategie auf den Handel vor:
   - Oberes Fraktal-Ausbruch → Long-Position eröffnen (oder Short-Position bei aktiviertem *Reverse Mode*).
   - Unteres Fraktal-Ausbruch → Short-Position eröffnen (oder Long-Position bei aktiviertem *Reverse Mode*).
3. **Positionsmanagement** – die konvertierte Strategie reproduziert das MetaTrader-Verhalten:
   - Optionales Schließen der Gegenposition vor dem Eröffnen einer neuen.
   - Initialer Stop-Loss und Take-Profit werden gemäß den konfigurierten Pip-Abständen gesetzt.
   - Ein zweistufiger Trailing-Stop bewegt das Schutzniveau, nachdem der Preis um den angegebenen *Trailing-Schritt* vorrückt.
4. **Geldverwaltung** – wählen zwischen festem Lot oder eigenkapitalbasiertem Risikoprozentsatz. Im Risikomodus schätzt der Algorithmus das Volumen anhand des Portfolio-Eigenkapitals, der Preisschrittgröße und der konfigurierten Stop-Distanz.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `StopLossPips` | Stop-Loss-Abstand in Pips. Auf null setzen, um das Stop-Loss-Niveau zu deaktivieren. |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Null deaktiviert das Ziel. |
| `TrailingStopPips` | Basis-Trailing-Stop-Abstand. Erfordert, dass `TrailingStepPips` größer als null ist. |
| `TrailingStepPips` | Zusätzlicher Abstand, den der Preis zurücklegen muss, bevor der Trailing-Stop vorrückt. |
| `ReverseMode` | Ausbruchsregeln umkehren (obere Fraktale verkaufen, untere Fraktale kaufen). |
| `CloseOpposite` | Wenn wahr, wird jede Gegenposition geschlossen, bevor ein neuer Trade eröffnet wird. |
| `ManagementMode` | Zwischen `FixedLot` oder `RiskPercent`-Geldverwaltung wählen. |
| `ManagementValue` | Vom aktiven Geldverwaltungsmodus verwendeter Wert (Lotgröße oder Prozentsatz). |
| `CandleType` | Kerzenserie für Fraktal-Erkennung und Handelsentscheidungen. |

## Nutzungshinweise

- Die Pip-Größe wird automatisch aus dem Instrument-Preisschritt abgeleitet. Assets mit drei oder fünf Dezimalstellen werden als fraktionale Pip-Instrumente (0.1 Pip) behandelt. Pip-Parameter entsprechend anpassen.
- Die Trailing-Stop-Logik entspricht dem Original-Expert: sowohl die Trailing-Distanz als auch der zusätzliche Schritt müssen positiv sein. Andernfalls wird Trailing übersprungen.
- Die Geldverwaltung im Risikomodus setzt voraus, dass die Preisschritt-Kosten verfügbar sind. Wenn nicht, fällt die Strategie auf eine vereinfachte Berechnung basierend auf dem rohen Preisabstand zurück.
- *Close Opposite* aktivieren, um das Expert-Advisor-Verhalten zu emulieren, bei dem ein neuer Ausbruch den laufenden Trade schließt, bevor in die entgegengesetzte Richtung eingegangen wird.

## Dateien

- `CS/FraktrakXonaxAdvancedStrategy.cs` – Implementierung der Strategie.
- `README.md` – aktuelles Dokument.
- `README_ru.md` – russische Beschreibung.
- `README_zh.md` – chinesische Beschreibung.
