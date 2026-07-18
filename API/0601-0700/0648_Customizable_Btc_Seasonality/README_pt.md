# Estratégia de Sazonalidade BTC Configurável
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia explora a sazonalidade intradiária do Bitcoin entrando e saindo em horas UTC definidas pelo usuário.
Uma posição comprada é aberta na hora de entrada e fechada na hora de saída.

## Detalhes

- **Critérios de entrada**: o tempo é igual à hora de entrada definida pelo usuário
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: o tempo é igual à hora de saída definida pelo usuário
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 1 minuto
  - `EntryHour` = 21
  - `ExitHour` = 23
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
