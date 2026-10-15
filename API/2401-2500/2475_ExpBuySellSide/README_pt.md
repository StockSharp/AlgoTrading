# Estratégia ExpBuySellSide
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o Expert Advisor do MetaTrader **ExpBuySellSide** para a API do StockSharp. Combina um sistema de stops baseado em ATR com um filtro de tendência simplificado Step Up/Down.

O módulo ATR calcula níveis de stop dinâmicos em torno de cada vela. Quando o preço rompe acima da banda superior, o mercado é considerado em fase de alta; romper abaixo da banda inferior indica uma fase de baixa.

O módulo Step Up/Down compara uma SMA muito rápida com uma SMA mais lenta e verifica se o diferencial entre elas está se expandindo. Um diferencial crescente na direção do cruzamento confirma a tendência.

Uma operação é aberta apenas quando **ambos** os módulos apontam na mesma direção. As posições existentes podem ser opcionalmente fechadas quando um sinal contrário aparece.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço fecha acima da banda superior ATR **e** a SMA rápida se afasta da SMA lenta para cima.
  - **Vendido**: o preço fecha abaixo da banda inferior ATR **e** a SMA rápida se afasta da SMA lenta para baixo.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal contrário aparece e a opção *Close Opposite* está habilitada.
  - Stop manual via proteção de posição.
- **Stops**: Baseados em bandas `ATR * Multiplier`.
- **Valores padrão**:
  - `ATR Period` = 5.
  - `ATR Multiplier` = 2.5.
  - `Fast SMA` = 2.
  - `Slow SMA` = 30.
  - `Candle Type` = período de 1 hora.
  - `Close Opposite` = true.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

