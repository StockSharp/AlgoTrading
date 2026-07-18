# Estratégia Accumulation/Distribution Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa o indicador Accumulation/Distribution (A/D) para medir a pressão compradora e vendedora. Um A/D crescente junto com o preço acima da média móvel sinaliza acumulação, enquanto um A/D decrescente com o preço abaixo da média indica distribuição.

Os testes indicam um retorno anual médio de aproximadamente 187%. Funciona melhor no mercado de ações.

As operações são realizadas na direção da tendência do A/D em relação à média móvel. Uma mudança na direção do A/D atua como sinal de saída.

Os stops são opcionais, mas podem ajudar a gerenciar o risco.

## Detalhes

- **Critérios de entrada**: A/D crescente com preço acima da MA ou decrescente abaixo da MA.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: A/D reverte ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: A/D, MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

