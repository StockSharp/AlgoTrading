# Estratégia de Razão de Bias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Razão de Bias opera rompimentos baseados no desvio do preço em relação a médias móveis de longo prazo. Ela compara o preço de fechamento com uma média móvel exponencial (EMA) e uma média móvel simples (SMA). Uma posição comprada é aberta quando o preço supera a EMA por uma razão especificada, enquanto uma posição vendida é aberta quando o preço cai abaixo da SMA pela mesma razão.

## Detalhes

- **Critérios de entrada**:
  - `close / EMA >= 1 + BiasThreshold` → entrar comprado.
  - `close / SMA <= 1 - BiasThreshold` → entrar vendido.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - O sinal oposto fecha e reverte as posições.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MaPeriod` = 200
  - `BiasThreshold` = 0.025
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: EMA, SMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
