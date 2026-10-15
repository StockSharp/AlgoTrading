# Estratégia de Engolfo de Oferta e Demanda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera padrões de engolfo de alta e de baixa perto das zonas de suporte e resistência de Donchian.

## Detalhes

- **Critérios de entrada**: Padrão de engolfo nos limites da zona.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `ZonePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Donchian
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim (engulfing)
  - Nível de risco: Médio
