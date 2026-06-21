# Estrategia Rawstocks de Modelo de 15 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Rawstocks 15 Minute Model utiliza bloques de órdenes de swing y niveles de retroceso de Fibonacci para operar dentro de una sesión diaria.

## Cómo funciona
- Detecta máximos y mínimos de swing con un filtro ATR.
- Construye bloques de órdenes alcistas y bajistas y calcula los niveles Fibonacci del 61.8% y 79%.
- Entra en largo cuando el precio toca un bloque de órdenes alcista y cierra por encima de un nivel Fibonacci antes del tiempo de corte de entrada.
- Entra en corto cuando el precio prueba un bloque de órdenes bajista y cierra por debajo de un nivel Fibonacci.
- Cierra todas las posiciones a las 16:30 ET.

## Parámetros
- Start Hour
- Start Minute
- Last Entry Hour
- Last Entry Minute
- Force Close Hour
- Force Close Minute
- Fib Level (%)
- Min Swing Size (%)
- Risk/Reward

### Indicadores
- Average True Range
