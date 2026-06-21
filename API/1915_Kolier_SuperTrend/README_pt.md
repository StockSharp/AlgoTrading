# Kolier SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Kolier SuperTrend que aplica bandas ATR para detectar reversões de tendência.

O indicador desenha níveis dinâmicos de suporte e resistência derivados do ATR. Uma reversão de alta ocorre quando o preço fecha acima da banda inferior e a linha vira abaixo do preço. Uma reversão de baixa ocorre quando o preço fecha abaixo da banda superior.

Seguindo essa trilha adaptativa, a estratégia tenta capturar tendências fortes enquanto permanece protegida quando o momentum desaparece.

## Detalhes

- **Critérios de entrada**: O preço cruza a linha SuperTrend.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ATR, SuperTrend
  - Stops: Não
  - Complexidade: Básico
  - Período: Swing (4h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
