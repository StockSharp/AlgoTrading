# Estratégia Laguerre ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica um filtro Laguerre aos componentes +DI e -DI do indicador Average Directional Index (ADX). O suavização reduz o ruído no movimento direcional e destaca mudanças repentinas no domínio entre compradores e vendedores. Quando o +DI suavizado por Laguerre cruza abaixo do -DI suavizado, o sistema entra em uma posição comprada, esperando uma reversão de alta. Por outro lado, quando o +DI suavizado cruza acima do -DI suavizado, o sistema abre uma posição vendida.

As posições são fechadas quando os valores suavizados atuais indicam que o lado oposto assumiu o controle. O método é projetado como uma abordagem contrária, aproveitando extremos de curto prazo no índice direcional.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Laguerre +DI cruza abaixo de Laguerre –DI.
  - **Vendido**: Laguerre +DI cruza acima de Laguerre –DI.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Laguerre –DI se move acima de Laguerre +DI.
  - **Vendido**: Laguerre +DI se move acima de Laguerre –DI.
- **Stops**: Sem stops fixos, apenas proteção de posição padrão.
- **Valores padrão**:
  - `ADX Period` = 14.
  - `Gamma` = 0.764 (fator de suavização Laguerre).
  - `Candle Type` = período de 4 horas.
- **Filtros**:
  - Categoria: Contra-tendência
  - Direção: Ambos
  - Indicadores: ADX
  - Stops: Não
  - Complexidade: Médio
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
