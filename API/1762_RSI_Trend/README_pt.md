# Estratégia de Tendência RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Tendência RSI** usa o Índice de Força Relativa (RSI) para detectar reversões de tendência e gerencia posições com um trailing stop baseado em ATR. O sistema abre uma posição comprada quando o RSI cruza acima de um limiar de sobrecompra e entra em uma posição vendida quando o RSI cai abaixo de um limiar de sobrevenda. O risco é controlado usando um trailing stop derivado do Intervalo Verdadeiro Médio (ATR), permitindo que o nível de stop se adapte à volatilidade atual.

Esta implementação é projetada para fins educacionais e demonstra como construir uma estratégia StockSharp de alto nível usando vinculações de indicadores. A estratégia opera apenas em velas concluídas e não referencia valores anteriores de indicadores diretamente, alinhando-se com as melhores práticas do StockSharp.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `RSI(t) > BuyLevel` e `RSI(t-1) <= BuyLevel`.
  - **Vendido**: `RSI(t) < SellLevel` e `RSI(t-1) >= SellLevel`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Trailing stop baseado em múltiplo de ATR.
- **Stops**: Sim, trailing stop dinâmico.
- **Valores padrão**:
  - `RSI Period` = 14.
  - `BuyLevel` = 73.
  - `SellLevel` = 27.
  - `ATR Period` = 100.
  - `ATR Multiple` = 3.
- **Filtros**:
  - Categoria: Seguidor de tendência.
  - Direção: Ambos.
  - Indicadores: RSI, ATR.
  - Stops: Sim.
  - Complexidade: Intermediário.
  - Período: Qualquer (velas de 1 minuto por padrão).
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Moderado.

