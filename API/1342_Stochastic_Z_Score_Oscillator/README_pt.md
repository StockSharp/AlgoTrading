# Estratégia de Oscilador Stochastic Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina um oscilador Stochastic reescalado com um Z-Score de preço. Uma operação é aberta quando a média deles cruza um limiar e fechada quando o Z-Score retorna a zero. Contadores de resfriamento evitam sinais frequentes.

## Detalhes

- **Critérios de entrada**: média do %K do Stochastic reescalado e Z-Score do preço cruza acima/abaixo do limiar após o resfriamento
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Z-Score cruzando zero
- **Stops**: Não
- **Valores padrão**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `StochLength` = 14
  - `StochSmooth` = 7
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Stochastic, SMA, StandardDeviation
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
