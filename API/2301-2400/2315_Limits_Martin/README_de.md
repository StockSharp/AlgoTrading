# Limits Martin-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie platziert gepaarte Limitorders oberhalb und unterhalb des aktuellen Marktpreises. Jeder Trade verwendet eine konfigurierbare Schrittdistanz und optionales Martingale-Positionssizing zur Rückgewinnung früherer Verluste.

## Parameter
- **Step** – Abstand in Pips zwischen dem Marktpreis und den ausstehenden Limitorders.
- **Stop Loss** – Schutzstop-Größe in Pips für offene Positionen.
- **Take Profit** – Zielgewinn-Größe in Pips für offene Positionen.
- **Use Martingale** – aktiviert die Volumenerhöhung nach einem Verlustrade.
- **Loss Limit** – maximale Anzahl aufeinanderfolgender Verlusttrades, bevor das Volumen zurückgesetzt wird.
- **Volume** – anfängliches Ordervolumen.
- **Use MegaLot** – verdoppelt das Volumen anstatt das Basisvolumen hinzuzufügen, wenn Martingale aktiv ist.
- **Candle Type** – Kerzendatentyp für die Verarbeitung.

## Handelslogik
1. Wenn keine offene Position oder aktive Order vorhanden ist, platziert die Strategie eine Buy-Limit-Order unterhalb des letzten Schlusskurses und eine Sell-Limit-Order darüber, beide im angegebenen `Step`-Abstand.
2. Nach Ausführung einer Order bleibt die gegenläufige ausstehende Order bestehen, sodass immer nur eine aktive Position möglich ist.
3. Die Position wird geschlossen, wenn entweder der Stop-Loss- oder der Take-Profit-Level erreicht wird.
4. Nach einem Verlusttrade kann das Positionsvolumen gemäß den Martingale-Einstellungen erhöht werden.

## Hinweise
- Die Strategie verwendet die High-Level-StockSharp-API mit dem `Bind`-Ansatz zur Kerzendatenverarbeitung.
- Alle Kommentare im Code sind auf Englisch verfasst, um den Repository-Konventionen zu entsprechen.
