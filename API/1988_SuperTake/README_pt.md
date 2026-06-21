# Estratégia Super Take
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia alterna entre posições compradas e vendidas e aumenta o take profit após cada operação perdedora usando um multiplicador martingale. O stop loss é fixo enquanto o take profit é redefinido ao valor base após uma operação vencedora. Ao alternar constantemente de direção e ajustar as metas após perdas, a estratégia tenta recuperar drawdowns anteriores.

Uma nova posição é aberta somente quando não há posição ativa. A primeira operação é comprada por padrão. Cada operação subsequente é aberta na direção oposta à última posição fechada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Sem posição ativa e a última posição fechada foi vendida ou ausente.
  - **Vendido**: Sem posição ativa e a última posição fechada foi comprada.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Fechar a posição quando o preço atingir o take profit dinâmico ou o stop loss fixo.
- **Stops**: Stop loss fixo, take profit dinâmico com martingale após operações perdedoras.
- **Valores padrão**:
  - `TakeProfit` = 10
  - `StopLoss` = 15
  - `MartinFactor` = 1.8
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
