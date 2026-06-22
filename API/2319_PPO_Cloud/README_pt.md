# Estratégia PPO Nuvem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de momentum negocia cruzamentos entre o Percentage Price Oscillator (PPO) e sua linha de sinal. Uma posição comprada é aberta quando o PPO cruza acima de sua linha de sinal, enquanto uma posição vendida é aberta no cruzamento oposto. As posições existentes podem opcionalmente ser fechadas no sinal contrário. A estratégia opera em um único período.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `PPO cruza acima do sinal`.
  - **Vendido**: `PPO cruza abaixo do sinal`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: `PPO cruza abaixo do sinal` (opcional).
  - **Vendido**: `PPO cruza acima do sinal` (opcional).
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Fast Period` = 12.
  - `Slow Period` = 26.
  - `Signal Period` = 9.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
