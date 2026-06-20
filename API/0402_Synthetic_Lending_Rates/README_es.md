# Estrategia de Tasas de Préstamo Sintéticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia explota las diferencias entre las tasas de préstamo sintéticas derivadas de los mercados de derivados y los rendimientos de préstamo on-chain. Al pedir prestado donde las tasas son bajas y prestar donde las tasas son altas, captura el spread entre ellas.

Las posiciones se rebalancean regularmente para mantener la neutralidad, y el riesgo se controla mediante umbrales de cambio de tasas y filtros de liquidez.

## Detalles

- **Datos**: Financiamiento de swaps perpetuos y tasas de préstamo DeFi.
- **Entrada**: Pedir prestado en el venue de tasa baja y prestar en el de tasa alta cuando el spread > umbral.
- **Salida**: Cerrar cuando el spread revierta a la media o la liquidez se deteriore.
- **Instrumentos**: Swaps perpetuos y plataformas DeFi.
- **Riesgo**: Límite de spread y stop de liquidez.

