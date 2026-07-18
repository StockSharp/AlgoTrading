# Estratégia de Trailing Stop EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia gerencia uma posição existente aplicando um trailing stop. Ela monitora operações tick a tick e desloca o nível de stop à medida que o preço se move em uma direção favorável. Quando o mercado reverte e atinge o nível de trailing, a estratégia encerra a posição.

## Detalhes

- **Entrada**: A estratégia não abre posições; assume que uma posição já está aberta.
- **Lógica comprado**: Para posições compradas, o stop acompanha o preço para cima assim que o preço sobe pela distância do trailing.
- **Lógica vendido**: Para posições vendidas, o stop se move para baixo conforme o preço cai.
- **Saída**: A posição é fechada quando o preço atinge o trailing stop.
- **Indicadores**: Nenhum.
- **Período**: Baseado em ticks, reage a cada operação.
- **Stops**: Apenas trailing stop.

## Parâmetros

- `TrailingPoints` — distância do trailing stop em pontos (passos de preço). Padrão: 200.
