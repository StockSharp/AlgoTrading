# Estrategia de Nivel DVD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción simplificada del asesor experto MQL5 original "DVD Level". Emplea el Range Action Verification Index (RAVI) para determinar la dirección del mercado. RAVI se calcula utilizando medias móviles exponenciales de 2 y 24 períodos en velas de 1 hora.

## Parámetros
- `Volume` – volumen de la orden utilizado para las operaciones.

## Lógica
1. Suscribirse a velas de 1 hora y calcular EMA(2) y EMA(24).
2. Calcular `RAVI = (EMA2 - EMA24) / EMA24 * 100`.
3. Si RAVI cruza por debajo de cero, la estrategia compra si está plana o corta.
4. Si RAVI cruza por encima de cero, la estrategia vende si está plana o larga.
5. La protección de posición integrada se activa a través de `StartProtection()`.

El enfoque captura posibles reversiones cuando el momentum a corto plazo diverge de la tendencia a largo plazo.
