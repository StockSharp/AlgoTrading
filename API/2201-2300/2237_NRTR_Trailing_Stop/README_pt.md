# Estratégia de Trailing Stop NRTR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia segue as tendências do mercado usando o indicador **NRTR (Nick R's Trend Reverse)**. O algoritmo calcula um nível de trailing stop derivado da faixa média das velas recentes. Quando o preço rompe o nível de trailing, a posição se inverte na direção do rompimento. O sistema funciona tanto no lado comprado quanto vendido e inclui proteções opcionais de stop-loss e take-profit.

O comprimento do NRTR define a sensibilidade do trailing stop: um período mais curto reage mais rápido, mas pode gerar falsas oscilações, enquanto um período mais longo filtra o ruído. Um parâmetro adicional de deslocamento de dígitos ajusta o indicador a instrumentos com diferentes escalas de preço. A estratégia se inscreve em velas do período escolhido e calcula os valores NRTR a cada barra finalizada.

## Detalhes

- **Lógica de entrada**:
  - **Comprado**: O preço cruza acima do nível NRTR após uma tendência de baixa.
  - **Vendido**: O preço cruza abaixo do nível NRTR após uma tendência de alta.
- **Lógica de saída**:
  - As posições são invertidas quando ocorre um rompimento oposto.
- **Stops**: Stop-loss e take-profit opcionais via `StartProtection`.
- **Valores padrão**:
  - `Length` = 10
  - `DigitsShift` = 0
  - `TakeProfit` = 2000 pontos
  - `StopLoss` = 1000 pontos
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: NRTR, ATR
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Flexível
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
