# Grid-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein einfaches Grid-Trading-System. Es platziert Buy-Stop- und Sell-Stop-Orders in festen Preisabständen, die durch `GridStep` definiert werden. Jede ausgeführte Order verwendet einen festen Take-Profit-Abstand. Ein globales Gewinnziel schließt alle Positionen und setzt das Grid zurück. Optional erhöht sich das Volumen neuer Orders nach einem Martingale-Schema.

## Details

- **Einstiegskriterien:**
  - Buy Stop einen Schritt über dem letzten Preis.
  - Sell Stop einen Schritt unter dem letzten Preis.
- **Long/Short:** Beide.
- **Ausstiegskriterien:**
  - Jede Position schließt beim festen Take-Profit.
  - Wenn der Gesamtgewinn `ProfitTarget` überschreitet, werden alle Orders und Positionen geschlossen.
- **Stops:** Nur Take-Profit.
- **Filter:** Keine.
