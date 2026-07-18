# 4218 RSI MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Portierung des ursprünglichen MetaTrader Expert Advisors, der sich in `MQL/9925` befindet. Es bildet den Impulsoszillator RSI_MA nach, indem ein klassischer RSI mit der Steigung eines exponentiellen gleitenden Durchschnitts kombiniert wird, der auf dem gewichteten Preis `(High + Low + 2 * Close) / 4` basiert. Signale werden nur für abgeschlossene Kerzen generiert, sodass das Verhalten mit der Quellimplementierung identisch bleibt.

Das Skript ist für tägliche EURUSD-Kerzen (D1-Zeitrahmen) konzipiert und eröffnet jeweils eine einzelne Position. Dennoch kann jedes Instrument mit einem sinnvollen Preissprung verwendet werden, sofern der Kerzentyp entsprechend konfiguriert ist.

## Strategielogik
1. **Indikatorberechnung**
   - Auf Basis der Schlusskurse wird ein Relative-Stärke-Index mit konfigurierbarer Länge berechnet.
   - Aus dem gewichteten Preis wird ein exponentieller gleitender Durchschnitt mit derselben Länge berechnet.
   - Der Indikatorwert beträgt `RSI * (EMA(current) - EMA(previous)) / pipSize` und ist auf den Bereich `[1, 99]` begrenzt.
2. **Langer Eintrag**
   - Vorheriger Indikatorwert unterhalb des überverkauften Extremwerts (Standard 5).
   - Letzter Indikatorwert über dem überverkauften Aktivierungsschwellenwert (Standard 20).
   - Keine offene Position oder bestehende Short-Position (der Short wird geschlossen, bevor eine neue Long-Position eröffnet wird).
3. **Kurzer Eintrag**
   - Vorheriger Indikatorwert über dem überkauften Extremwert (Standard 95).
   - Letzter Indikatorwert unterhalb der überkauften Aktivierungsschwelle (Standard 80).
   - Keine offene Position oder bestehende Long-Position (die Long-Position wird geschlossen, bevor eine neue Short-Position eröffnet wird).
4. **Indikatorbasierter Ausstieg**
   - Long-Positionen werden geschlossen, wenn der Indikator von über dem überkauften Extremwert auf unter das Aktivierungsniveau fällt (standardmäßig 95 → 80).
   - Short-Positionen werden geschlossen, wenn der Indikator von unterhalb des überverkauften Extremwerts auf über das Aktivierungsniveau (standardmäßig 5 → 20) steigt.
5. **Schutzausgänge**
   - Optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Abstände werden in Pips ausgedrückt. Entfernungen werden mithilfe der Sicherheit `PriceStep` (Fallback 0,0001) automatisch in Preise umgerechnet.
   - Die Straffung des Trailing-Stops folgt dem Verhalten des ursprünglichen EA: Sie wird erst aktiviert, wenn sich der Preis um mehr als die konfigurierte Distanz in die günstige Richtung bewegt.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `RsiPeriod` | RSI und EMA Länge.|
| `OversoldActivationLevel` | Schwellenwert, der einen langen Aufbau nach einem überverkauften Extrem bestätigt. |
| `OversoldExtremeLevel` | Extremwert, der erreicht werden muss, bevor Long-Positionen zugelassen werden. |
| `OverboughtActivationLevel` | Schwellenwert, der einen Short-Setup nach einem überkauften Extrem bestätigt. |
| `OverboughtExtremeLevel` | Extremwert, der erreicht werden muss, bevor Shorts erlaubt sind. |
| `StopLossPips` | Abstand für den schützenden Stop-Loss. Aktivieren/deaktivieren über `UseStopLoss`. |
| `TakeProfitPips` | Distanz zum Gewinnziel. Aktivieren/deaktivieren über `UseTakeProfit`. |
| `TrailingStopPips` | Distanz für den Trailing Stop. Aktivieren/deaktivieren über `UseTrailingStop`. |
| `UseStopLoss` | Aktiviert das Stop-Loss-Management. |
| `UseTakeProfit` | Aktiviert das Take-Profit-Management. |
| `UseTrailingStop` | Aktiviert Trailing-Stop-Updates. |
| `UseMoneyManagement` | Aktiviert die Positionsgröße basierend auf `RiskPercent`. |
| `RiskPercent` | Risikoprozentsatz des Portfolios pro Trade, wenn das Geldmanagement aktiv ist. |
| `TradeVolume` | Festes Volumen, das verwendet wird, wenn die Geldverwaltung deaktiviert ist. |
| `CandleType` | Datentyp der von der Strategie verarbeiteten Kerzen (Standard: Täglich). |

## Nutzungshinweise
- Hängen Sie die Strategie an EURUSD-Tageskerzen an, um das Verhalten des ursprünglichen EA zu reproduzieren. Andere Instrumente/Zeitrahmen werden nach Anpassung von `CandleType` und Schwellenwerten unterstützt.
- Es bleibt immer nur eine Position offen. Bei der Eingabe eines neuen Trades wird automatisch zuerst die Gegenrichtung geschlossen.
- Das Geldmanagement greift auf den festen `TradeVolume` zurück, wenn keine Portfolioinformationen verfügbar sind oder das berechnete Volumen nicht mehr positiv ist.
- Stellen Sie sicher, dass das Wertpapier `PriceStep` einen Pip widerspiegelt (0,0001 für die meisten FX-Paare). Ansonsten passen Sie die Parameter entsprechend an.

## Risikomanagement
- Stop-Loss- und Take-Profit-Level werden für jede abgeschlossene Kerze anhand der Hoch-/Tiefstbereiche der Kerze bewertet.
- Der Trailing Stop wird nur aktualisiert, wenn der Trade mehr als die konfigurierte Distanz im Gewinn ist und sich nie in eine ungünstige Richtung bewegt.
- Indikatorbasierte Exits funktionieren auch dann noch, wenn die Risikokontrollen deaktiviert sind, und sorgen so für eine sanfte Herabstufung ähnlich der MQL-Version.
