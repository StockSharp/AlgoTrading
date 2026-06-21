# Estratégia Color XXDPO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza um Oscilador de Preço Detrendido duplamente suavizado para capturar reversões de inclinação.

## Detalhes
- **Critérios de entrada**: Inclinação ascendente com valor atual em alta abre comprado; inclinação descendente com valor atual em queda abre vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: A mudança de inclinação oposta fecha as posições.
- **Stops**: Nenhum.
- **Valores padrão**: Comprimento da primeira MA 21, comprimento da segunda MA 5, período de candles 6 horas.
- **Filtros**: Nenhum.
