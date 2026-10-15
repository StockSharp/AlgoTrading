# Estratégia de Acumulação em Níveis Não Mitigados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Acumula posições compradas colocando ordens limitadas nas mínimas anteriores de dia, semana, mês e ano que não foram revisitadas recentemente. As ordens são colocadas apenas durante a sessão de Londres e todas as posições são fechadas em novas máximas históricas.

## Detalhes

- **Critérios de entrada**:
  - Compras limitadas em mínimas históricas não mitigadas durante o horário da sessão.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Fechar tudo em nova máxima histórica.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Max Lookback` = 50
  - `Session Start` = 09:00
  - `Session End` = 17:00
  - `Base PDL` = 0.1
  - `Base PWL` = 0.2
  - `Base PML` = 0.4
  - `Base PYL` = 0.8
- **Filtros**:
  - Categoria: Mean Reversion
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Sim (sessão de Londres)
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
