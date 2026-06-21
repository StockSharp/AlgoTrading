# Estratégia de Trading MH Baseada em Hull Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento baseada em Hull Moving Average.

A estratégia compara o preço de abertura com níveis dinâmicos derivados da Hull Moving Average. Entra comprado quando o preço rompe acima do nível superior e vendido quando cai abaixo do nível inferior. As posições existentes são encerradas em rompimentos opostos.

## Detalhes

- **Critérios de entrada**: Relação do preço com os níveis de Hull Moving Average.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Rompimento oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `HullPeriod` = 210
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
