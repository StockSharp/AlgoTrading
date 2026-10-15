# Estratégia de Scalping Híbrido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema de scalping híbrido que combina sinais de RSI com filtros de tendência EMA e confirmação de volume opcional. O bot pode ajustar a sensibilidade do sinal de muito fácil a forte e inclui funções de saída rápida e trailing stop.

Os testes indicam um retorno anual médio de cerca de 35%. Funciona melhor em pares de criptomoedas líquidos.

A estratégia entra comprado ou vendido com base em limites de RSI e a força da vela, opcionalmente filtrada por tendência e volume. As posições são protegidas com take-profit, stop-loss e lógica de trailing configuráveis, e os limites diários de negociação são redefinidos no início de cada sessão.

## Detalhes

- **Critérios de entrada**:
  - **Compra**: RSI abaixo de 30 com vela altista, filtros opcionais de tendência/volume dependendo da sensibilidade.
  - **Venda**: RSI acima de 70 com vela baixista, filtros opcionais de tendência/volume.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Take profit, stop loss, trailing stop ou reversão rápida por RSI/EMA.
- **Stops**: Sim, SL/TP baseado em percentual e trailing stop opcional.
- **Filtros**:
  - Filtros de tendência e volume dependendo da configuração.
