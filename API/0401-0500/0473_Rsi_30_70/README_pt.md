# Estratégia RSI 30-70
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia simples de momentum utiliza o Índice de Força Relativa (RSI) para identificar zonas de sobrevenda e sobrecompra. Quando o RSI cai abaixo do nível de sobrevenda, uma posição comprada é aberta. A operação é encerrada assim que o RSI sobe acima do limiar de sobrecompra. O sistema opera em um único período e não toma posições vendidas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `RSI < oversold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - **Comprado**: `RSI > overbought`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Long
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
