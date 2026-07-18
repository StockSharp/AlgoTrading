# Estratégia Heiken Ashi Suavizado Somente Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente comprado usando velas Heikin-Ashi suavizadas. Compra quando a vela suavizada muda de vermelho para verde e sai quando volta a ficar vermelha.

## Detalhes

- **Critérios de entrada**: HA suavizado muda de vermelho para verde
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: HA suavizado fica vermelho
- **Stops**: Nenhum
- **Valores padrão**:
  - `EmaLength` = 10
  - `SmoothingLength` = 10
