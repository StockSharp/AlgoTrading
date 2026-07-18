# Estratégia Premarket Gap MomoTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera uma única ruptura comprada durante a sessão de pré-mercado quando a vela atual ganha pelo menos uma porcentagem especificada acima do fechamento anterior, imprime uma vela bullish com volume suficiente e o corpo da vela ocupa grande parte de seu intervalo. O tamanho da posição é escalado dependendo do tamanho do corpo.

Após a entrada, a estratégia mantém a posição enquanto as próximas velas permanecerem bullish e seu volume aumentar. Uma vela vermelha ou volume não crescente fecha a posição. Apenas uma operação é permitida por dia e o trading pode ser restrito à sessão 04:00–09:30.

## Detalhes

- **Critérios de entrada**:
  - Ganho da vela atual ≥ `MinGainPct` comparado ao fechamento anterior.
  - A vela é verde e `Volume` > `MinVolume`.
  - O percentual do corpo define o tamanho da posição: ≥90% → 100%, ≥85% → 50%, ≥75% → 25%.
  - Filtro de sessão opcional 04:00–09:30 se `UseSession` estiver habilitado.
- **Critérios de saída**:
  - Primeira vela vermelha ou vela com volume não crescente após a entrada.
- **Stops**: Não.
- **Valores padrão**:
  - `MinGainPct` = 5.
  - `MinVolume` = 15000.
  - `UseSession` = true.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Médio
  - Período: Intradiário
