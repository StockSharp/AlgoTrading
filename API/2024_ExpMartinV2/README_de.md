# Exp Martin V2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Exp Martin V2-Strategie implementiert einen exponentiellen Martingale-Ansatz. Es wird immer nur eine einzige offene Position gehalten, und nach jedem Trade wird die nächste Richtung und das Volumen basierend auf dem Gewinn des letzten Geschäfts entschieden.

Die Strategie beginnt mit einem vordefinierten Auftragstyp (Kauf oder Verkauf) und einem Startvolumen. Jede Position erhält einen festen Take-Profit und Stop-Loss. Wenn ein Trade mit Gewinn endet, wird eine neue Position gleicher Art mit dem Startvolumen eröffnet. Endet der Trade mit Verlust, wird die Richtung umgekehrt und das Volumen mit einem angegebenen Faktor multipliziert. Die Multiplikation setzt sich nach jedem Verlust fort, bis eine maximale Anzahl von Multiplikationen erreicht ist; dann wird das Volumen auf den Startwert zurückgesetzt.

Dies erzeugt eine eskalierende Sequenz entgegengesetzter Trades, die darauf abzielt, frühere Verluste zu erholen, sobald eine profitable Bewegung eintritt.

## Details

- **Einstiegslogik**:
  - Anfangsposition gemäß *Start Type* (0 - Kauf, 1 - Verkauf) mit dem *Start Volume* eröffnen.
  - Nach einem profitablen Trade dieselbe Richtung mit dem Startvolumen wiederholen.
  - Nach einem verlustbringenden Trade die Richtung umkehren und das Volumen mit *Factor* multiplizieren, bis *Limit* Multiplikationen erreicht sind.
- **Long/Short**: Beide, abhängig von der aktuellen Sequenz.
- **Ausstiegslogik**:
  - Positionen werden geschlossen, wenn der Preis die konfigurierten *Take Profit*- oder *Stop Loss*-Niveaus erreicht.
- **Stops**: Feste Stop-Loss und Take-Profit in Punkten.
- **Filter**: Keine.
- **Positionsverwaltung**: Nur eine Position ist gleichzeitig offen.

Verwenden Sie diese Strategie, um mit Martingale-Geldmanagement in StockSharp ohne zusätzliche Indikatoren zu experimentieren.
