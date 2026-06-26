# Exp X2MA Candle MM Recovery-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Konvertierung des MetaTrader-Experts **Exp_X2MACandle_MMRec**. Sie beobachtet die Farbe einer doppelt geglätteten Kerze, die vom originalen X2MA-Indikator erzeugt wird, um zu entscheiden, wann Positionen geöffnet oder geschlossen werden. Die StockSharp-Version recreiert die duale Glättungspipeline und hält eine leichtgewichtige Geldmanagement-Schicht, die das Handelsvolumen nach einer konfigurierbaren Anzahl von kürzlichen Verlusten reduziert.

Der Algorithmus verarbeitet nur abgeschlossene Kerzen. Er abonniert einen konfigurierbaren Zeitrahmen, wendet zwei verkettete gleitende Durchschnitte auf die OHLC-Werte der Kerze an, leitet eine synthetische Kerzenfarbe (grün, grau oder rot) ab und verwendet Farbübergänge mit einer benutzerwählbaren Balkenverschiesung, um Aktionen auszulösen. Long-Trades werden geöffnet, wenn sich die Farbe von bullisch zu etwas anderem ändert. Short-Trades folgen der symmetrischen Bedingung. Positionsausgänge sind mit denselben Farbprüfungen ausgerichtet und können für jede Seite separat aktiviert oder deaktiviert werden.

## Indikatorlogik
1. Jede Kerze wird zweimal geglättet. Beide Stufen können unterschiedliche Methoden und Längen verwenden.
2. Glättungsoptionen werden StockSharp-Indikatoren zugeordnet:
   - `Simple` → `SimpleMovingAverage`
   - `Exponential` → `ExponentialMovingAverage`
   - `Smoothed` → `SmoothedMovingAverage` (RMA)
   - `Weighted` → `WeightedMovingAverage`
   - `Jurik` → `JurikMovingAverage` (der Phase-Parameter wird berücksichtigt, wenn verfügbar).
3. Der synthetische Kerzenkörper wird abgeflacht, wenn der absolute Öffnungs-/Schluss-Unterschied unter `GapPoints * Security.StepPrice` liegt.
4. Farben werden wie folgt zugewiesen: Öffnung < Schluss → `2` (bullisch), Öffnung > Schluss → `0` (bärisch), ansonsten → `1` (neutral).
5. Signale werden auf Balken `SignalBar + 1` ausgewertet (zwei Balken zurück mit der Standardeinstellung), sodass Orders nur nach Bestätigung eines vollständigen Kerzenfarbwechsels übermittelt werden.

## Geldmanagement
- Der ursprüngliche Expert reduzierte die Positionsgröße dynamisch nach einer Verlustserie unter Verwendung historischer Deal-Statistiken. StockSharp stellt die genaue MetaTrader-Historie nicht bereit, daher hält die Portierung eine interne Warteschlange aktueller geschlossener Trades.
- Die Warteschlangenlänge wird durch `HistoryDepth` kontrolliert und das Volumen fällt auf `ReducedVolume`, sobald `LossTrigger` oder mehr Verluste innerhalb des Fensters erkannt werden.
- Die Strategie erfasst Trade-Ergebnisse anhand von Kerzenschlusskursen, wenn ein manueller Ausstieg ausgelöst wird. Stop-Loss/Take-Profit-Orders aus der MetaTrader-Version werden nicht recreiert. Sie können bei Bedarf eigene Schutzregeln über StockSharp-Risikomanager hinzufügen.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `CandleType` | Zeitrahmen der Kerzen für Glättung und Handel. |
| `FirstMethod`, `FirstLength`, `FirstPhase` | Primäre Glättungsmethode, Länge und Jurik-Phase. |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Sekundäre Glättungsmethode, Länge und Jurik-Phase. |
| `GapPoints` | Kerzenkörper-Abflachungsschwelle in Kursschritten. |
| `SignalBar` | Verschiebung (0 = letzte abgeschlossene Kerze) beim Lesen der Farbpuffer. |
| `AllowLongEntry` / `AllowShortEntry` | Öffnen von Long- oder Short-Positionen aktivieren. |
| `AllowLongExit` / `AllowShortExit` | Schließen von Long- oder Short-Positionen aktivieren. |
| `NormalVolume` | Standard-Ordergröße (Lots, Aktien, Kontrakte). |
| `ReducedVolume` | Ordergröße nach der konfigurierten Anzahl von Verlusten. |
| `HistoryDepth` | Anzahl der für Verluste inspizierten jüngsten Trades (0 deaktiviert die Historienverfolgung). |
| `LossTrigger` | Verlustanzahl, die das reduzierte Volumen aktiviert (0 deaktiviert den Schalter). |

## Verwendungshinweise
- Die Strategie operiert auf einem einzelnen Wertpapier, das von `GetWorkingSecurities()` zurückgegeben wird.
- Signale und Ausstiege werden einmal pro abgeschlossener Kerze verarbeitet, um doppelte Orders zu vermeiden.
- `ReducedVolume` gleich `NormalVolume` setzen, wenn die Volumenreduzierung deaktiviert werden soll, während die Historienstatistiken beibehalten werden.
- Da die Portierung auf Kerzenschlusskursen basiert, um Trades zu klassifizieren, kann der Verlustzähler leicht von MetaTrader abweichen, wenn Slippage oder Teilausführungen auftreten. Die Dokumentation sollte helfen, Parameter anzupassen, um ähnliches Verhalten zu erzielen.
- Stops und Take-Profits aus der MQL-Version werden nicht automatisch recreiert. StockSharp-Risikomanager (`StartProtection`) verwenden, wenn Plattform-Level-Schutz benötigt wird.
