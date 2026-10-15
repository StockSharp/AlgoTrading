# XFatl XSatl Cloud Gegentrend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie recreiert den MT5-Experten **Exp_XFatlXSatlCloud**. Sie beobachtet die geglättete FATL/SATL-"Wolke" und handelt **gegen** die Richtung ihrer Kreuzung. Wenn die schnelle Linie (XFATL) nach dem Überschreiten der langsamen Linie (XSATL) zurückfällt, öffnet die Strategie eine Long-Position. Wenn die schnelle Linie nach dem Unterschreiten wieder steigt, öffnet sie eine Short-Position. Optionale Stop-Loss- und Take-Profit-Level werden in Instrumentenpreisschritten ausgedrückt.

## Handelsstrategie

- Die Standarddatenquelle ist ein 8-Stunden-Zeitrahmen. Andere Kerzentypen können mit dem `CandleType`-Parameter ausgewählt werden.
- Zwei Glättungspipelines werden aus StockSharp-gleitenden Durchschnitten aufgebaut. Standardmäßig verwenden beide einen Jurik-gleitenden Durchschnitt mit konfigurierbarer Länge und Phase. Alternative Glättungsfamilien (SMA, EMA, SMMA, WMA) sind ebenfalls verfügbar.
- Signale werden an der durch `SignalBar` definierten Bar ausgewertet (Versatz in Bars von der letzten geschlossenen Kerze). Die Strategie speichert ein rollendes Fenster aktueller Indikatorwerte, sodass die letzten und vorherigen Werte wie in der MT5-Version verglichen werden können.
- Einstiegsregeln (konträr):
  - **Long** – die schnelle Linie war auf der vorherigen Bar über der langsamen Linie und hat jetzt auf oder unter sie gekreuzt.
  - **Short** – die schnelle Linie war auf der vorherigen Bar darunter und hat jetzt auf oder darüber gekreuzt.
- Ausstiegsregeln:
  - Long-Positionen schließen, wenn die vorherige Bar eine bärische Wolke zeigte (schnell unter langsam) und `AllowLongExit` aktiviert ist.
  - Short-Positionen schließen, wenn die vorherige Bar eine bullische Wolke zeigte (schnell über langsam) und `AllowShortExit` aktiviert ist.
- Eine neue Position wird erst geöffnet, nachdem die vorherige vollständig geschlossen wurde, was das Verhalten des ursprünglichen Expertenberaters widerspiegelt.

## Risikomanagement

- `TradeVolume` kontrolliert die für Marktorders verwendete Menge. Die Strategie skaliert nie hinein – jede neue Position verwendet dieselbe Größe.
- `TakeProfitTicks` und `StopLossTicks` werden direkt in Preisschrittdistanzen umgerechnet und in das integrierte Schutzmodul von StockSharp eingespeist. Setzen Sie sie auf null, um die entsprechende Schutzorder zu deaktivieren.
- Da der MT5-Experte auf broker-spezifischen Money-Management-Berechnungen basierte, ersetzt diese Version diese Logik durch explizite Volumen- und Schutzparameter.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Kerzentyp oder Zeitrahmen für Indikatorberechnungen. |
| `FastMethod` / `SlowMethod` | Glättungsfamilie für XFATL und XSATL (Jurik standardmäßig). |
| `FastLength` / `SlowLength` | Periodenlängen für die schnellen und langsamen Filter. |
| `FastPhase` / `SlowPhase` | Phaseneingaben, die bei Unterstützung an den Jurik-gleitenden Durchschnitt weitergeleitet werden. |
| `SignalBar` | Bar-Versatz bei der Kreuzungsbewertung (1 = vorherige Bar). |
| `TradeVolume` | Ordergröße für Einstiege. |
| `AllowLongEntry` / `AllowShortEntry` | Konträre Einstiege in jede Richtung aktivieren oder deaktivieren. |
| `AllowLongExit` / `AllowShortExit` | Erlauben, dass der Indikator offene Positionen bei entgegengesetzten Signalen schließt. |
| `TakeProfitTicks` | Abstand zum Take-Profit-Ziel in Preisschritten. |
| `StopLossTicks` | Abstand zum Schutz-Stop in Preisschritten. |

## Implementierungshinweise

- Die Strategie hält kurze Warteschlangen aktueller Indikatorausgaben und trimmt sie auf die von `SignalBar` benötigte Mindestlänge. Es werden keine zusätzlichen historischen Puffer erstellt.
- Die Jurik-Phasenunterstützung wird über Reflection konfiguriert, damit die Strategie mit verschiedenen StockSharp-Versionen kompatibel bleibt. Wenn dem zugrunde liegenden Indikator eine `Phase`-Eigenschaft fehlt, wird der Wert einfach ignoriert.
- Nur der Schlusskurs jeder Kerze wird verwendet, was der gebräuchlichsten Einstellung für den ursprünglichen Experten entspricht. Die Logik auf alternative Preistypen zu erweitern würde eine Erweiterung der Strategie erfordern.
- High-Level-API-Komponenten (`SubscribeCandles`, `Bind`, `StartProtection`) werden durchgängig verwendet, sodass die Strategie sauber mit Designer und anderen StockSharp-Produkten integriert.
