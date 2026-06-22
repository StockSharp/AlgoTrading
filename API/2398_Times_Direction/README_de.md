# Zeitbasierte Richtungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese zeitbasierte Strategie eröffnet eine einzelne Long- oder Short-Position während eines vordefinierten Fensters und schließt sie während eines anderen Fensters. Die Einstiegsrichtung ist konfigurierbar und das System überwacht optionale Stop-Loss- und Take-Profit-Niveaus. Der Ansatz basiert ausschließlich auf abgeschlossenen Kerzen ohne Verwendung von Indikatoren.

## Details

- **Einstiegskriterien**:
  - Wenn die aktuelle Kerzenzeit innerhalb von `[OpenTime, OpenTime + TradeInterval)` liegt und keine Position offen ist, in der konfigurierten Richtung einsteigen.
- **Ausstiegskriterien**:
  - Die Position schließen, wenn die Zeit innerhalb von `[CloseTime, CloseTime + TradeInterval)` liegt.
  - Zusätzlich aussteigen, wenn Stop-Loss- oder Take-Profit-Niveaus erreicht werden.
- **Long/Short**: Konfigurierbar.
- **Stops**: Stop-Loss und Take-Profit in Preiseinheiten relativ zum Einstiegspreis.
- **Standardwerte**:
  - `Trade` = Sell.
  - `OpenTime` = 1970-01-01 00:00.
  - `CloseTime` = 3000-01-01 00:00.
  - `TradeInterval` = 1 Minute.
  - `StopLoss` = 1000.
  - `TakeProfit` = 2000.
  - `Volume` = 0.1.
- **Filter**:
  - Kategorie: Zeitbasiert
  - Richtung: Einzeln
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
