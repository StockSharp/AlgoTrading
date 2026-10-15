# Estratégia de Limiar RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Converte o especialista *Exp_RSI* do MetaTrader para StockSharp. A estratégia abre e fecha posições quando o Índice de Força Relativa (RSI) cruza níveis predefinidos de sobrecompra e sobrevenda.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: RSI cruza acima de `RSI Low Level`.
  - **Vendido**: RSI cruza abaixo de `RSI High Level`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Sinal inverso ou parâmetros de stop.
- **Stops**: Take Profit e Stop Loss em unidades de preço absolutas.
- **Valores padrão**:
  - `RSI Period` = 14
  - `RSI High Level` = 60
  - `RSI Low Level` = 40
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: H4
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
