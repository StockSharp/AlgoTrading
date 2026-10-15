# Estratégia Innocent Heikin Ashi Ethereum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra Ethereum quando uma sequência de velas baixistas abaixo da EMA50 é seguida por uma vela altista acima da EMA50. O stop loss é colocado na mínima mais baixa das últimas 28 barras e o take profit é calculado com o multiplicador `RiskReward`. O **Moon Mode** opcional permite entradas acima da EMA200. A posição pode fechar antecipadamente em sinais de venda ou de armadilha.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: pelo menos `ConfirmationLevel` velas vermelhas abaixo da EMA50, seguidas de uma vela verde acima da EMA50.
  - **Agressivo**: se `EnableMoonMode` for verdadeiro e o preço estiver acima da EMA200.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Stop loss na mínima mais baixa das últimas 28 barras.
  - Take profit usando o multiplicador `RiskReward`.
  - Sinais opcionais de venda ou armadilha para saída antecipada.
- **Stops**: Sim.
- **Valores padrão**:
  - `RiskReward` = 1.
  - `ConfirmationLevel` = 1.
  - `EnableMoonMode` = true.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
