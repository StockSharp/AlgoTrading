# Estratégia de Rompimento da Primeira Vela de 30m do HSI1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera rompimentos do range dos primeiros 30 minutos em um gráfico de 15 minutos, permitindo apenas uma operação por dia.

## Detalhes

- **Critérios de entrada**: O preço rompe acima/abaixo da máxima/mínima dos primeiros 30 minutos durante a sessão.
- **Comprado/Vendido**: Ambos, selecionável.
- **Critérios de saída**: Take profit ou stop loss baseado no range.
- **Stops**: Sim.
- **Valores padrão**:
  - `RiskReward` = 1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Preço
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
