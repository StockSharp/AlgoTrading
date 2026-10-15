# Estratégia de Sazonalidade BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma posição com base em regras predefinidas de dia da semana e hora usando o Horário Padrão do Leste (EST). O usuário escolhe o dia e hora de entrada, o dia e hora de saída, e se operar comprado ou vendido. A posição é aberta no momento de entrada especificado e fechada no momento de saída especificado.

## Detalhes

- **Critérios de entrada**:
  - O dia EST atual é igual a `EntryDay` e a hora atual é igual a `EntryHour`.
- **Comprado/Vendido**: Configurável.
- **Critérios de saída**:
  - O dia EST atual é igual a `ExitDay` e a hora atual é igual a `ExitHour`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `EntryDay` = Saturday
  - `ExitDay` = Monday
  - `EntryHour` = 10
  - `ExitHour` = 10
  - `IsLong` = true
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Configurável
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
