# Color XMUV Zeit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader Expert Advisor **Exp_ColorXMUV_Tm** zu StockSharp. Sie recreiert die ursprüngliche Color XMUV geglättete Linie und den Zeitfensterfilter, während die High-Level-Handels-API von StockSharp verwendet wird. Die Strategie folgt der Farbe der geglätteten Linie: Ein Übergang zu Blaugrün (steigend) löst das Long-Management aus, während ein Übergang zu Magenta (fallend) das Short-Management antreibt.

## Kernlogik
- Für jede abgeschlossene Kerze baut die Strategie einen zusammengesetzten Preis ähnlich der MQL-Version auf (`(H + Close)/2` bei bullischen Kerzen, `(L + Close)/2` bei bärischen Kerzen oder `Close` für Doji-Kerzen).
- Der zusammengesetzte Preis wird durch die angeforderte Glättungsmethode geleitet. Gängige Methoden (SMA, EMA, SMMA/RMA, LWMA und Jurik) sind mit StockSharp-Indikatoren implementiert. Exotische Optionen wie T3 oder VIDYA fallen auf eine EMA zurück, da StockSharp keine direkten Äquivalente bietet. Der Phase-Parameter wird für Konfigurationskompatibilität beibehalten, auch wenn der zugrunde liegende Indikator ihn ignoriert.
- Die Color XMUV "Farbe" wird durch Vergleich des neuesten geglätteten Werts mit dem vorherigen rekonstruiert. Steigende Steigungen werden der bullischen Farbe zugeordnet, fallende Steigungen der bärischen Farbe und unveränderte Werte der neutralen Farbe.
- `SignalBar` definiert, wie viele vollständig abgeschlossene Balken beim Auswerten eines Signals zurückgeschaut wird (z.B. bedeutet der Standardwert 1, dass die Logik auf Bestätigung auf dem Balken vor dem jüngsten wartet).
- Ein bullischer Flip (vorherige Farbe nicht bullisch, aktuelle Farbe bullisch) schließt jede Short-Position und öffnet oder ergänzt optional eine Long-Position. Ein bärischer Flip führt die symmetrischen Aktionen für Short-Trades aus.
- Der Zeitfilter imitiert den ursprünglichen EA: Außerhalb des Handelsfensters schließt die Strategie sofort bestehende Positionen und ignoriert neue Einstiege. Der Filter unterstützt Übernacht-Sitzungen (Startzeit nach Endzeit).
- `StopLossPoints` und `TakeProfitPoints` werden unter Verwendung des Preisschritts des Instruments in absolute Abstände umgerechnet und mit `StartProtection` registriert, damit StockSharp Ausstiege serverseitig verwalten kann, wo möglich.

## Risiko- und Positionsverwaltung
- Aufträge werden mit dem Parameter `OrderVolume` dimensioniert. Beim Richtungswechsel addiert die Strategie den Absolutwert der aktuellen Position, sodass die Umkehrung den alten Trade schließt und einen neuen in einer einzigen Transaktion eröffnet.
- Optionaler Stop-Loss und Take-Profit werden von Punktwerten in absolute Preisabstände umgerechnet. Setzen Sie einen der Parameter auf null, um die jeweilige Schutzschicht zu deaktivieren.
- Positionsausstiege, die durch den Farbflip ausgelöst werden, respektieren die Schalter `EnableBuyExits` und `EnableSellExits`, was eine unabhängige Steuerung des Long- und Short-Managements ermöglicht.

## Parameter
- **Candle Type** – Für Berechnungen verwendete Kerzenserie (Standard 4-Stunden-Kerzen).
- **Order Volume** – Basisgröße der Marktorder.
- **Enable Long Entries / Enable Short Entries** – Öffnung von Positionen bei bullischen/bärischen Flips erlauben.
- **Close Longs / Close Shorts** – Automatische Ausstiege bei entgegengesetzten Farbübergängen aktivieren.
- **Use Time Filter** – Handel auf die konfigurierte Sitzung beschränken.
- **Start Hour / Start Minute / End Hour / End Minute** – Handelssitzungsgrenzen. Wenn der Start nach dem Ende liegt, reicht die Sitzung über Mitternacht.
- **Smoothing Method** – Gleitender-Durchschnitt-Algorithmus für die Color XMUV-Linie. Optionen ohne native StockSharp-Implementierung werden durch EMA ersetzt und sind oben dokumentiert.
- **Length** – Glättungslänge (muss positiv sein).
- **Phase** – Hilfsphaseparameter für Konfigurationskompatibilität beibehalten.
- **Signal Bar** – Anzahl abgeschlossener Balken zum Verzögern der Signalprüfung. Auf null setzen, um auf dem jüngsten geschlossenen Balken zu agieren.
- **Stop Loss (pts) / Take Profit (pts)** – Offsets in Preispunkten ausgedrückt; null deaktiviert die jeweilige Schicht.

## Hinweise
- Der MQL-Experte stützte sich auf externe Glättungsbibliotheken. Wenn solche Glättungsmodi in StockSharp nicht verfügbar sind (ParMA, VIDYA, T3) substituiert die Implementierung eine EMA. Dokumentieren Sie diese Fallbacks, wenn Sie die Strategie mit Benutzern teilen.
- Die Strategie speichert nur den minimalen Farbverlauf, der von `SignalBar` benötigt wird, und hält sich damit an die Repository-Richtlinie, die vom Aufbau benutzerdefinierter Datencaches abrät.
