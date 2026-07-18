# Estrategia de Seguimiento de Tendencia con MAs 3D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza dos medias móviles simples cortas para detectar la dirección de la tendencia.
Se abre una posición larga cuando la media de 5 períodos está por encima de la media de 10 períodos.
Se abre una posición corta cuando ocurre lo contrario.

## Detalles

- **Entrada**:
  - **Largo**: SMA(5) > SMA(10)
  - **Corto**: SMA(5) < SMA(10)
- **Salida**: señal inversa
- **Indicadores**: SMA
- **Marco temporal**: configurable
- **Tipo**: Seguimiento de tendencia
