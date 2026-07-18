# Estratégia de Seguimento de Tendência com Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia segue a tendência usando uma média móvel e sinais simples de velas.
Compra quando o preço está acima da média móvel com uma vela de alta rompendo a resistência pivot, e vende quando o preço está abaixo da média móvel com uma vela de baixa rompendo o suporte pivot.

## Detalhes

- **Critérios de entrada**: vela de alta/baixa acima/abaixo da MA rompendo níveis pivot
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `MaPeriod` = 10
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
