# ADX-Stop-Order-Vorlage-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie demonstriert, wie ausstehende Stop-Orders mit dem Average Directional Index (ADX) und seinen Directional Movement-Komponenten platziert werden. Sie repliziert die Kernlogik einer klassischen MQL-Vorlage: Wenn der Markt einen starken Trend zeigt und die +DI- und -DI-Linien kreuzen, platziert das System eine Kauf-Stop- oder Verkauf-Stop-Order in einem festen Abstand. Schutzende Stop-Loss- und Take-Profit-Niveaus werden automatisch verwaltet.

Das Beispiel ist bewusst einfach gehalten und konzentriert sich auf die Auftragsabwicklung. Trader können es mit zusätzlichen Filtern oder Geldmanagementregeln erweitern, um fortgeschrittenere Systeme zu entwickeln.

## Details

- **Einstiegskriterien**:
  - ADX-Wert über dem Parameter `ADX Threshold`.
  - **Long**: `+DI` größer als `-DI` und vor zwei Kerzen war `+DI` unter `-DI`.
  - **Short**: `+DI` kleiner als `-DI` und vor zwei Kerzen war `+DI` über `-DI`.
  - Der aktuelle Spread muss unter dem Parameter `Max Spread` liegen.
- **Orderplatzierung**:
  - Ausstehende Stop-Orders werden `Pips` Preisschritte vom aktuellen Bid oder Ask entfernt platziert.
  - Es ist jeweils nur eine ausstehende Order aktiv; ältere Orders werden storniert, wenn ein neues Signal erscheint.
- **Ausstiegskriterien**:
  - Long-Positionen werden geschlossen, wenn `-DI` über `+DI` steigt.
  - Short-Positionen werden geschlossen, wenn `+DI` über `-DI` steigt.
- **Stops**:
  - Stop-Loss und Take-Profit werden über `StartProtection` mit den Parametern `Stop Loss` und `Take Profit` angewendet.
- **Standardwerte**:
  - `ADX Period` = 14
  - `ADX Threshold` = 5
  - `Pips` = 10 Preisschritte
  - `Take Profit` = 1000 Preisschritte
  - `Stop Loss` = 500 Preisschritte
  - `Max Spread` = 20 Preisschritte
  - `Candle Type` = 15-Minuten-Kerzen
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ADX, DMI
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Spread-Filter: Ja
