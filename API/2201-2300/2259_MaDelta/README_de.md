# MaDelta-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MaDelta-Strategie misst die Differenz zwischen einem schnellen und einem langsamen gleitenden Durchschnitt. Die Differenz wird mit einem Multiplikator skaliert und zur dritten Potenz erhoben, was einen oszillierenden Wert `px` erzeugt. Zwei dynamische Schwellenwerte, die durch `Delta` (in Pips) getrennt sind, verfolgen das jüngste Hoch und Tief dieses Werts. Wenn `px` über den oberen Schwellenwert bricht, wechselt die Strategie zu einer Long-Ausrichtung; wenn `px` unter den unteren Schwellenwert fällt, wechselt sie zu einer Short-Ausrichtung. Bestehende Positionen entgegen der neuen Ausrichtung werden geschlossen und ein neuer Trade in Richtung des Signals eröffnet.

Der Ansatz erfasst effektiv Momentum-Bursts, wenn sich der Abstand zwischen den zwei gleitenden Durchschnitten schnell ausweitet. Das Potenzieren der Differenz übertreibt starke Bewegungen, während kleine Schwankungen herausgefiltert werden. Der Parameter `Delta` definiert, wie weit `px` reisen muss, bevor eine Umkehrung erkannt wird, und verhindert Whipsaw in flachen Märkten.

## Details

- **Einstiegskriterien**:
  - **Long**: `px > hi` setzt `trade = 1` und öffnet ein Long, wenn keine Position besteht.
  - **Short**: `px < lo` setzt `trade = -1` und öffnet ein Short, wenn flach.
- **Umkehrlogik**:
  - Long-Signal bei Short-Position schließt das Short mit einem Markt-Kauf, bevor Long eingegangen wird.
  - Short-Signal bei Long-Position schließt das Long mit einem Markt-Verkauf, bevor Short eingegangen wird.
- **Indikatoren**:
  - Schneller gleitender Durchschnitt (SMA) mit Periode `FastMaPeriod`.
  - Langsamer gleitender Durchschnitt (EMA) mit Periode `SlowMaPeriod`.
  - Oszillator: `px = ((Multiplier * 0.1) * (FastMA - SlowMA))^3`.
- **Parameter**:
  - `Delta` – Größe des Hoch/Tief-Kanals in Pips.
  - `Multiplier` – skaliert die MA-Differenz vor dem Potenzieren.
  - `FastMaPeriod` – Länge des schnellen SMA.
  - `SlowMaPeriod` – Länge des langsamen EMA.
  - `Volume` – Ordervolumen bei Einstiegen.
  - `CandleType` – Zeitrahmen der verarbeiteten Kerzen.
- **Sonstige Hinweise**:
  - Funktioniert nur mit abgeschlossenen Kerzen.
  - Kein expliziter Stop-Loss oder Take-Profit; Positionen kehren bei entgegengesetzten Signalen um.
  - Verwendet High-Level-API mit Indikatorbindung und automatischer Darstellung.
