# Estrategia LANZ 2.0 [Backtest]
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en swings que registra la estructura del mercado y la tendencia usando máximos y mínimos recientes.
Las posiciones se abren a las 02:00 hora de Nueva York tras una ruptura de estructura en la dirección de la tendencia.
El stop-loss se establece desde los puntos de swing o cobertura total y el take-profit se calcula mediante un multiplicador de riesgo/recompensa.
Cualquier posición abierta se cierra manualmente a las 11:45 hora de Nueva York.
