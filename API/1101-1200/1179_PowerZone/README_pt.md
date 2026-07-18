# Estratégia PowerZone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia identifica blocos de ordens "PowerZone" criados por uma vela bearish seguida de velas bullish consecutivas (e vice-versa). Um rompimento acima da zona bullish aciona uma operação comprada, enquanto uma quebra abaixo da zona bearish abre uma vendida. Alvos e stops são baseados no intervalo da zona.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Vela bearish há `Periods+1` barras seguida de `Periods` velas bullish e o preço rompe acima do máximo da zona.
  - **Vendido**: Vela bullish há `Periods+1` barras seguida de `Periods` velas bearish e o preço rompe abaixo do mínimo da zona.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Take profit e stop loss como múltiplos do intervalo da zona.
- **Indicadores**: Nenhum.
- **Valores padrão**:
  - `Periods` = 5
  - `Threshold` = 0
  - `UseWicks` = false
  - `Take Profit Factor` = 1.5
  - `Stop Loss Factor` = 1
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
