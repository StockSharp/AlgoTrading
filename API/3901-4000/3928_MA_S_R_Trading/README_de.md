# MA S.R. Handelsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Der MA S.R. Die Handelsstrategie ist ein Trendumkehrsystem, das vom ursprünglichen MetaTrader-Berater „MA S.R Trading“ übernommen wurde. Es überwacht die Form eines kurzen einfachen gleitenden Durchschnitts (SMA), um zu erkennen, wann die Preisdynamik in ein lokales Hoch oder Tief abknickt. Wenn der SMA seinen Höhepunkt oder Tiefpunkt erreicht, geht die Strategie sofort in die Richtung der Wende über und schützt die Position mit einem Stop-Level, das beim letzten Schwung verankert ist.

Im Gegensatz zu klassischen Crossover-Systemen, die mehrere gleitende Durchschnitte mit unterschiedlichen Längen vergleichen, analysiert dieser Ansatz die Krümmung desselben SMA, indem sein Wert mit den drei zuletzt abgeschlossenen Kerzen verglichen wird. Ein lokales Maximum (`SMA[t-2]` größer als `SMA[t-1]` und `SMA[t-3]`) signalisiert eine rückläufige Umkehr und löst einen Short-Einstieg aus. Ein lokales Minimum (`SMA[t-2]` unter beiden Nachbarn) signalisiert eine zinsbullische Umkehr und eröffnet eine Long-Position. Unmittelbar nach einem Signal speichert die Strategie den Extrempreis über ein konfigurierbares Lookback-Fenster und verwendet ihn als Schutzstopp.

Die Exit-Logik ahmt die nachgestellte Änderung aus der MQL-Quelle nach. Bei Short-Trades wird der Stop auf das höchste Hoch innerhalb des Lookback-Fensters gesetzt, vorausgesetzt, dass dieses Niveau über dem vorherigen Schlusskurs bleibt (andernfalls wird das Niveau ignoriert). Bei Long-Positionen wird nach der gleichen Regel das niedrigste Tief verwendet. Wenn der Preis bei nachfolgenden Kerzen das gespeicherte Niveau berührt, schließt die Strategie die Position zum Marktwert und emuliert so effektiv die Stop-Loss-Aktualisierung des ursprünglichen Experten.

Das System ist für Instrumente konzipiert, die auf Intraday- und Kurzzeit-Charts ein ausgeprägtes Swing-Verhalten aufweisen. Kurze SMA-Perioden (Standard = 5) ermöglichen es dem Algorithmus, schnell auf Mikrostrukturänderungen zu reagieren, während der Stop-Lookback (Standard = 5 Balken für Hochs und Tiefs) steuert, wie aggressiv das nachlaufende Niveau dem Preis folgt. Verwenden Sie engere Fenster für Scalping-Umgebungen und breitere Einstellungen für lautere Märkte.

Backtests zu FX-Majors und liquiden Index-CFDs zeigen die beste Performance in schwankenden Zeiträumen mit häufigen Schwankungen. Trends mit sanften Rückschlägen erfordern möglicherweise zusätzliche Filter oder eine Bestätigung der Volatilität, um vorzeitige Umkehrungen zu vermeiden. Erwägen Sie, die Strategie bei der Live-Bereitstellung mit einem breiteren Marktkontext oder Zeitfiltern zu kombinieren.

## Einzelheiten

- **Eintrittsbedingungen**
  - **Kurz**: `SMA[t-1] < SMA[t-2]` UND `SMA[t-3] < SMA[t-2]`. Die letzte abgeschlossene SMA-Probe bildet ein lokales Maximum.
  - **Lang**: `SMA[t-1] > SMA[t-2]` UND `SMA[t-3] > SMA[t-2]`. Die letzte abgeschlossene SMA-Stichprobe bildet ein lokales Minimum.
- **Management stoppen**
  - **Short**: Stop-Level = höchstes Hoch innerhalb von `HighLookback` Kerzen, wenn das Level über dem vorherigen Schlusskurs liegt. Wird beendet, wenn der Preis das Niveau erreicht.
  - **Long**: Stop-Level = niedrigstes Tief innerhalb von `LowLookback` Kerzen, wenn das Level unter dem vorherigen Schlusskurs liegt. Wird beendet, wenn der Preis das Niveau erreicht.
- **Positionsregeln**: Wechselt immer zum neuesten Signal. Beim Umkehren schließt die Strategie die bestehende Position und eröffnet die neue in einer einzigen Marktorder, deren Größe das vorherige Engagement plus das gewünschte Volumen abdeckt.
- **Standardparameter**
  - `SmaPeriod` = 5.
  - `HighLookback` = 5.
  - `LowLookback` = 5.
  - `CandleType` = 30-minütiger Zeitrahmen.
  - `TradeVolume` = 1 Grundstück (wird beim Start auf die Immobilie `Volume` angewendet).
- **Filter**
  - Kategorie: Umkehrung.
  - Richtung: Sowohl lang als auch kurz.
  - Indikatoren: Einfacher gleitender Durchschnitt, Höchster/Tiefster Swing-Tracker.
  - Stopps: Dynamisch, schwungbasiert.
  - Zeitrahmen: Intraday zum Swingen.
  - Komplexität: Mittel.
  - Risikostufe: Moderat (enge Stopps, aber häufige Trades).

## Nutzungshinweise

1. Funktioniert am besten bei Instrumenten mit sichtbaren Schwingungen. Erwägen Sie, den Handel im Zusammenhang mit wichtigen Nachrichtenereignissen zu deaktivieren, um falsche Schwankungen zu vermeiden.
2. Optimieren Sie den Zeitraum SMA und die Lookback-Fenster für das Zielsymbol und den Zielzeitraum. Kleinere Einstellungen erhöhen die Empfindlichkeit, aber auch die Peitschenhiebe.
3. Die Stoppwerte werden erst neu berechnet, wenn ein neues Blinkersignal erscheint. Wenn ein Stop ungültig wird (z. B. wenn das Hoch nicht über dem vorherigen Schlusskurs liegt), wird er verworfen, wodurch verhindert wird, dass die Strategie Schutzniveaus zu nahe am Preis platziert.
4. Da Ausstiege auf Marktaufträgen beruhen, kann es bei schnellen Bewegungen zu Slippage kommen. Kombinieren Sie es mit Schutzanordnungen auf Maklerseite, wenn der Veranstaltungsort diese unterstützt.
5. Die Strategie verwendet keine Take-Profit-Ziele. Um sie hinzuzufügen, erweitern Sie die Logik in `ProcessCandle` um zusätzliche Bedingungen.
