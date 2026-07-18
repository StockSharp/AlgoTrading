# Estratégia Medico Action Zone Self Adjust TF Versão 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de cruzamento de EMA com confirmação de período de tempo superior. Uma posição é aberta quando a EMA rápida cruza acima da EMA lenta e o fechamento do período superior está acima da EMA rápida. A posição se inverte ao sinal contrário.

## Detalhes

- **Critérios de entrada**: EMA rápida cruza acima da EMA lenta com o fechamento do período superior acima da EMA rápida.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto com confirmação.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
