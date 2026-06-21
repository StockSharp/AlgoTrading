# Estratégia de Média Móvel Flexível
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Ajusta a posição com base em cruzamentos entre o fechamento do período anterior e uma média móvel configurável. Um cruzamento abaixo reduz a posição em uma porcentagem definida pelo usuário, enquanto um cruzamento acima restaura a posição completa.

## Detalhes

- **Critérios de entrada**:
  - **Inicial**: Comprado completo opcional na primeira barra.
  - **Aumento**: Fechamento anterior cruza acima da média móvel → posição a 100%.
- **Critérios de saída**:
  - **Redução**: Fechamento anterior cruza abaixo da média móvel → reduzir em `SellPercentage`.
- **Indicadores**:
  - Média móvel simples, exponencial, ponderada, Hull ou suavizada.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MaLength` = 200
  - `SellPercentage` = 100
  - `MaMethod` = SMA
  - `AllowInitialBuy` = true
- **Filtros**:
  - Seguidor de tendência
  - Período único
  - Indicadores: médias móveis
  - Stops: nenhum
  - Complexidade: Básico

