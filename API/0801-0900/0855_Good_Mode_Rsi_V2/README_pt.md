# Estratégia Good Mode RSI v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera em extremos do RSI com limites personalizados de take-profit e trailing stop. Vende quando o RSI supera um nível alto e fecha quando o RSI cai para um valor de tomada de lucro. Compra quando o RSI cai para um nível baixo e fecha quando o RSI sobe até o alvo de lucro. Em ambos os casos, um trailing stop acompanha o preço mais favorável para proteger os ganhos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `RSI < buy level`.
  - **Vendido**: `RSI > sell level`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: `RSI > take profit level buy` ou trailing stop acionado.
  - **Vendido**: `RSI < take profit level sell` ou trailing stop acionado.
- **Stops**: Trailing stop em ticks.
- **Valores padrão**:
  - `RSI Period` = 2
  - `Sell Level` = 96
  - `Buy Level` = 4
  - `Take Profit Level Sell` = 20
  - `Take Profit Level Buy` = 80
  - `Trailing Stop Offset` = 100
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
