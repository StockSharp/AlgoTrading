# VR Overturn Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **VR Overturn Strategie** implementiert eine einfache Martingal/Anti-Martingal-Logik.
Sie hält immer nur eine einzige Position und eröffnet nach deren Schließung sofort eine neue,
basierend auf dem Ergebnis des vorherigen Trades.

## Strategielogik

1. Erste Position gemäß `StartSide` mit Volumen `StartVolume` eröffnen.
2. Stop-Loss und Take-Profit anhand von Punktoffsets anhängen.
3. Wenn die Position geschlossen wird:
   - Gewinn des letzten Trades berechnen.
   - Für den **Martingale**-Modus:
     - Nach einem profitablen Trade: Volumen auf `StartVolume` zurücksetzen und dieselbe Richtung beibehalten.
     - Nach einem Verlust-Trade: Volumen mit `Multiplier` multiplizieren und Richtung umkehren.
   - Für den **AntiMartingale**-Modus:
     - Nach einem profitablen Trade: Volumen mit `Multiplier` multiplizieren und dieselbe Richtung beibehalten.
     - Nach einem Verlust-Trade: Volumen auf `StartVolume` zurücksetzen und Richtung umkehren.
4. Nächste Position mit der berechneten Richtung und dem berechneten Volumen eröffnen.

Der Vorgang wiederholt sich unbegrenzt, solange die Strategie läuft.

## Parameter

| Name | Beschreibung |
|------|--------------|
| `Mode` | Handelsmodus: `Martingale` oder `AntiMartingale`. |
| `StartSide` | Seite des allerersten Trades (`Buy` oder `Sell`). |
| `TakeProfit` | Take-Profit-Wert in Punkten vom Einstiegspreis. |
| `StopLoss` | Stop-Loss-Wert in Punkten vom Einstiegspreis. |
| `StartVolume` | Anfangsvolumen für die erste Order. |
| `Multiplier` | Multiplikator, der nach Gewinn oder Verlust auf das Volumen angewendet wird. |

## Hinweise

- Schutzorders werden als Stop- und Limitorders registriert.
- Zu jedem Zeitpunkt existiert nur eine Position.
- Die Strategie verwendet keine Marktindikatoren.
