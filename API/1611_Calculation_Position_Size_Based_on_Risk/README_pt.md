# Estratégia de Cálculo do Tamanho de Posição Baseado em Risco
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Demonstra o dimensionamento de operações com base no risco da conta e em um percentual de stop-loss. As entradas são aleatórias para mostrar a lógica de cálculo do tamanho de posição.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: a cada 333 barras.
  - **Vendido**: a cada 444 barras.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**:
  - Apenas stop loss.
- **Stops**: Stop Loss.
- **Valores padrão**:
  - `Stop Loss %` = 10
  - `Risk Value` = 2
  - `Risk Is Percent` = true
  - `Long Period` = 333
  - `Short Period` = 444
- **Filtros**:
  - Categoria: Risk Management
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
