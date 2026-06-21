# Estratégia MultiCamada de Aceleração/Desaceleração
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia acumula até cinco entradas compradas usando o oscilador Acceleration/Deceleration. Uma ordem de compra stop é colocada acima da máxima da barra cada vez que o momentum se desenvolve na direção da tendência identificada pelos fractais e pelos dentes do Alligator. Quando o oscilador enfraquece ou a tendência se reverte, todas as ordens pendentes são canceladas e a posição é fechada.

## Detalhes

- **Critérios de entrada**:
  - Tendência de alta confirmada quando o preço rompe um fractal de alta acima dos dentes do Alligator.
  - O oscilador AC exibe um padrão de barra verde e o fechamento está acima do filtro EMA.
  - Até cinco ordens stop são colocadas no nível de ativação.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - A tendência vira para baixo.
  - O oscilador fica negativo.
- **Stops**: Usa stop loss baseado em fractais.
- **Valores padrão**:
  - `EMA Length` = 100.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Complexo
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
