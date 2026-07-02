# Alliheik Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertierung des MetaTrader 4 Expert Advisors **alliheik.mq4**. Die Strategie kombiniert einen doppelt geglätteten Heiken-Ashi-Kerzenkörper mit dem nach vorne verschobenen Alligator-„Kiefer“-gleitenden Durchschnitt. Einträge treten auf, wenn sich die Heiken-Ashi-Puffer nach dem Glättungsprozess kreuzen. Exits basieren auf einem Jaw-Crossover-Filter, optionalen festen Zielen und einem preisbasierten Trailing Stop.

## Handelslogik

- **Heiken Ashi-Konstruktion**
  - Glatte rohe Eröffnungs-, Höchst-, Tiefst- und Schlusskurse mit `PreSmoothMethod` / `PreSmoothPeriod`.
  - Bauen Sie klassische Heiken Ashi-Kerzen zu den geglätteten Preisen.
  - Tauschen Sie die Hoch-/Tiefpuffer je nach Kerzenfarbe aus (bullisch hält die niedrige/hohe Reihenfolge aufrecht, bärisch kehrt sie um).
  - Wenden Sie einen zweiten Glättungsdurchlauf (`PostSmoothMethod` / `PostSmoothPeriod`) auf die bedingten Puffer an. Dies sind die Werte, die in den Signalregeln verglichen werden.
- **Signaldefinition**
  - **Long**: Der aktuelle untere Puffer liegt unter dem oberen Puffer, während der vorherige Balken das entgegengesetzte oder gleiche Verhältnis hatte.
  - **Short**: Der aktuelle untere Puffer liegt über dem oberen Puffer, während der vorherige Balken das entgegengesetzte oder gleiche Verhältnis hatte.
- **Jaw-Filter und Trailing**
  - Der Alligator-Kiefer ist ein gleitender Durchschnitt von `JawsPeriod` Balken, der um `JawsShift` Balken nach vorne verschoben und mit `JawsPrice` gespeist wird.
  - `Close[6]` (vor sechs Takten) muss den Kiefer kreuzen, bevor die Position automatisch geschlossen werden kann.
  - Sobald die Differenz zwischen `Close[6]` und dem Kiefer acht Punkte erreicht und sich durch den Kiefer umkehrt, ist die Position geschlossen.
  - Wenn `TrailingStopPoints` größer als Null ist, folgt der Stop-Preis `Close[6]`, sobald sich diese Kerze auf der profitablen Seite des Kiefers befindet.
- **Stopps und Ziele**
  - `StopLossPoints` und `TakeProfitPoints` sind optionale feste Entfernungen, die bei der Einreise angewendet werden.
  - Die Trailing-Logik überschreibt den Schutzstopp, sobald er sich zugunsten des Handels bewegt.

## Standardparameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Für alle Berechnungen verwendeter Zeitrahmen. |
| `JawsPeriod` | 144 | Länge des Alligator Kiefer-gleitenden Durchschnitts. |
| `JawsShift` | 8 | Vorverlagerung des Kiefers (Anzahl der Balken). |
| `JawsMethod` | Einfach | Typ des gleitenden Durchschnitts für den Kiefer (einfach, exponentiell, geglättet, gewichtet). |
| `JawsPrice` | Schließen | Preiskomponente, die dem Kiefer zugeführt wird (Schluss/Offen/Hoch/Niedrig/Median/Typisch/Gewichtet). |
| `PreSmoothMethod` | Exponentiell | Der gleitende Durchschnitt wird zur Glättung der OHLC-Rohwerte vor der Berechnung von Heiken Ashi verwendet. |
| `PreSmoothPeriod` | 21 | Zeitraum der Vorglättungsdurchschnitte. |
| `PostSmoothMethod` | Gewichtet | Auf die bedingten Heiken Ashi-Puffer angewendeter gleitender Durchschnitt. |
| `PostSmoothPeriod` | 1 | Zeitraum der Nachglättungsmittelwerte (1 behält die ursprünglichen Puffer bei). |
| `StopLossPoints` | 0 | Fester Stoppabstand in Punkten (0 deaktiviert). |
| `TrailingStopPoints` | 0 | Trailing-Stop-Distanz basierend auf `Close[6]` (0 deaktiviert). |
| `TakeProfitPoints` | 225 | Take-Profit-Distanz in Punkten (0 deaktiviert). |
| `OrderVolume` | 0,1 | Losgröße für Einträge. |

## Verwendete Indikatoren

- Vorglättende MAs (vier parallele Serien für Eröffnung, Hoch, Tief, Schluss).
- Der Wiederaufbau von Heiken Ashi wurde durch die geglätteten Preise vorangetrieben.
- Nachglättende MAs der bedingten Puffer, die das Eintrittssignal bilden.
- Alligator beweglicher Kieferdurchschnitt mit einstellbarem Typ, Verschiebung und angewendetem Preis.

## Ein- und Ausstiegsübersicht

- **Geben Sie Long ein**, wenn der geglättete untere Puffer den oberen Puffer unterschreitet und der vorherige Balken nicht bullisch war (Kreuzungsbedingung oben beschrieben).
- **Long beenden**, wenn:
  - `Close[6]` fällt wieder unter den Kiefer, nachdem er sich zuvor darüber befunden hatte und der Abstand ≥ 8 Punkte erreicht hat; oder
  - `TakeProfitPoints` Ziel ist erreicht; oder
  - Die Haltestelle `StopLossPoints`/`TrailingStopPoints` wird erreicht.
- **Geben Sie Short ein**, wenn der geglättete untere Puffer den oberen Puffer überschreitet und der vorherige Balken nicht bärisch war.
- **Exit Short** wenn:
  - `Close[6]` steigt wieder über den Kiefer, nachdem er sich zuvor darunter befand und der Abstand ≥ 8 Punkte erreicht hat; oder
  - `TakeProfitPoints` Ziel ist erreicht; oder
  - Die Haltestelle `StopLossPoints`/`TrailingStopPoints` wird erreicht.

## Konvertierungshinweise

- Die Strategie erzwingt einen Trade pro Balken und spiegelt die `isOrderAllowed()`-Prüfung im ursprünglichen EA wider.
- Schutzstopps und -ziele werden intern simuliert, da StockSharp-Strategien nicht auf Broker-seitige MT4-Aufträge zurückgreifen können.
- Der bewegliche Kieferdurchschnitt speichert historische Werte, sodass die Vorwärtsverschiebung das `iMA`-Verhalten mit `ma_shift = JawsShift` reproduziert.
- Bei allen Berechnungen werden Dezimalarithmetik und Indikatorbindungen verwendet, die den StockSharp-Anforderungen auf hoher Ebene API entsprechen.

## Risiko und Nutzung

- Entwickelt für den Long- und Short-Handel mit demselben Instrument.
- Funktioniert am besten auf Trendmärkten, in denen die Kieferverschiebung und die Glättung durch Heiken Ashi mittelfristige Schwankungen hervorheben können.
- Erwägen Sie eine Anpassung von `TrailingStopPoints` und `TakeProfitPoints`, um sie an die Volatilität des Instruments anzupassen.
- Führen Sie vor der Live-Bereitstellung immer Backtests und Forward-Tests für Papierkonten durch.
