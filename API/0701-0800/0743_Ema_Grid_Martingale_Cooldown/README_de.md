# EMA-Grid-Martingale-Cooldown-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert ein EMA-basiertes Long-only-Grid-System mit optionaler Martingale-Positionsgröße und Abkühlphase zwischen den Grids. Ein neues Grid startet, wenn beide schnellen EMAs ihre langsamen Gegenstücke nach oben kreuzen. Weitere Käufe werden in festen Pip-Abständen platziert, und die Position wird zum gewichteten Durchschnittspreis zuzüglich eines Puffers geschlossen.
