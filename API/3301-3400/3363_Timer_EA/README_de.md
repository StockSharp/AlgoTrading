# Timer EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des MetaTrader TimerEA-Roboters. Der Schwerpunkt liegt auf der Eröffnung und Schließung von Geschäften zu geplanten Terminen
mit optionalem Pending Orders, Trailing Protection und Break-Even-Handling.

## Handelslogik

- **Zeitplan**
  - `OpenTime` löst die Auftragserteilung aus, sobald die erste fertige Kerze die konfigurierte Minute erreicht.
  - `CloseTime` erzwingt die Positionsauflösung und storniert optional verbleibende ausstehende Aufträge.
- **Bestellmodi**
  - Es können Markt-, Stop- oder Limiteinträge ausgewählt werden. Ausstehende Aufträge werden in einem konfigurierbaren Abstand (in Preisschritten) platziert und können
verfallen nach der angegebenen Anzahl von Minuten.
- **Richtungskontrolle**
  - Separate Schalter ermöglichen die Aktivierung von Long- und/oder Short-Trades. Jede Seite gibt pro Lauf einen Auftrag ab.
- **Risikomanagement**
  - Die Größenbestimmung mit festem Volumen oder auf Gleichgewichtsbasis (unter Verwendung von `RiskFactor`) ahmt die ursprüngliche Losauswahl nach.
  - Stop-Loss- und Take-Profit-Abstände werden in Preisschritten ausgedrückt und nach jedem Eintrag neu erstellt.
  - Die Trailing-Stop-Logik hält den Stop auf einem konstanten Offset, sobald der Gewinn den `BreakEvenSteps`-Puffer überschreitet. Der Weg wird aktiviert
nur, wenn der Stopp bereits über dem anfänglichen Versatz plus `TrailingStep` liegt.
- **Schutzmaßnahmen**
  - Die optionale Break-Even-Anforderung verhindert ein Nachlaufen, bis die Mindestgewinnschwelle erreicht ist.
  - Ausstehende Bestellungen, deren Ablaufdatum abgelaufen ist, werden automatisch storniert.

## Standardparameter

- Bestellmodus: Markt.
- Offener Kauf/Verkauf: deaktiviert.
- Take Profit / Stop Loss: jeweils 10 Schritte.
- Trailing Stop und Break-Even: deaktiviert.
- Ausstehende Distanz: 10 Schritte mit Ablauf von 60 Minuten.
- Losgröße: Manuelles Volumen = 1,0 (Risikofaktor = 1,0 für Balance-Modus).
- Kerzentyp: 1-Minuten-Zeitrahmen.

## Notizen

- Die Strategie arbeitet mit fertigen Kerzen und reagiert daher mit bis zu einem Balken Latenz.
- StockSharp verwendet ein Netting-Positionsmodell, sodass ein gleichzeitiges Long- und Short-Engagement nicht unterstützt wird, selbst wenn beide Umschaltmöglichkeiten vorhanden sind
aktiviert.
- Preisstufen werden mit `Security.PriceStep` berechnet. Instrumente ohne konfigurierten Schritt behandeln Entfernungen als Rohpreis
Punkte.
