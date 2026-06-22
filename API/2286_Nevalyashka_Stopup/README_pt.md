# Estratégia Nevalyashka Stopup
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia alterna a direção da posição após cada operação, imitando o brinquedo "Nevalyashka" que muda de lado. Utiliza uma abordagem martingale: se uma operação fecha com perda, as distâncias de stop-loss e take-profit para a próxima operação são multiplicadas por um coeficiente. Após uma operação lucrativa, as distâncias são redefinidas para seus valores base e a estratégia pode opcionalmente parar de negociar.

A direção inicial é vendida. Cada vez que uma posição é fechada, a nova posição é aberta na direção oposta com o volume pré-configurado.

## Detalhes

- **Critérios de entrada**:
  - A primeira operação vende a mercado.
  - As operações subsequentes sempre entram na direção oposta à última operação fechada.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - A posição é fechada quando o preço alcança a distância de take-profit ou stop-loss da entrada.
- **Stops**: Sim, stop-loss e take-profit fixos em pontos. As distâncias crescem pelo coeficiente martingale após perdas.
- **Valores padrão**:
  - `StopLossPoints` = 150
  - `TakeProfitPoints` = 50
  - `OrderVolume` = 0.1
  - `MartingaleCoeff` = 1.5
  - `StopAfterProfit` = false
- **Filtros**:
  - Categoria: Reversão / Martingale
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
