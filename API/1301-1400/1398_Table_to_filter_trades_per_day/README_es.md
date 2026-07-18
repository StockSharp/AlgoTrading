# Estrategia de Tabla para Filtrar Operaciones por Día
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia simple de cruce de medias móviles usando SMA50 y SMA200 con objetivos fijos de beneficio y pérdida.

## Detalles

- **Entrada**
  - Largo: SMA50 cruza por encima de SMA200.
  - Corto: SMA50 cruza por debajo de SMA200.
- **Salida**: cerrar posición cuando se alcanza el objetivo o el stop.
