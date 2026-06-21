# Canales de Regresión Lineal Adaptables
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Canal de regresión lineal adaptable que ajusta el período al marco temporal y opera retrocesos.

## Detalles

- **Datos**: Velas de precio.
- **Entrada**: Comprar cuando el precio cae por debajo de la banda inferior en tendencia alcista; vender cuando el precio sube por encima de la banda superior en tendencia bajista.
- **Salida**: Cerrar cuando el precio regresa a la línea de regresión.
- **Instrumentos**: Cualquiera.
- **Riesgo**: El ancho del canal controla la exposición.
