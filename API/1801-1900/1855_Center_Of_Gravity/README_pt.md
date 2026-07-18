# Estratégia de Centro de Gravidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador Center of Gravity que multiplica SMA e WMA e suaviza o resultado. Uma posição comprada é aberta quando a linha central cruza acima de sua média suavizada e uma posição vendida é aberta no cruzamento oposto. As posições são fechadas quando o sinal muda contra a direção atual.

## Detalhes

- **Critérios de entrada**: Linha central cruza sua média suavizada
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal muda de lado
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = H4
  - `Period` = 10
  - `SmoothPeriod` = 3
- **Filtros**:
  - Categoria: Indicador
  - Direção: Ambos
  - Indicadores: SMA, WMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
