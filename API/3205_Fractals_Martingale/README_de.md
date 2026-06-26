# Fractals Martingale-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ordner enthält den StockSharp-Port des MetaTrader Expert Advisors "Fractals Martingale". Die Strategie kombiniert Bill-Williams-Fraktale, einen Ichimoku-basierten Trendfilter und eine monatliche MACD-Bestätigung. Die Positionsgröße folgt einer klassischen Martingal-Sequenz, die das Trade-Volumen nach jedem verlorenen Zyklus multipliziert, während ein optionaler Cool-Down unkontrollierte Exposition verhindert.

## Trading-Logik

1. **Fraktaldetektion auf dem Arbeitszeitrahmen** – abgeschlossene Kerzen werden gepuffert, um lokale Hochs und Tiefs zu erkennen, die durch `FractalDepth` Nachbarn getrennt sind. Ein bullisches Setup wird registriert, wenn die nächste Kerze über dem Fraktalhoch öffnet, während ein bearisches Setup erfordert, dass die nächste Öffnung unter dem Fraktaltief liegt. Erkannte Niveaus bleiben für `FractalLookback` verarbeitete Kerzen gültig.
2. **Ichimoku-Trendfilter** – das Fraktal muss mit dem Ichimoku-Trend übereinstimmen, der auf dem höheren Zeitrahmen berechnet wird, der durch `IchimokuCandleType` definiert ist. Long-Trades erfordern Tenkan-sen über Kijun-sen; Short-Trades erfordern Tenkan-sen unter Kijun-sen.
3. **Monatliche MACD-Bestätigung** – der ursprüngliche EA verwendete einen monatlichen MACD, um zu entscheiden, ob Käufer oder Verkäufer dominieren. Der Port abonniert die `MacdCandleType`-Serie (standardmäßig 30-Tages-Kerzen) und akzeptiert Long-Signale nur, wenn die MACD-Linie über der Signallinie liegt; Short-Signale benötigen die entgegengesetzte Bedingung.
4. **Session-Filter** – Orders werden nur zwischen `StartHour` (einschließlich) und `EndHour` (ausschließlich) platziert. Ein Wrap-Around-Fenster wird für Übernacht-Trading-Sessions unterstützt.
5. **Martingal-Volumenskalierung** – die Basis-Ordergröße kommt von `TradeVolume`. Nach jeder verlierenden Runde wird das nächste Ordervolumen mit `Multiplier` multipliziert und am Instrument-Volumenschritt ausgerichtet. Gewinnende Trades setzen die Sequenz zurück. Wenn `MaxConsecutiveLosses` überschritten wird, pausiert der Algorithmus `PauseMinutes`, bevor er mit dem Basisvolumen fortfährt.
6. **Richtungswechsel** – immer wenn ein neuer Trade gesendet wird, gleicht die Strategie automatisch alle entgegengesetzten Positionen aus, bevor sie Exposition in der angeforderten Richtung eröffnet.

## Risikomanagement

- `StopLossPips` und `TakeProfitPips` werden in absolute Preisabstände umgerechnet, unter Verwendung der erkannten Pip-Größe und über `StartProtection` angewendet. Dies spiegelt den ursprünglichen EA wider, bei dem beide Stops in Pips definiert waren.
- Die ursprüngliche Implementierung enthüllte optionale geldbasierte Trailing-Stops. Der StockSharp-Port stützt sich auf den eingebauten Schutzblock, da die reale Portfolio-Währungsbehandlung broker-spezifisch ist.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `TradeVolume` | Basis-Ordergröße für den ersten Eintrag einer Sequenz. |
| `Multiplier` | Faktor, der nach einem Verlust auf das nächste Trade-Volumen angewendet wird. |
| `StopLossPips`, `TakeProfitPips` | Schutz-Stop- und Zielabstände in Pips gemessen. |
| `FractalDepth` | Anzahl der Kerzen auf jeder Seite, die zum Bestätigen eines Fraktal-Hoch/Tief erforderlich sind. |
| `FractalLookback` | Maximale Anzahl verarbeiteter Kerzen, für die ein erkanntes Fraktal gültig bleibt. |
| `StartHour`, `EndHour` | Trading-Fenster in Börsenstunden ausgedrückt. Wenn beide Werte übereinstimmen, wird der Filter deaktiviert. |
| `MaxConsecutiveLosses` | Anzahl verlierender Trades, bevor die Strategie pausiert. |
| `PauseMinutes` | Dauer des Cool-Down-Zeitraums nach Überschreitung des Verlust-Caps. |
| `TenkanPeriod`, `KijunPeriod`, `SenkouPeriod` | Ichimoku Kinko Hyo-Längen auf dem höheren Zeitrahmen. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | EMA-Längen für die höhere Zeitrahmen-MACD-Bestätigung. |
| `CandleType` | Primäre Kerzenserie, auf der Fraktale und Ausführungen ausgewertet werden. |
| `IchimokuCandleType` | Höherer Zeitrahmen zur Berechnung von Tenkan- und Kijun-Linien. |
| `MacdCandleType` | Zeitrahmen zur Berechnung des MACD-Filters (standardmäßig monatlich). |

## Verwendungshinweise

1. **Pip-Größenberechnung** – der Pip-Wert wird aus `Security.PriceStep` abgeleitet. Fünfstellige Forex-Kurse werden automatisch skaliert, um der MetaTrader-Definition aus dem Quell-EA zu entsprechen.
2. **Indikatorabonnements** – die Strategie verbraucht bis zu drei Kerzenserien. Stellen Sie sicher, dass der Datenfeed alle angeforderten Zeitrahmen liefern kann, um die Filter synchron zu halten.
3. **Martingal-Vorsichtsmaßnahmen** – die Verdoppelung des Volumens erhöht die Exposition schnell. Verwenden Sie die Cool-Down-Parameter oder senken Sie den Multiplikator, wenn das Konto keine längeren Verlustserien aushalten kann.
4. **Unterschiede vs. MT4-EA** – Mail-/Benachrichtigungsalerts, saldoabhängige Trailing-Stops und explizite Margin-Prüfungen wurden entfernt, da StockSharp bereits Konnektivität, Portfolio-Sicherheit und Orderausführung handhabt. Die Kern-Einstieg-/Ausstiegslogik entspricht der MQL-Implementierung.

## Dateien

- `CS/FractalsMartingaleStrategy.cs` – C#-Implementierung mit der High-Level-Strategie-API.
- `README.md` – englische Dokumentation (diese Datei).
- `README_zh.md` – vereinfachte chinesische Übersetzung.
- `README_ru.md` – russische Übersetzung.
