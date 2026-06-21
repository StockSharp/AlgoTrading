# Estratégia de Seguimento de Tendência MM3 Máximos e Mínimos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza uma média móvel simples de 3 períodos de máximos e mínimos. Uma posição comprada é aberta quando o preço fecha acima da SMA dos máximos e encerrada quando o preço cai abaixo da SMA dos mínimos.

## Detalhes

- **Critérios de entrada**: Close > SMA(high).
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Close < SMA(low).
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
