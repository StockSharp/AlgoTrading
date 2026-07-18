# Strategie zur Bestätigung von Diagrammen synchronisieren
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie spiegelt die Idee des ursprünglichen MQL-Dienstprogramms „SyncCharts“ wider, indem sie zwei Kerzen-Feeds desselben Instruments überwacht und
Handelsentscheidungen nur dann treffen, wenn beide Ströme die gleiche Trendrichtung bestätigen. Als Referenzchart dient die Masterserie
(diejenige, die ein Händler normalerweise beobachtet), während die Follower-Reihe ein Hilfsdiagramm darstellt (z. B. einen schnelleren Zeitrahmen oder).
eine alternative Aggregation). Indem das System beide Streams dazu zwingt, vor dem Markteintritt zuzustimmen, filtert es das von ihnen ausgehende Rauschen heraus
vorübergehende Desynchronisation zwischen Diagrammintervallen.

Das Setup funktioniert am besten bei Instrumenten, die eine Trendstruktur mit mehreren Zeitrahmen aufweisen, wie z. B. Index-Futures und liquide Währungspaare.
Da sich beide Charts gemeinsam bewegen müssen, bevor ein Trade getätigt wird, werden Fehlsignale reduziert und die Strategie wird auf natürliche Weise eingeschränkt
Engagement in chaotischen Marktphasen, wenn die Zeitrahmen nicht übereinstimmen oder neue Kerzen zu unterschiedlichen Zeitpunkten gedruckt werden.

## Einzelheiten

- **Eintrittskriterien**:
  - **Long**: Sowohl die einfachen gleitenden Durchschnitte (SMAs) des Masters als auch des Followers steigen bei ihren letzten abgeschlossenen Kerzen an und
Die Zeitstempel dieser Kerzen unterscheiden sich um weniger als die Synchronisierungstoleranz.
  - **Kurz**: Beide SMAs fallen ab und die Zeitstempeldifferenz liegt innerhalb des Toleranzfensters.
- **Ausstiegskriterien**:
  - Zeitliche Desynchronisierung: Wenn die letzten Kerzen um mehr als die zulässige Toleranz voneinander entfernt sind, wird die Position abgeflacht.
  - Trendunstimmigkeit: Wenn ein SMA nach oben zeigt, während der andere nach unten zeigt, wird die offene Position sofort geschlossen.
- **Stoppt**: Die implizite Flatten-Logik fungiert als sanfter Stopp. Es wird kein separater Hardstop übermittelt.
- **Long/Short**: Es wird auf beiden Seiten gehandelt.
- **Standardwerte**:
  - Master-Kerze: 5-Minuten-Zeitrahmen.
  - Folgekerze: 1 Minute Zeitrahmen.
  - SMA Länge: 20 Perioden in beiden Streams.
  - Synchronisationstoleranz: 15 Sekunden zwischen den Kerzenöffnungszeiten.
- **Filter**:
  - Kategorie: Trendbestätigung / Multi-Timeframe.
  - Richtung: Bidirektional.
  - Indikatoren: SMA (Dual-Stream).
  - Stopps: Kein fester Stopp, automatische Abflachung bei Divergenz der Charts.
  - Komplexität: Mittel (Mehrfachabonnement mit Synchronisierungsprüfungen).
  - Zeitrahmen: Konfigurierbar (Standard Intraday).
  - Saisonalität: Keine.
  - Neuronale Netze: Nein.
  - Divergenz: Verwendet Zeitrahmendivergenz als Filter (erfordert Vereinbarung, keine Preisdivergenz).
  - Risikostufe: Moderat aufgrund der Bestätigungspflicht.

## Wie es funktioniert

1. Über den High-Level StockSharp API werden zwei Kerzenabonnements erstellt: eines für das Master-Chart und eines für den Follower.
2. Jeder Feed wird von einem SMA mit der gleichen Länge verarbeitet, was ein Trendrichtungsflag (`up` ergibt, wenn der Wert von SMA im Vergleich zu steigt
vorherige Kerze, andernfalls `down`).
3. Immer wenn beide Kerzen enden, überprüft die Strategie, ob ihre Zeitstempel nahe genug beieinander liegen (absolute Differenz unter dem
konfigurierte Toleranz).
4. Wenn die Diagramme synchronisiert sind und beide Trends nach oben zeigen, kauft die Strategie (alle Short-Positionen werden zuerst geschlossen). Wenn beide Trends nach unten zeigen,
es wird leerverkauft (alle Long-Positionen werden zuerst geschlossen).
5. Jeder Synchronisationsverlust oder jede Trendabweichung löst eine sofortige Abflachung aus, um das Konto an den Diagrammen auszurichten
Händleruhren.

## Empfohlene Verwendung

- Auf dasselbe Instrument in zwei verschiedenen Zeitrahmen anwenden, die normalerweise korrelieren (z. B. 5 Minuten und 1 Minute oder stündlich und).
15 Minuten).
- Erhöhen Sie die Synchronisierungstoleranz, wenn Sie mit exotischen Datenquellen arbeiten, die Kerzen mit geringfügigen Verzögerungen drucken.
- Kombinieren Sie es mit einem externen Risikomanager oder einem Add-on-Stopp-Modul, wenn Sie es für den Live-Handel einsetzen.
