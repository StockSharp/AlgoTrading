# Estrategia de Fusión de Indicadores para Trading Intradía
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia opera velas de 5 minutos utilizando Parabolic SAR, MACD (12,26,9), Stochastic Oscillator (5,3,3) y Momentum (14). Requiere que todos los indicadores estén alineados antes de entrar en una posición.

- **Entrada larga**: SAR por debajo del precio con SAR previo por encima del actual, Momentum < 100, línea MACD por debajo de la señal, Stochastic %K < 35.
- **Entrada corta**: SAR por encima del precio con SAR previo por debajo del actual, Momentum > 100, línea MACD por encima de la señal, Stochastic %K > 60.

Las posiciones se cierran cuando se dan las condiciones opuestas. La gestión del riesgo utiliza un stop trailing y un take profit opcional.

## Parámetros
- **Volume** – volumen de la orden.
- **Take Profit** – beneficio objetivo en puntos.
- **Trailing Stop** – distancia del stop trailing en puntos.
- **Candle Type** – tipo de suscripción de velas (por defecto 5 minutos).
