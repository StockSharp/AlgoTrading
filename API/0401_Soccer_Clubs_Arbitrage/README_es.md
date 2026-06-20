# Estrategia de Arbitraje de Clubes de Fútbol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca oportunidades de arbitraje entre fan tokens de clubes de fútbol negociados en múltiples venues. Observando los spreads de precios y los desequilibrios en las tasas de financiamiento, abre posiciones largas y cortas compensatorias para capturar las ineficiencias de precios.

Una operación se activa cuando el spread entre los intercambios supera un umbral. Las posiciones se cubren y se cierran cuando los precios convergen o se alcanza un stop protector.

## Detalles

- **Datos**: Precios de fan tokens y tasas de financiamiento.
- **Entrada**: Abrir posiciones opuestas cuando el spread > X%.
- **Salida**: Cerrar cuando el spread < Y% o al llegar al stop por tiempo.
- **Instrumentos**: Fan tokens listados en intercambios.
- **Riesgo**: Stop de porcentaje fijo para proteger contra el deslizamiento.

