# Estrategia de Cambio de Mes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este patrón estacional compra índices de renta variable unos días antes del fin de mes y sale poco después de que comience el nuevo mes, con el objetivo de capturar el efecto "cambio de mes".

El sistema permanece en efectivo fuera de este período para reducir la exposición.

## Detalles

- **Datos**: Niveles diarios del índice.
- **Entrada**: Comprar N días antes del fin de mes.
- **Salida**: Vender M días después del inicio de mes.
- **Instrumentos**: Futuros sobre índices de renta variable o ETF.
- **Riesgo**: Sin posiciones fuera de la ventana programada.

