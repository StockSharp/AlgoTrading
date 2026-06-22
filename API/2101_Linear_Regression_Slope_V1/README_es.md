# Pendiente de Regresión Lineal V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza la pendiente de una regresión lineal y su copia desplazada para detectar cambios de tendencia.

## Detalles
- **Datos**: Velas de precio.
- **Entrada**:
  - Comprar cuando la pendiente cruza por debajo de su valor desplazado.
  - Vender cuando la pendiente cruza por encima de su valor desplazado.
- **Salida**: La señal opuesta cierra la posición.
- **Instrumentos**: Cualquier instrumento.
- **Riesgo**: Sin stop ni objetivo integrado.
