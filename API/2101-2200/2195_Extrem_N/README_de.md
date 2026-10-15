# Extrem N-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Extrem N-Strategie handelt Umkehrungen basierend auf neuen Hochs und Tiefs, die in einem gleitenden Fenster erkannt werden.

Die Strategie stützt sich auf den Donchian-Channel-Indikator, um Kursextreme zu markieren. Wenn eine Kerze ein neues Hoch relativ zur Lookback-Periode setzt und die folgende Kerze ein neues Tief setzt, wird eine Long-Position eröffnet. Eine Short-Position wird eröffnet, wenn auf ein neues Tief ein neues Hoch folgt. Entgegengesetzte Signale schließen bestehende Positionen.

- **Einstiegsbedingungen**:
  - Long: Die vorherige Kerze hat ein neues Hoch gesetzt und die aktuelle Kerze hat ein neues Tief gesetzt.
  - Short: Die vorherige Kerze hat ein neues Tief gesetzt und die aktuelle Kerze hat ein neues Hoch gesetzt.
- **Ausstiegsbedingungen**:
  - Long-Positionen werden bei einem Short-Einstiegssignal geschlossen.
  - Short-Positionen werden bei einem Long-Einstiegssignal geschlossen.
- **Parameter**:
  - `Period` – Donchian-Lookback-Periode (Standard: 9).
  - `CandleType` – Verarbeitungszeitrahmen (Standard: 4 Stunden).
  - `BuyPosOpen` – Long-Positionen öffnen erlauben (Standard: true).
  - `SellPosOpen` – Short-Positionen öffnen erlauben (Standard: true).
  - `BuyPosClose` – Long-Positionen schließen erlauben (Standard: true).
  - `SellPosClose` – Short-Positionen schließen erlauben (Standard: true).
- **Indikatoren**: Donchian Channel.
