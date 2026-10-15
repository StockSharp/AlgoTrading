# Seguidor de Tendência Multitemporal com Filtro EMA 200 - Somente Comprado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia abre posições compradas quando a EMA rápida está acima da EMA lenta nos gráficos de 5, 15 e 30 minutos e o preço está acima da EMA 200 no gráfico de 5 minutos. A posição é fechada se qualquer período se tornar baixista ou o preço cair abaixo da EMA 200.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida > EMA lenta nos períodos de 5, 15 e 30 minutos e fechamento > EMA 200 (5m).
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - A tendência de qualquer período fica negativa ou fechamento < EMA 200 (5m).
- **Stops**:
  - Stop Loss: percentual.
  - Alvo de lucro: percentual.
- **Valores padrão**:
  - `Fast EMA Length` = 9
  - `Slow EMA Length` = 21
  - `200 EMA Length` = 200
  - `Stop Loss %` = 1
  - `Take Profit %` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: base 5m com confirmação 15m e 30m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
