# Estrategia de Cuatro Pantallas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Cuatro Pantallas opera usando velas Heikin-Ashi en cuatro marcos temporales: 5, 15, 30 y 60 minutos.
Entra en largo cuando todos los marcos temporales muestran velas alcistas y entra en corto cuando todos muestran velas bajistas.
Los niveles de stop-loss y take-profit se establecen en puntos con un trailing stop opcional.

## Cómo funciona
1. Se suscribe a flujos de velas de 5, 15, 30 y 60 minutos.
2. Calcula la apertura y cierre Heikin-Ashi para cada vela.
3. Marca cada marco temporal como alcista o bajista.
4. Entra en largo cuando todos son alcistas, entra en corto cuando todos son bajistas.
5. Usa `StartProtection` para aplicar stop-loss, take-profit y trailing opcional.

## Parámetros
- `CandleType` – marco temporal base para velas de 5 minutos.
- `StopLossPoints` – distancia del stop-loss en puntos.
- `TakeProfitPoints` – distancia del take-profit en puntos.
- `UseTrailing` – habilitar trailing stop (true/false).

El volumen de operaciones lo define la propiedad `Volume` de la estrategia.

## Notas
- Funciona con la API de alto nivel usando `SubscribeCandles` y `Bind`.
- Solo procesa velas finalizadas.
- Los comentarios en el código están en inglés.
