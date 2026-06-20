# VWMA Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Média Móvel Ponderada por Volume (VWMA) enfatiza os níveis de preço com maior volume de negociação. Esta estratégia opera os cruzamentos entre o preço e a VWMA.

Os testes indicam um retorno anual médio de aproximadamente 184%. Funciona melhor no mercado de criptomoedas.

Um fechamento acima da VWMA após estar abaixo dela gera uma entrada comprada, enquanto uma queda abaixo da VWMA provoca uma operação vendida. As posições são encerradas quando o preço cruza de volta na direção oposta.

Usar uma média ponderada por volume reduz o ruído dos períodos de baixo volume.

## Detalhes

- **Critérios de entrada**: O preço cruza a VWMA de baixo para cima ou de cima para baixo.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento inverso ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `VWMAPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: VWMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

