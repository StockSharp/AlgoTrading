# Estratégia Comprada no Máximo/Mínimo do Dia Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia fica comprada quando o preço rompe acima do máximo ou mínimo do dia anterior durante uma sessão especificada e o ADX indica fortalecimento do momentum de alta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o fechamento cruza acima do máximo ou mínimo do dia anterior com ADX em alta durante a sessão.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - stop dinâmico e alvos de lucro ou ao final da sessão.
- **Stops**: Trailing stop.
- **Valores padrão**:
  - `MaxProfit` = 150.
  - `MaxStopLoss` = 15.
  - `AdxLength` = 11.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado
  - Indicadores: ADX
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
